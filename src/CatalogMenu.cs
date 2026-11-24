using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot;
using EpicLoot.ShardStones;
using EpicLoot.Biomes;
using EpicLoot.GatedItemType;
using UnityEngine;

namespace EpicLootSpawner
{
    internal sealed class EffectCard
    {
        public MagicItemEffectDefinition Definition;
        public string Title, Description, Note, Range, Search;
    }
    public sealed partial class Plugin
    {
        private static readonly Dictionary<GameObject, string> itemLabels = new Dictionary<GameObject, string>();
        private readonly List<EffectCard> effectCards = new List<EffectCard>();
        private GameObject[] filteredItems = Array.Empty<GameObject>(), filteredStones = Array.Empty<GameObject>();
        private EffectCard[] filteredEffects = Array.Empty<EffectCard>();
        private bool libraryDirty = true, effectFilterDirty = true, stoneFilterDirty = true;
        private string catalogLanguage;
        private int itemSort;
        private ItemTierIndex itemTiers = new ItemTierIndex(null, null);

        private void RefreshItemTiers()
        {
            itemTiers = new ItemTierIndex(BiomeDataManager.BossKeysInOrder,
                GatedItemTypeHelper.AllItemsWithDetails?.Select(x =>
                    new KeyValuePair<string, IEnumerable<string>>(x.Key, x.Value?.RequiredBosses)));
            libraryDirty = true;
        }

        private static string Label(GameObject prefab)
        {
            if (!prefab) return "Eksik eşya";
            if (itemLabels.TryGetValue(prefab, out string label)) return label;
            string name = prefab.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_name;
            label = string.IsNullOrEmpty(name) ? prefab.name : Localization.instance.Localize(name);
            itemLabels[prefab] = label;
            return label;
        }
        private static bool Matches(string text, string search) => text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        private static int ItemCategory(ItemDrop.ItemData item)
        {
            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Bow: return 1;
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder: return 2;
                case ItemDrop.ItemData.ItemType.Shield: return 3;
                case ItemDrop.ItemData.ItemType.Tool: return 4;
                default: return 5;
            }
        }

        private void RefreshEffectCards()
        {
            effectCards.Clear();
            foreach (var definition in available)
            {
                MagicItemEffectDefinition.ValueDef range = null;
                try { range = builder.EffectRange(definition); }
                catch (InvalidOperationException) { } // Range text explains invalid special configuration; other cards stay usable.
                float value = range?.MinValue ?? 1;
                string title = EffectDisplay.Title(definition, value), description = EffectDisplay.Description(definition, value);
                string note = EffectDisplay.SpawnerNote(definition, value);
                effectCards.Add(new EffectCard { Definition = definition, Title = title, Description = description,
                    Note = note, Range = EffectDisplay.Range(definition, builder.Rarity, builder.LegendaryID), Search = definition.Type + " " + title + " " + description + " " + note });
            }
            effectCards.Sort((a, b) => { int order = string.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase); return order != 0 ? order : string.CompareOrdinal(a.Definition.Type, b.Definition.Type); });
            effectFilterDirty = true;
        }
        private void UpdateCatalogViews()
        {
            if (!IsOpen) return;
            string language = Localization.instance.GetSelectedLanguage();
            if (language != catalogLanguage) Run(RefreshCatalog);
            if (libraryDirty)
            {
                filteredItems = items.Where(x => Matches(x.name + " " + Label(x), itemSearch)
                    && (category == 0 || ItemCategory(x.GetComponent<ItemDrop>().m_itemData) == category)
                    && (!favoritesOnly || favorites.Contains(x.name)))
                    .OrderBy(x => itemSort == 2 && favorites.Contains(x.name) ? 0 : 1)
                    .ThenBy(x => itemSort == 1 ? ItemCategory(x.GetComponent<ItemDrop>().m_itemData) : 0)
                    .ThenBy(x => itemTiers.Get(x.name))
                    .ThenBy(Label, StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.name, StringComparer.Ordinal).ToArray();
                libraryDirty = false;
            }
            if (effectFilterDirty)
            {
                filteredEffects = effectCards.Where(x => Matches(x.Search, effectSearch)).ToArray(); effectFilterDirty = false;
            }
            if (stoneFilterDirty)
            {
                filteredStones = stones.Where(x => Shards.GetShardRarity(x.GetComponent<ItemDrop>().m_itemData) == stoneRarity
                    && Matches(x.name + " " + Label(x), stoneSearch)).ToArray(); stoneFilterDirty = false;
            }
        }
    }
}
