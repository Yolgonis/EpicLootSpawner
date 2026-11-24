// Shapes and lookup semantics verified against the installed EpicLoot 0.14.13 DLL.
// Requirements replace native item-type/external predicates with explicit prefab fixtures.
using System.Collections.Generic;
using System.Linq;
namespace EpicLoot
{
    public class MagicItemEffectRequirements
    {
        public string RequiredPrefab;
        public bool CheckRequirements(ItemDrop.ItemData item, MagicItem magic) => RequiredPrefab == null || item.m_dropPrefab?.name == RequiredPrefab;
    }
}
namespace EpicLoot.LegendarySystem
{
    public class GuaranteedMagicEffect { public string Type; public MagicItemEffectDefinition.ValueDef Values; }
    public class LegendaryInfo
    {
        public string ID, Name, Description;
        public MagicItemEffectRequirements Requirements = new MagicItemEffectRequirements();
        public List<GuaranteedMagicEffect> GuaranteedMagicEffects = new List<GuaranteedMagicEffect>();
        public int GuaranteedEffectCount = -1;
        public bool IsSetItem;
    }
    public class SetBonusInfo { public int Count; public GuaranteedMagicEffect Effect; }
    public class LegendarySetInfo
    {
        public string ID, Name;
        public List<string> LegendaryIDs = new List<string>();
        public List<SetBonusInfo> SetBonuses = new List<SetBonusInfo>();
    }
    public static class UniqueLegendaryHelper
    {
        public static readonly Dictionary<string, LegendaryInfo> LegendaryInfo = new Dictionary<string, LegendaryInfo>(), MythicInfo = new Dictionary<string, LegendaryInfo>();
        public static readonly Dictionary<string, LegendarySetInfo> LegendarySets = new Dictionary<string, LegendarySetInfo>(), MythicSets = new Dictionary<string, LegendarySetInfo>();
        public static readonly Dictionary<string, LegendarySetInfo> LegendaryItemsToSetMap = new Dictionary<string, LegendarySetInfo>(), MythicItemsToSetMap = new Dictionary<string, LegendarySetInfo>();
        public static bool TryGetLegendaryInfo(string id, out LegendaryInfo info) => MythicInfo.TryGetValue(id, out info) || LegendaryInfo.TryGetValue(id, out info);
        // EpicLoot 0.15.6: one lookup over AllSets; sets are no longer tied to a single rarity.
        public static bool TryGetLegendarySetInfo(string id, out LegendarySetInfo info)
        {
            info = null;
            return !string.IsNullOrEmpty(id) && (MythicSets.TryGetValue(id, out info) || LegendarySets.TryGetValue(id, out info));
        }
        public static bool TryGetLegendarySetInfo(string id, out LegendarySetInfo info, out ItemRarity rarity)
        {
            rarity = ItemRarity.Mythic;
            if (MythicSets.TryGetValue(id, out info)) return true;
            rarity = ItemRarity.Legendary;
            return LegendarySets.TryGetValue(id, out info);
        }
        public static string GetSetForLegendaryItem(LegendaryInfo info)
        {
            if (!info.IsSetItem) return null;
            if (MythicItemsToSetMap.TryGetValue(info.ID, out var set) || LegendaryItemsToSetMap.TryGetValue(info.ID, out set)) return set.ID;
            return null;
        }
        public static MagicItemEffectDefinition.ValueDef GetLegendaryEffectValues(string id, string type)
            => !string.IsNullOrEmpty(id) && TryGetLegendaryInfo(id, out var info) ? info.GuaranteedMagicEffects.FirstOrDefault(x => x.Type == type)?.Values : null;
        public static void Clear()
        {
            LegendaryInfo.Clear(); MythicInfo.Clear(); LegendarySets.Clear(); MythicSets.Clear(); LegendaryItemsToSetMap.Clear(); MythicItemsToSetMap.Clear();
        }
    }
}
