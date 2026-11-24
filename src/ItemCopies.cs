using System.Reflection;
using EpicLoot.MagicItemEffects;

namespace EpicLootSpawner
{
    internal static class ItemCopies
    {
        private static readonly MethodInfo Cloner = typeof(ItemDrop.ItemData.SharedData).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        public static ItemDrop.ItemData.SharedData Baseline(ItemDrop.ItemData item)
        {
            var shared = (ItemDrop.ItemData.SharedData)Cloner.Invoke(item.m_shared, null);
            shared.m_useDurability = Indestructible.OriginallyUsesDurability(item);
            return shared;
        }
        public static void RestoreBaseline(ItemDrop.ItemData item, ItemDrop.ItemData.SharedData baseline)
        {
            Indestructible.Revert(item);
            item.m_shared = baseline;
            Indestructible.Sync(item);
        }
        public static ItemDrop.ItemData Clone(ItemDrop.ItemData item)
        {
            var baseline = Baseline(item);
            var clone = item.Clone();
            RestoreBaseline(clone, baseline);
            return clone;
        }
    }
}
