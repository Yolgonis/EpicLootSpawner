using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot;
using EpicLoot.MagicItemEffects;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using UnityEngine;

namespace EpicLootSpawner
{
    [HarmonyPatch(typeof(IncreaseDrop), nameof(IncreaseDrop.TryDropExtraItems))]
    internal static class ExactWoodDrops
    {
        public const string Marker = Plugin.Id + ".exact-effects";
        internal static Action<Exception> Warning;
        private static readonly HashSet<string> WoodPrefabs = new HashSet<string>(StringComparer.Ordinal)
        { "Wood", "FineWood", "RoundLog", "YggdrasilWood" };

        private static bool Prefix(IncreaseDrop __instance, Character character, DropTable dropTable, Vector3 objPosition)
        {
            bool deliveryStarted = false;
            try
            {
                if (__instance.MagicEffect != MagicEffectType.IncreaseTreeDrop || !(character is Player player)
                    || player != Player.m_localPlayer) return true;
                var weapon = player.GetCurrentWeapon();
                if (weapon == null || !weapon.m_customData.ContainsKey(Marker)) return true;
                if (!MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, weapon, MagicEffectType.IncreaseTreeDrop, out float value))
                    return false;
                if (!Rules.TryRuntimeWoodCount(value, out int count)) return true;
                if (count == 0 || dropTable == null) return false;
                var choices = dropTable.m_drops.Where(x => x.m_item && WoodPrefabs.Contains(x.m_item.name)
                    && x.m_item.GetComponent<ItemDrop>() != null).ToArray();
                if (choices.Length == 0) return true;
                double weight = choices.Sum(x => Math.Max(0, (double)x.m_weight));
                int remaining = count;
                // Preserve species weights, but bypass quantity/chance/resource-rate rolls.
                while (remaining > 0)
                {
                    var selected = choices[0];
                    double roll = UnityEngine.Random.value * weight;
                    foreach (var choice in choices)
                    {
                        roll -= Math.Max(0, (double)choice.m_weight);
                        if (roll <= 0) { selected = choice; break; }
                    }
                    var item = selected.m_item.GetComponent<ItemDrop>().m_itemData.Clone();
                    item.m_dropPrefab = selected.m_item;
                    item.m_stack = Rules.WoodPackSize(remaining, item.m_shared.m_maxStackSize);
                    int units = item.m_stack;
                    deliveryStarted = true;
                    if (!ItemDrop.DropItem(item, units, objPosition + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.4f, Quaternion.identity))
                        throw new InvalidOperationException("Ekstra odun oluşturulamadı.");
                    remaining -= units;
                }
                return false; // Prevent a second bonus through EpicLoot's original roll path.
            }
            catch (Exception ex)
            {
                try { Warning?.Invoke(ex); } catch { }
                // A failed bonus must not abort the caller's tree destruction. Once delivery
                // started, avoid the original bonus path because some units may already exist.
                return !deliveryStarted;
            }
        }
    }
}
