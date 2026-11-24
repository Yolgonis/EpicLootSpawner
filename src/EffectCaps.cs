using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EpicLoot;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using UnityEngine;

namespace EpicLootSpawner
{
    // Lifts the strength caps inside EpicLoot's effect code while Limitsiz mod is on.
    // Every patch falls through to EpicLoot's own code when the mode is off. Targets are
    // patched one by one so a renamed EpicLoot method disables only that cap.
    internal static class EffectCaps
    {
        internal static bool Lifted;
        internal static readonly List<string> Active = new List<string>(), Missing = new List<string>();
        private static readonly Dictionary<MethodBase, MethodInfo> clampSwaps = new Dictionary<MethodBase, MethodInfo>();

        private sealed class Target
        {
            public string Name, Type, Method, Patch;
            public bool Postfix, Swap;
        }

        private static readonly Target[] Targets =
        {
            new Target { Name = "Direnç tavanı (MaxResistance)", Type = "EpicLoot.MagicItemEffects.ModifyResistance", Method = "GetCappedResistanceValue", Patch = nameof(Resistance) },
            new Target { Name = "Bulk Up can bonusu %100", Type = "EpicLoot.MagicItemEffects.BulkupEffect", Method = "GetAdditionalHealthWithBulkUp", Patch = nameof(StrengthClamp), Swap = true },
            new Target { Name = "Off-Set saldırı hasar azaltma", Type = "EpicLoot.MagicItemEffects.OffSetAttack", Method = "ReduceIncomingHit", Patch = nameof(ReductionClamp), Swap = true },
            new Target { Name = "Gece hasar azaltma", Type = "EpicLoot.MagicItemEffects.Shards.DamageReductionAtNight", Method = "ModifyIncoming", Patch = nameof(ReductionClamp), Swap = true },
            new Target { Name = "Yavaşlatma %10 tabanı", Type = "EpicLoot.MagicItemEffects.Slow", Method = "ClampMultiplier", Patch = nameof(Slow) },
            new Target { Name = "Parry penceresi", Type = "EpicLoot.MagicItemEffects.ModifyParryWindow", Method = "GetBlockTimer", Patch = nameof(BlockTimer) },
            new Target { Name = "İksir bekleme süresi", Type = "EpicLoot.Magic.MagicItemEffects.ModifyMeads", Method = "ModifyRestoringMead", Patch = nameof(StrengthClamp), Swap = true },
            new Target { Name = "Mercenary yumuşak tavanı", Type = "EpicLoot.MagicItemEffects.Shards.Mercenary", Method = "ApplySoftCap", Patch = nameof(SoftCap) },
            new Target { Name = "Lucky Loot çarpanı", Type = "EpicLoot.MagicItemEffects.Shards.LuckyLoot", Method = "Multiplier", Patch = nameof(LuckyMultiplier) },
            new Target { Name = "Lucky Loot kayıt başına adet", Type = "EpicLoot.MagicItemEffects.Shards.LuckyLoot", Method = "GetMaxUnitsPerEntry", Patch = nameof(LootUnits), Postfix = true },
            new Target { Name = "Lucky Loot ölüm başına adet", Type = "EpicLoot.MagicItemEffects.Shards.LuckyLoot", Method = "GetMaxAddedUnitsPerDeath", Patch = nameof(LootUnits), Postfix = true },
            new Target { Name = "Kan bedeli azaltma tavanı", Type = "EpicLoot.MagicItemEffects.Shards.KillsReduceNextBloodCost", Method = "GetMaxReduction", Patch = nameof(FullReduction), Postfix = true },
            new Target { Name = "Shocking Charge en az yük", Type = "EpicLoot.MagicItemEffects.Shards.EikthyrShockingCharge", Method = "GetChargesNeeded", Patch = nameof(ShockingCharges) },
            new Target { Name = "Adrenalin → dayanıklılık verimi", Type = "EpicLoot.MagicItemEffects.Shards.UseAdrenalineAsStamina", Method = "ClampEfficiency", Patch = nameof(AdrenalineEfficiency) },
        };

