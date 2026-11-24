using System;
using System.Globalization;

namespace EpicLootSpawner
{
    internal static class Rules
    {
        public static bool ValidRange(float min, float max)
            => !float.IsNaN(min) && !float.IsInfinity(min) && !float.IsNaN(max) && !float.IsInfinity(max) && min <= max;

        public static float DurabilityRatio(float durability, float maximum)
            => maximum <= 0 || float.IsNaN(maximum) || float.IsInfinity(maximum) || float.IsNaN(durability) || float.IsInfinity(durability) ? 0 : Math.Max(0, Math.Min(1, durability / maximum));

        public static int RemapEffectIndex(int index, string[] oldTypes, string[] newTypes)
            => index < 0 || index >= oldTypes.Length ? -1 : Array.IndexOf(newTypes, oldTypes[index]);
        public static float SliderValue(float value, float min, float max, float increment)
        {
            if (value <= min) return min;
            if (value >= max) return max;
            if (increment > 0 && !float.IsInfinity(increment))
                value = (float)(min + Math.Round(((double)value - min) / increment, MidpointRounding.AwayFromZero) * increment);
            return Math.Max(min, Math.Min(max, value));
        }
        public static int IconIndex(int count, int variant)
            => count <= 0 ? -1 : (variant >= 0 && variant < count ? variant : 0);

        public static int ExtraWoodCount(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0 || (double)value > int.MaxValue || value != Math.Truncate(value))
                throw new InvalidOperationException("Lumberjack değeri negatif olmayan bir tam sayı olmalı.");
            return (int)value;
        }

        public static bool TryRuntimeWoodCount(float value, out int count)
        {
            count = 0;
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0 || (double)value > int.MaxValue) return false;
            count = (int)value; // Match EpicLoot's truncation of the nonnegative equipment total.
            return true;
        }

        public static int WoodPackSize(int remaining, int maxStack)
            => Math.Min(remaining, Math.Max(1, maxStack));
        // Store any finite custom value, including for normally binary effects.
        public static float CustomEffectValue(string text, bool numeric)
            => EffectValue(text, null, null);

        public static int Whole(string text, int minimum, string label)
        {
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) || value < minimum)
                throw new InvalidOperationException(label + ": geçerli bir tam sayı gir.");
            return value;
        }

        public static int Bounded(string text, int min, int max, string label)
        {
            int value = Whole(text, min, label);
            if (value > max) throw new InvalidOperationException($"{label}: {min}–{max} aralığında olmalı.");
            return value;
        }

        public static float EffectValue(string text, float? min, float? max)
        {
            if ((min.HasValue && !ValidRange(min.Value, min.Value)) || (max.HasValue && !ValidRange(max.Value, max.Value))
                || (min.HasValue && max.HasValue && min.Value > max.Value))
                throw new InvalidOperationException("EpicLoot efekt aralığı geçersiz. Efekt yapılandırmasını kontrol et.");
            if (string.IsNullOrWhiteSpace(text) || !float.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                || float.IsNaN(value) || float.IsInfinity(value))
                throw new InvalidOperationException("Efekt değeri geçerli, sonlu bir sayı olmalı.");
            if (min.HasValue && value < min.Value || max.HasValue && value > max.Value)
                throw new InvalidOperationException($"Efekt değeri {min}–{max} aralığında olmalı. Aralık dışı değer için Limitsiz modu aç.");
            return value;
        }
    }
}
