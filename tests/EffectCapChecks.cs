using System;
using EpicLootSpawner;

// Limitsiz mod lifts EpicLoot's in-effect caps. Off, every helper must match EpicLoot's
// original clamp; on, strength grows with the value while game-breaking limits stay.
internal static class EffectCapChecks
{
    public static void Run(Action<bool, string> check)
    {
        bool Near(float a, float b) => Math.Abs(a - b) < 0.0001f;
        foreach (float v in new[] { -2f, -0.5f, 0f, 0.3f, 1f, 1.5f, 10f })
        {
            check(Near(EffectCapMath.Reduction(false, v), Math.Clamp(v, 0f, 1f)), "reduction off matches Clamp01: " + v);
            check(Near(EffectCapMath.Strength(false, v), Math.Clamp(v, 0f, 1f)), "strength off matches Clamp01: " + v);
        }
        check(Near(EffectCapMath.Reduction(true, 0.4f), 0.4f), "lifted reduction keeps in-range value");
        check(Near(EffectCapMath.Reduction(true, 5f), 1f), "lifted reduction stops at full immunity, never negative damage");
        check(Near(EffectCapMath.Reduction(true, -0.5f), -0.5f), "lifted reduction lets negative values increase damage taken");
        check(Near(EffectCapMath.Strength(true, 5f), 5f), "lifted strength has no 100% ceiling");
        check(Near(EffectCapMath.Strength(true, -3f), 0f), "lifted strength never goes negative");
        check(Near(EffectCapMath.Strength(true, 1f - (-1f)), 2f), "negative mead cooldown value lengthens the mead");

        check(Near(EffectCapMath.ResistanceDamageFactor(0.95f), 0.05f), "resistance above EpicLoot's MaxResistance reduces damage further");
        check(Near(EffectCapMath.ResistanceDamageFactor(3f), 0f), "resistance stops at immunity, never heals");
        check(Near(EffectCapMath.ResistanceDamageFactor(-0.5f), 1.5f), "negative resistance increases damage taken");

        check(Near(EffectCapMath.SlowMultiplier(0.02f), 0.02f), "slow goes below EpicLoot's 10% floor");
        check(Near(EffectCapMath.SlowMultiplier(-1f), 0f), "slow never reverses movement");
        check(Near(EffectCapMath.SlowMultiplier(1.2f), 1.2f), "negative slow can speed enemies up");

        check(Near(EffectCapMath.BlockTimer(0.1f, 50f), 0.05f), "parry window shrinks the block timer as before");
        check(Near(EffectCapMath.BlockTimer(0.1f, 500f), 0f), "parry block timer never goes negative");
        check(Near(EffectCapMath.BlockTimer(0.1f, -100f), 0.2f), "negative parry value is no longer capped at the original timer");
        check(Near(EffectCapMath.BlockTimer(-1f, 50f), -1f), "inactive block timer is untouched");

        check(EffectCapMath.LuckyLootMultiplier(10f) == 6, "lucky loot multiplier follows EpicLoot's formula");
        check(EffectCapMath.LuckyLootMultiplier(100f) == 51, "lucky loot multiplier passes EpicLoot's 10x default");
        check(EffectCapMath.LuckyLootMultiplier(1e9f) == EffectCapMath.LootMultiplierCeiling, "lucky loot multiplier keeps an anti-flood ceiling");
        check(EffectCapMath.LuckyLootMultiplier(-50f) == 1 && EffectCapMath.LuckyLootMultiplier(float.NaN) == 1, "lucky loot multiplier never drops below 1");
        check(EffectCapMath.LootUnitsCeiling > 300, "lucky loot unit ceiling is above EpicLoot defaults");

        check(EffectCapMath.ShockingCharges(18, 0.3f, 10f) == 15, "shocking charge follows EpicLoot's formula in range");
        check(EffectCapMath.ShockingCharges(18, 0.3f, 50f) == 3, "shocking charge goes below the MinCharges setting");
        check(EffectCapMath.ShockingCharges(18, 0.3f, 1000f) == 1, "shocking charge keeps a one-charge floor");
        check(EffectCapMath.ShockingCharges(18, 0.3f, -100f) == 18, "shocking charge never exceeds MaxCharges");
        check(EffectCapMath.ShockingCharges(18, 0.3f, float.NaN) == 18, "non-finite shocking charge value falls back to MaxCharges");

        check(Near(EffectCapMath.AdrenalineEfficiency(2.5f), 2.5f), "adrenaline efficiency passes 100%");
        check(Near(EffectCapMath.AdrenalineEfficiency(-1f), 0f), "adrenaline efficiency never goes negative");
    }
}
