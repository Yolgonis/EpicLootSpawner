using System;
using System.Globalization;
using EpicLoot;

namespace EpicLootSpawner
{
    internal static class EffectDisplay
    {
        public static float Value(string text, float fallback)
        {
            if (float.TryParse((text ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                && !float.IsNaN(value) && !float.IsInfinity(value)) return value;
            return fallback;
        }

        public static string Title(MagicItemEffectDefinition definition, float value)
        {
            try
            {
                string text = MagicItem.GetEffectText(definition, value);
                return string.IsNullOrWhiteSpace(text) ? definition.Type : text;
            }
            catch (FormatException) { return definition.Type; }
        }

        public static string Description(MagicItemEffectDefinition definition, float value)
        {
            if (string.IsNullOrWhiteSpace(definition.Description)) return "EpicLoot bu efekt için açıklama sağlamıyor.";
            try
            {
                string text = MagicItem.GetEffectDescription(definition, value);
                return string.IsNullOrWhiteSpace(text) ? "EpicLoot bu efekt için açıklama sağlamıyor." : text;
            }
            catch (FormatException) { return "EpicLoot efekt açıklamasının metin biçimi geçersiz: " + definition.Type; }
        }

        public static string SpawnerNote(MagicItemEffectDefinition definition, float value)
            => definition.Type == MagicEffectType.IncreaseTreeDrop
                ? $"Spawner notu: Ağaç/kütük ganimet olayı başına {value:0.###} ekstra odun katkısı. Ekipman toplamının kesirli kısmı aşağı yuvarlanır. Dünya kaynak çarpanı bu bonusa uygulanmaz. Kullanılan silah spawner ile üretilmiş veya düzenlenmiş olmalı."
                : "";

        public static string Range(MagicItemEffectDefinition definition, ItemRarity rarity, string legendaryID = null)
        {
            MagicItemEffectDefinition.ValueDef range;
            try { range = SpecialItems.RollRange(definition, rarity, legendaryID); }
            catch (InvalidOperationException ex) { return ex.Message; }
            return range == null ? "Aç/kapat etkisi • Normal değer: 1" : !Rules.ValidRange(range.MinValue, range.MaxValue)
                ? "EpicLoot efekt aralığı geçersiz • Yapılandırmayı kontrol et"
                : $"Normal aralık: {range.MinValue:0.###} – {range.MaxValue:0.###}";
        }
    }
}
