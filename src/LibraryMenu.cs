using System.Linq;
using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private FavoriteStore favorites;
        private int category;
        private bool favoritesOnly;
        private static readonly string[] CategoryNames = { "Tümü", "Silah", "Zırh", "Kalkan", "Alet", "Diğer" };

        private static readonly string[] SortNames = { "Tier", "Tür", "Favori" };

        private void DrawItemLibrary()
        {
            GUILayout.Label("Eşyalar", section);
            string search = SearchInput(itemSearch, "Eşya ara");
            if (search != itemSearch) QueueAction(() => { itemSearch = search; itemScroll = Vector2.zero; libraryDirty = true; });
            int nextCategory = GUILayout.SelectionGrid(category, CategoryNames, 3, chip, GUILayout.Height(64));
            if (category != nextCategory) QueueAction(() => { category = nextCategory; itemScroll = Vector2.zero; libraryDirty = true; });
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(favoritesOnly ? "★ Favoriler" : "☆ Favoriler", "Yalnızca favorileri göster"), favoritesOnly ? selected : chip, GUILayout.Height(30)))
            {
                bool next = !favoritesOnly;
                QueueAction(() => { favoritesOnly = next; itemScroll = Vector2.zero; libraryDirty = true; });
            }
            if (GUILayout.Button(new GUIContent("Sıra: " + SortNames[itemSort], "Tier: ilerleme sırası, sonra ad • Tür: silah/zırh grupları • Favori: favoriler önce"), chip, GUILayout.Height(30)))
            {
                int next = (itemSort + 1) % SortNames.Length;
                QueueAction(() => { itemSort = next; libraryDirty = true; itemScroll = Vector2.zero; });
            }
            GUILayout.EndHorizontal();
            itemScroll = GUILayout.BeginScrollView(itemScroll, GUILayout.ExpandHeight(true));
            int previousTier = -1;
            foreach (var prefab in filteredItems)
            {
                string label = Label(prefab);
                int tier = itemTiers.Get(prefab.name);
                if (itemSort == 0 && tier != previousTier) GUILayout.Label(ItemTierIndex.Caption(tier), caption);
                previousTier = tier;
                GUILayout.BeginHorizontal();
                DrawIcon(prefab, 36);
                if (NameButton(label, label + "\nPrefab: " + prefab.name, builder.Prefab == prefab && inventoryKey == null, 38))
                    QueueDesignAction(() => SelectCatalogItem(prefab));
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && favorites.Ready;
                if (GUILayout.Button(new GUIContent(favorites.Contains(prefab.name) ? "★" : "☆", "Favoriye ekle / çıkar"), chip, GUILayout.Width(36), GUILayout.Height(36)))
                    QueueAction(() => { favorites.Toggle(prefab.name); libraryDirty = true; });
                GUI.enabled = enabled;
                GUILayout.EndHorizontal();
            }
            if (filteredItems.Length == 0)
            {
                GUILayout.Label("Filtrelere uyan eşya yok.", muted);
                if (GUILayout.Button("Filtreleri temizle")) QueueAction(() => { itemSearch = ""; category = 0; favoritesOnly = false; libraryDirty = true; itemScroll = Vector2.zero; });
            }
            GUILayout.EndScrollView();
            GUILayout.Label(filteredItems.Length + " / " + items.Count + " eşya", muted);
            if (!favorites.Ready) GUILayout.Label("Favoriler okunamadı. Yenile ile tekrar dene.", errorHint);
        }

        private void SelectCatalogItem(GameObject prefab)
        {
            if (!prefab || !prefab.TryGetComponent(out ItemDrop drop) || drop.m_itemData?.m_shared == null)
                throw new System.InvalidOperationException("Eşya artık mevcut değil. Yenile ile listeyi tekrar yükle.");
            inventoryKey = null;
            builder.Prefab = prefab; builder.Effects.Clear(); builder.Stones.Clear();
            builder.LegendaryID = null; builder.SetID = null; builder.Name = "";
            builder.Quality = "1"; builder.SocketCount = "0";
            RefreshEffects();
            status = "Seçilen eşya: " + Label(prefab);
        }
    }
}