        internal static int Total => Targets.Length;

        public static void Apply(Harmony harmony, Action<string> warn)
        {
            Active.Clear(); Missing.Clear(); clampSwaps.Clear();
            var assembly = typeof(MagicItem).Assembly;
            foreach (var target in Targets)
            {
                try
                {
                    var method = AccessTools.Method(assembly.GetType(target.Type, true), target.Method)
                        ?? throw new MissingMethodException(target.Type, target.Method);
                    var patch = new HarmonyMethod(AccessTools.Method(typeof(EffectCaps), target.Swap ? nameof(SwapClamp) : target.Patch));
                    if (target.Swap)
                    {
                        clampSwaps[method] = AccessTools.Method(typeof(EffectCaps), target.Patch);
                        harmony.Patch(method, transpiler: patch);
                    }
                    else if (target.Postfix) harmony.Patch(method, postfix: patch);
                    else harmony.Patch(method, prefix: patch);
                    Active.Add(target.Name);
                }
                catch (Exception ex)
                {
                    Missing.Add(target.Name);
                    warn?.Invoke("Limitsiz efekt tavanı bağlanamadı (" + target.Name + "): " + ex.Message);
                }
            }
        }

        // Swaps Mathf.Clamp01 for the lift-aware helper registered for this method.
        private static IEnumerable<CodeInstruction> SwapClamp(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var clamp01 = AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp01));
            var replacement = clampSwaps[original];
            var code = instructions.ToList();
            int swapped = 0;
            foreach (var instruction in code)
                if (instruction.Calls(clamp01)) { instruction.operand = replacement; swapped++; }
            if (swapped == 0) throw new InvalidOperationException("Mathf.Clamp01 çağrısı bulunamadı.");
            return code;
        }

        private static float ReductionClamp(float value) => EffectCapMath.Reduction(Lifted, value);
        private static float StrengthClamp(float value) => EffectCapMath.Strength(Lifted, value);

        private static bool Resistance(Player player, string effect, float additionalResistance, ref float __result)
        {
            if (!Lifted || !player) return true;
            __result = EffectCapMath.ResistanceDamageFactor(player.GetTotalActiveMagicEffectValue(effect, 0.01f) + additionalResistance);
            return false;
        }

        private static bool Slow(float multiplier, ref float __result)
        {
            if (!Lifted) return true;
            __result = EffectCapMath.SlowMultiplier(multiplier);
            return false;
        }

        private static bool BlockTimer(float original, float effectValue, ref float __result)
        {
            if (!Lifted) return true;
            __result = EffectCapMath.BlockTimer(original, effectValue);
            return false;
        }

        private static bool SoftCap(float raw, ref float __result)
        {
            if (!Lifted) return true;
            __result = raw;
            return false;
        }

        private static bool LuckyMultiplier(float value, ref int __result)
        {
            if (!Lifted) return true;
            __result = EffectCapMath.LuckyLootMultiplier(value);
            return false;
        }

        private static void LootUnits(ref int __result)
        {
            if (Lifted) __result = Math.Max(__result, EffectCapMath.LootUnitsCeiling);
        }

        private static void FullReduction(ref float __result)
        {
            if (Lifted) __result = 1f;
        }

        private static bool ShockingCharges(float value, ref int __result)
        {
            if (!Lifted) return true;
            int max = EffectConfig.GetIntAtLeast(MagicEffectType.ShockingCharge, "MaxCharges", 18, 1);
            __result = EffectCapMath.ShockingCharges(max, EffectConfig.Get(MagicEffectType.ShockingCharge, "ChargesPerValue", 0.3f), value);
            return false;
        }

        private static bool AdrenalineEfficiency(float efficiency, ref float __result)
        {
            if (!Lifted) return true;
            __result = EffectCapMath.AdrenalineEfficiency(efficiency);
            return false;
        }
    }
}
