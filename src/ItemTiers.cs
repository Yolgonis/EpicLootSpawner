using System;
using System.Collections.Generic;

namespace EpicLootSpawner
{
    // Progression comes from the loaded EpicLoot configuration, independent of rarity
    // and upgrade quality. Rebuilt with the catalogue, never during GUI drawing.
    internal sealed class ItemTierIndex
    {
        public const int Unknown = int.MaxValue;
        private readonly Dictionary<string, int> tiers = new Dictionary<string, int>(StringComparer.Ordinal);

        public ItemTierIndex(IEnumerable<string> bossKeys, IEnumerable<KeyValuePair<string, IEnumerable<string>>> items)
        {
            var ranks = new Dictionary<string, int>(StringComparer.Ordinal) { ["none"] = 0 };
            int rank = 0;
            if (bossKeys != null)
                foreach (string key in bossKeys)
                    if (!string.IsNullOrWhiteSpace(key) && !ranks.ContainsKey(key)) ranks.Add(key, ++rank);

            if (items == null) return;
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Key)) continue;
                int tier = 0;
                if (item.Value == null) tier = Unknown;
                else foreach (string key in item.Value)
                {
                    if (string.IsNullOrWhiteSpace(key) || !ranks.TryGetValue(key, out int required)) { tier = Unknown; break; }
                    // EpicLoot checks all required boss keys: use the latest requirement.
                    tier = Math.Max(tier, required);
                }
                tiers[item.Key] = tier;
            }
        }

        public int Get(string prefab) => prefab != null && tiers.TryGetValue(prefab, out int tier) ? tier : Unknown;
        public static string Caption(int tier) => tier == Unknown ? "Tier bilinmiyor" : tier == 0 ? "Tier 0 • Başlangıç" : "Tier " + tier;
    }
}
