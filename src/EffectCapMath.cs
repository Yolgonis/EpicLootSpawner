using System;

namespace EpicLootSpawner
{
    // Limitsiz mod effect math. EpicLoot clamps several totals inside its effect code, so
    // large item values were stored but had no extra effect. Each method returns EpicLoot's
    // original result when lifted is false. Safety limits that would break the game stay:
    // damage taken never goes negative and costs/durations never drop below zero.
    internal static class EffectCapMath
    {
        // Anti-flood ceilings for Lucky Loot; far above EpicLoot's 10x / 100 / 300 defaults.
        public const int LootMultiplierCeiling = 1000, LootUnitsCeiling = 10000;

        public static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

        // Damage-taken reductions: at most full immunity; negative values increase damage taken.
        public static float Reduction(bool lifted, float value) => lifted ? Math.Min(value, 1f) : Clamp01(value);

        // Strength fractions (health bonus, cooldown factor): no ceiling, never negative.
        public static float Strength(bool lifted, float value) => lifted ? Math.Max(value, 0f) : Clamp01(value);

        // Replaces ModifyResistance's MaxResistance cap (default 0..max) with 100% immunity.
        public static float ResistanceDamageFactor(float total) => 1f - Math.Min(total, 1f);

        // EpicLoot keeps slowed enemies at 10% speed or more; lifted, they can be stopped.
        public static float SlowMultiplier(float multiplier) => Math.Max(multiplier, 0f);

        // EpicLoot never lets the parry timer grow; negative values now widen the window.
        public static float BlockTimer(float original, float effectValue)
            => original <= 0f ? original : Math.Max(original - effectValue / 1000f, 0f);

        public static int LuckyLootMultiplier(float value)
        {
            if (float.IsNaN(value)) return 1;
            double raw = 1 + Math.Round(value * 0.5);
            return (int)Math.Max(1, Math.Min(LootMultiplierCeiling, raw));
        }

        // Shocking Charge keeps only EpicLoot's 1-charge floor, not the MinCharges setting.
        public static int ShockingCharges(int maxCharges, float chargesPerValue, float value)
        {
            double raw = Math.Round(maxCharges - (double)value * chargesPerValue);
            if (double.IsNaN(raw)) return maxCharges;
            return (int)Math.Max(1, Math.Min(maxCharges, raw));
        }

        public static float AdrenalineEfficiency(float efficiency) => Math.Max(efficiency, 0f);
    }
}
