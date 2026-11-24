using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicLootSpawner
{
    internal sealed class ItemSnapshot
    {
        public int Quality, SocketCount;
        public int? Rarity;
        public string Name;
        public string LegendaryID, SetID;
        public Dictionary<string, float> Rolled = new Dictionary<string, float>(StringComparer.Ordinal);
        public Dictionary<string, float> Totals = new Dictionary<string, float>(StringComparer.Ordinal);
        public List<string> Stones = new List<string>();
    }

    internal enum ChangeKind { Added, Removed, Changed }
    internal sealed class EffectDifference
    {
        public string Type;
        public float? Before, After;
        public ChangeKind Kind => !Before.HasValue ? ChangeKind.Added : !After.HasValue ? ChangeKind.Removed : ChangeKind.Changed;
    }
    internal sealed class StoneDifference
    {
        public int Slot;
        public string Before, After;
    }
    internal sealed class ItemDifference
    {
        public bool Quality, Rarity, Name, Sockets, Identity, Set;
        public List<EffectDifference> Rolled, Totals;
        public List<StoneDifference> Stones;
        public bool HasChanges => Quality || Rarity || Name || Sockets || Identity || Set || Rolled.Count > 0 || Totals.Count > 0 || Stones.Count > 0;

        public static ItemDifference Between(ItemSnapshot before, ItemSnapshot after)
        {
            var changes = new ItemDifference
            {
                Quality = before.Quality != after.Quality, Rarity = before.Rarity != after.Rarity,
                Name = (before.Name ?? "") != (after.Name ?? ""), Sockets = before.SocketCount != after.SocketCount,
                Identity = (before.LegendaryID ?? "") != (after.LegendaryID ?? ""), Set = (before.SetID ?? "") != (after.SetID ?? ""),
                Rolled = Effects(before.Rolled, after.Rolled), Totals = Effects(before.Totals, after.Totals),
                Stones = new List<StoneDifference>()
            };
            for (int i = 0; i < Math.Max(before.Stones.Count, after.Stones.Count); i++)
            {
                string oldStone = i < before.Stones.Count ? before.Stones[i] : null;
                string newStone = i < after.Stones.Count ? after.Stones[i] : null;
                if (oldStone != newStone) changes.Stones.Add(new StoneDifference { Slot = i + 1, Before = oldStone, After = newStone });
            }
            return changes;
        }

        private static List<EffectDifference> Effects(Dictionary<string, float> before, Dictionary<string, float> after)
        {
            var result = new List<EffectDifference>();
            foreach (string type in before.Keys.Union(after.Keys).OrderBy(x => x, StringComparer.Ordinal))
            {
                bool had = before.TryGetValue(type, out float oldValue), has = after.TryGetValue(type, out float newValue);
                // Presence matters even when the value is zero (binary/zero-valued effects).
                if (had != has || oldValue != newValue)
                    result.Add(new EffectDifference { Type = type, Before = had ? (float?)oldValue : null, After = has ? (float?)newValue : null });
            }
            return result;
        }
    }
}
