using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot;
using EpicLoot.LegendarySystem;

namespace EpicLootSpawner
{
    // Identity comes from the loaded EpicLoot configuration, never from free-form IDs.
    internal static class SpecialItems
    {
        public static bool HasIdentity(string id, string set) => !string.IsNullOrEmpty(id) || !string.IsNullOrEmpty(set);

        public static LegendaryInfo Resolve(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!UniqueLegendaryHelper.TryGetLegendaryInfo(id, out var info) || info == null || info.ID != id)
                throw new InvalidOperationException("Özel eşya tanımı bulunamadı: " + id + ". EpicLoot yapılandırmasını kontrol et.");
            return info;
        }

        public static LegendaryInfo Find(string id) => !string.IsNullOrEmpty(id) && UniqueLegendaryHelper.TryGetLegendaryInfo(id, out var info) && info?.ID == id ? info : null;

        public static ItemRarity Rarity(LegendaryInfo info) => UniqueLegendaryHelper.MythicInfo.ContainsKey(info.ID) ? ItemRarity.Mythic : ItemRarity.Legendary;
        public static string Set(LegendaryInfo info) => UniqueLegendaryHelper.GetSetForLegendaryItem(info);
        public static IEnumerable<GuaranteedMagicEffect> Required(LegendaryInfo info) => info?.GuaranteedMagicEffects ?? new List<GuaranteedMagicEffect>();

        public static void ValidatePowers(LegendaryInfo info)
        {
            if (info == null) return;
            if (info.GuaranteedMagicEffects == null)
                throw new InvalidOperationException("Özel eşyanın başlangıç güçleri eksik: " + info.ID);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var effect in Required(info))
                if (effect == null || string.IsNullOrWhiteSpace(effect.Type) || !seen.Add(effect.Type)
                    || !MagicItemEffectDefinitions.AllDefinitions.TryGetValue(effect.Type, out var definition) || definition == null)
                    throw new InvalidOperationException("Özel eşyanın başlangıç güçleri eksik veya tutarsız: " + info.ID);
        }

        public static MagicItemEffectDefinition.ValueDef RollRange(MagicItemEffectDefinition definition, ItemRarity rarity, string id)
        {
            ValidatePowers(Find(id));
            return MagicItemEffectDefinitions.GetRollRange(definition, rarity, id);
        }

        public static LegendaryInfo Check(ItemDrop.ItemData item, MagicItem magic)
        {
            if (!HasIdentity(magic.LegendaryID, magic.SetID)) return null;
            var info = Resolve(magic.LegendaryID);
            if (info == null) throw new InvalidOperationException("Set eşyasının özel parça kimliği eksik.");
            if (magic.Rarity != Rarity(info)) throw new InvalidOperationException("Özel eşyanın nadirliği kimlik tanımıyla aynı olmalı.");
            string set = Set(info);
            if ((magic.SetID ?? "") != (set ?? "")) throw new InvalidOperationException("Set kimliği bu özel eşya ile eşleşmiyor.");
            if (info.IsSetItem && (string.IsNullOrEmpty(set) || !UniqueLegendaryHelper.TryGetLegendarySetInfo(set, out var setInfo)
                || setInfo?.LegendaryIDs == null || !setInfo.LegendaryIDs.Contains(info.ID)))
                throw new InvalidOperationException("Bu özel eşyanın set tanımı eksik veya tutarsız.");
            ValidatePowers(info);
            var shell = new MagicItem { Rarity = magic.Rarity, LegendaryID = magic.LegendaryID, SetID = magic.SetID };
            if (info.Requirements != null && !info.Requirements.CheckRequirements(item, shell))
                throw new InvalidOperationException("Özel eşya kimliği bu eşya türüne uygun değil.");
            return info;
        }

        public static IEnumerable<LegendaryInfo> Available(ItemDrop.ItemData item)
        {
            foreach (var info in UniqueLegendaryHelper.LegendaryInfo.Values.Concat(UniqueLegendaryHelper.MythicInfo.Values)
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ID)).GroupBy(x => x.ID).Select(x => x.Last()))
            {
                bool valid;
                try { Check(item, new MagicItem { Rarity = Rarity(info), LegendaryID = info.ID, SetID = Set(info) }); valid = true; }
                catch (InvalidOperationException) { valid = false; }
                if (valid) yield return info;
            }
        }

        public static string Name(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Sıradan büyülü eşya";
            try { return Localization.instance.Localize(Resolve(id).Name ?? id); }
            catch (InvalidOperationException) { return id; }
        }

        public static string SetName(string set)
        {
            if (string.IsNullOrEmpty(set)) return "Yok";
            return UniqueLegendaryHelper.TryGetLegendarySetInfo(set, out var info) && info != null ? Localization.instance.Localize(info.Name ?? set) : set;
        }
    }
}
