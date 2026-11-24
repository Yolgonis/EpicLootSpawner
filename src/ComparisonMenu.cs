using System.Globalization;
using System.Linq;
using System.Text;
using EpicLoot;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private string comparisonPreview = "";

        private static ItemSnapshot Snapshot(ItemDrop.ItemData item)
        {
            var magic = item.GetMagicItem();
            var snapshot = new ItemSnapshot { Quality = item.m_quality, Rarity = magic == null ? (int?)null : (int)magic.Rarity,
                Name = magic?.DisplayName ?? "", SocketCount = magic?.SocketCount ?? 0, LegendaryID = magic?.LegendaryID, SetID = magic?.SetID };
            if (magic == null) return snapshot;
            foreach (string type in magic.GetEffects().Select(x => x.EffectType).Distinct())
                snapshot.Rolled[type] = magic.GetTotalEffectValue(type, includeSocketed: false);
            foreach (string type in magic.GetEffects(includeSocketed: true).Select(x => x.EffectType).Distinct())
                snapshot.Totals[type] = magic.GetTotalEffectValue(type, includeSocketed: true);
            snapshot.Stones.AddRange(magic.Sockets.Select(x => x?.SourcePrefab));
            return snapshot;
        }

        private static string SafeLabel(string text) => (text ?? "").Replace('<', '‹').Replace('>', '›');
        private static string RarityName(int? rarity) => rarity.HasValue ? Localization.instance.Localize(API.GetRarityDisplayNameByIndex(rarity.Value)) : "Büyüsüz";
        private static string EffectResult(string type, float? value)
        {
            if (!value.HasValue) return "Yok";
            if (MagicItemEffectDefinitions.AllDefinitions.TryGetValue(type, out var definition)) return EffectDisplay.Title(definition, value.Value);
            return SafeLabel(type) + ": " + value.Value.ToString("G9", CultureInfo.CurrentCulture);
        }
        private string StoneName(string prefab)
        {
            if (string.IsNullOrEmpty(prefab)) return "Boş";
            var item = ObjectDB.instance.GetItemPrefab(prefab);
            return item ? Label(item) : SafeLabel(prefab);
        }
        private string DescribeComparison(ItemDrop.ItemData original, ItemDrop.ItemData candidate)
        {
            var before = Snapshot(original); var after = Snapshot(candidate);
            var changes = ItemDifference.Between(before, after);
            if (!changes.HasChanges) return "<color=#79C7A0>✓ Eşya tasarımı aynı.</color>\n\nSeviye, nadirlik, ad, efektler ve soketlerde değişiklik yok.";
            var text = new StringBuilder("<b>Uygulanacak değişiklikler</b>\n");
            if (changes.Quality) text.AppendLine("Seviye: " + before.Quality + " → " + after.Quality);
            if (changes.Rarity) text.AppendLine("Nadirlik: " + RarityName(before.Rarity) + " → " + RarityName(after.Rarity));
            if (changes.Identity) text.AppendLine("Özel kimlik: " + SafeLabel(SpecialItems.Name(before.LegendaryID)) + " → " + SafeLabel(SpecialItems.Name(after.LegendaryID)));
            if (changes.Set) text.AppendLine("Set: " + SafeLabel(SpecialItems.SetName(before.SetID)) + " → " + SafeLabel(SpecialItems.SetName(after.SetID)));
            if (changes.Name) text.AppendLine("Özel ad: " + (string.IsNullOrEmpty(before.Name) ? "Otomatik" : SafeLabel(before.Name)) + " → " + (string.IsNullOrEmpty(after.Name) ? "Otomatik" : SafeLabel(after.Name)));
            if (changes.Sockets) text.AppendLine("Soket sayısı: " + before.SocketCount + " → " + after.SocketCount);
            if (changes.Rolled.Count > 0)
            {
                text.AppendLine("\n<b>Eşyanın efektleri</b>");
                AppendEffects(text, changes.Rolled);
            }
            if (changes.Stones.Count > 0)
            {
                text.AppendLine("\n<b>Taşlar</b>");
                foreach (var stone in changes.Stones)
                    text.AppendLine("Soket " + stone.Slot + ": " + StoneName(stone.Before) + " → " + StoneName(stone.After));
            }
            if ((before.Stones.Count > 0 || after.Stones.Count > 0) && changes.Totals.Count > 0)
            {
                text.AppendLine("\n<b>Toplam etkiler · eşya + taş</b>");
                AppendEffects(text, changes.Totals);
            }
            text.Append("\nRenkler ekleme, kaldırma ve değişimi gösterir. Tam eşya bilgisi diğer sekmelerde.");
            return text.ToString();
        }
        private static void AppendEffects(StringBuilder text, System.Collections.Generic.List<EffectDifference> changes)
        {
            foreach (var effect in changes.OrderBy(x => EffectResult(x.Type, x.After ?? x.Before), System.StringComparer.CurrentCultureIgnoreCase))
            {
                string color = effect.Kind == ChangeKind.Added ? "#79C7A0" : effect.Kind == ChangeKind.Removed ? "#EAAD96" : "#DBBC7B";
                string tag = effect.Kind == ChangeKind.Added ? "+ " : effect.Kind == ChangeKind.Removed ? "− " : "↔ ";
                text.AppendLine("<color=" + color + ">" + tag + EffectResult(effect.Type, effect.Before) + " → " + EffectResult(effect.Type, effect.After) + "</color>");
            }
        }
    }
}
