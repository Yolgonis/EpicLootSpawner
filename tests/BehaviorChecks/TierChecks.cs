using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpicLoot.Biomes;
using EpicLoot.GatedItemType;
using UnityEngine;

// API shape verified against the installed EpicLoot 0.14.13 assembly.
namespace EpicLoot.Biomes
{
    public static class BiomeDataManager
    {
        public static IEnumerable<string> BossKeysInOrder { get; set; } = Array.Empty<string>();
    }
}
namespace EpicLoot.GatedItemType
{
    public class GatedItemDetails { public List<string> RequiredBosses { get; set; } }
    public static class GatedItemTypeHelper
    {
        public static Dictionary<string, GatedItemDetails> AllItemsWithDetails = new Dictionary<string, GatedItemDetails>();
    }
}
namespace EpicLootSpawner
{
    public partial class Plugin
    {
        public static void CheckTierSort()
        {
            string folder = Path.Combine(Path.GetTempPath(), "SpawnerTiers-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var previousKeys = BiomeDataManager.BossKeysInOrder;
            var previousItems = GatedItemTypeHelper.AllItemsWithDetails;
            try
            {
                // Configured progression, including the shared Black Forest/Ocean boss.
                BiomeDataManager.BossKeysInOrder = new[] { "defeated_eikthyr", "defeated_gdking", "defeated_gdking", "defeated_bonemass", "defeated_dragon", "defeated_goblinking", "defeated_queen", "defeated_fader", "defeated_frozenking_p3" };
                GatedItemTypeHelper.AllItemsWithDetails = new Dictionary<string, GatedItemDetails>();
                var plugin = new Plugin { catalogLanguage = Localization.instance.GetSelectedLanguage() };
                plugin.favorites = new FavoriteStore(Path.Combine(folder, "favorites.json")); plugin.favorites.Load();
                GameObject Add(string prefab, string label, params string[] bosses)
                {
                    var item = new GameObject { name = prefab, Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
                    item.Drop.m_itemData.m_shared.m_name = label;
                    plugin.items.Add(item);
                    if (bosses != null) GatedItemTypeHelper.AllItemsWithDetails[prefab] = new GatedItemDetails { RequiredBosses = bosses.ToList() };
                    return item;
                }
                var iron = Add("SwordIron", "A Iron", "defeated_bonemass");
                var wood = Add("SwordWood", "Z Wood", "none");
                var bronzeZ = Add("SwordBronze", "Z Bronze", "defeated_gdking");
                var bronzeA = Add("AxeBronze", "A Bronze", "defeated_gdking");
                var bronzeDuplicate = Add("AxeBronze2", "A Bronze", "defeated_gdking");
                var flint = Add("AxeFlint", "Z Flint", "defeated_eikthyr");
                var silver = Add("SwordSilver", "Silver", "defeated_dragon");
                var black = Add("SwordBlackmetal", "Blackmetal", "defeated_goblinking");
                var mist = Add("SwordMistwalker", "Mistwalker", "defeated_queen");
                var ash = Add("SwordNiedhogg", "Niedhogg", "defeated_fader");
                var north = Add("SwordGold", "Gold", "defeated_frozenking_p3");
                var unknownZ = Add("ModZ", "Z Unknown", (string[])null);
                var unknownA = Add("ModA", "A Unknown", (string[])null);
                plugin.RefreshItemTiers(); plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredItems.SequenceEqual(new[] { wood, flint, bronzeA, bronzeDuplicate, bronzeZ, iron, silver, black, mist, ash, north, unknownA, unknownZ }), "ascending progression precedes alphabetical localized names; unknown items sort last; prefab breaks ties");
                for (int tier = 0; tier <= 8; tier++)
                    Program.Assert(plugin.itemTiers.Get(new[] { wood, flint, bronzeA, iron, silver, black, mist, ash, north }[tier].name) == tier, "configured progression tier " + tier + " without duplicate Ocean rank");
                Program.Assert(plugin.itemTiers.Get(null) == ItemTierIndex.Unknown && plugin.itemTiers.Get("Missing") == ItemTierIndex.Unknown, "missing identifiers have no guessed tier");

                plugin.itemSearch = "Bronze"; plugin.libraryDirty = true; plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredItems.SequenceEqual(new[] { bronzeA, bronzeDuplicate, bronzeZ }), "search retains alphabetical order within a tier");
                plugin.itemSearch = ""; plugin.favorites.Toggle(wood.name); plugin.favorites.Toggle(iron.name);
                plugin.favoritesOnly = true; plugin.libraryDirty = true; plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredItems.SequenceEqual(new[] { wood, iron }), "favorites filter retains low-to-high tiers");
                plugin.favoritesOnly = false; plugin.itemSort = 2; plugin.libraryDirty = true; plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredItems.Take(2).SequenceEqual(new[] { wood, iron }) && plugin.filteredItems[2] == flint, "favorites-first mode sorts each group by tier then name");
                iron.Drop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Chest;
                wood.Drop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Chest;
                plugin.itemSort = 1; plugin.libraryDirty = true; plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredItems.Take(3).SequenceEqual(new[] { flint, bronzeA, bronzeDuplicate }) && plugin.filteredItems.Skip(plugin.filteredItems.Length - 2).SequenceEqual(new[] { wood, iron }), "type mode preserves category groups and tier order inside groups");
                plugin.category = 2; plugin.libraryDirty = true; plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredItems.SequenceEqual(new[] { wood, iron }), "category filter retains ascending tiers");

                var multi = Add("Multi", "Multi", "defeated_eikthyr", "defeated_dragon");
                var mixed = Add("Mixed", "Mixed", "defeated_eikthyr", "unconfigured_boss");
                var ungated = Add("Ungated", "Ungated");
                var invalid = Add("Invalid", "Invalid", "");
                GatedItemTypeHelper.AllItemsWithDetails["NullDetails"] = null;
                GatedItemTypeHelper.AllItemsWithDetails["NullKeys"] = new GatedItemDetails();
                plugin.RefreshItemTiers();
                Program.Assert(plugin.itemTiers.Get(multi.name) == 4, "all boss requirements use the latest progression step");
                Program.Assert(plugin.itemTiers.Get(mixed.name) == ItemTierIndex.Unknown, "unknown required boss does not misclassify an item into a low tier");
                Program.Assert(plugin.itemTiers.Get(ungated.name) == 0, "explicit empty requirements mark an ungated starting item");
                Program.Assert(plugin.itemTiers.Get(invalid.name) == ItemTierIndex.Unknown && plugin.itemTiers.Get("NullDetails") == ItemTierIndex.Unknown && plugin.itemTiers.Get("NullKeys") == ItemTierIndex.Unknown, "incomplete metadata remains unknown without breaking the catalogue");

                // Configuration refresh uses the live API order, with no material-name guesses.
                BiomeDataManager.BossKeysInOrder = new[] { "defeated_gdking", "defeated_bonemass", "defeated_eikthyr" };
                plugin.RefreshItemTiers(); plugin.category = 0; plugin.itemSort = 0; plugin.UpdateCatalogViews();
                Program.Assert(plugin.itemTiers.Get(bronzeA.name) == 1 && plugin.itemTiers.Get(flint.name) == 3, "custom progression order is honored on refresh");
                Program.Assert(Array.IndexOf(plugin.filteredItems, bronzeA) < Array.IndexOf(plugin.filteredItems, flint), "refresh invalidates the visible ordering");
                BiomeDataManager.BossKeysInOrder = null; GatedItemTypeHelper.AllItemsWithDetails = null;
                plugin.RefreshItemTiers(); plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredItems.Length == plugin.items.Count && plugin.filteredItems[0] == bronzeA, "unavailable configuration falls back to alphabetical order without hiding items");
                Program.Assert(ItemTierIndex.Caption(0).Contains("Başlangıç") && ItemTierIndex.Caption(2) == "Tier 2" && ItemTierIndex.Caption(ItemTierIndex.Unknown) == "Tier bilinmiyor", "tier captions distinguish starting, known and unknown groups");
            }
            finally
            {
                BiomeDataManager.BossKeysInOrder = previousKeys; GatedItemTypeHelper.AllItemsWithDetails = previousItems;
                Program.CleanupFolder(folder);
            }
        }
    }
}
