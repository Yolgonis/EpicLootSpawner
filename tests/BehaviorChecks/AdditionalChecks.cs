using System;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using EpicLoot;

namespace EpicLootSpawner
{
    public partial class Plugin
    {
        public static void CheckGameplayLanguage()
        {
            string previousLanguage = Localization.instance.Language;
            var definition = new MagicItemEffectDefinition { Type = MagicEffectType.IncreaseTreeDrop,
                Title = "$spawner_test_wood_title", Description = "$spawner_test_wood_description",
                Range = new MagicItemEffectDefinition.ValueDef { MinValue = 30, MaxValue = 40 } };
            try
            {
                Localization.instance.Language = "English";
                Localization.instance.Text[definition.Title] = "Lumberjack +30";
                Localization.instance.Text[definition.Description] = "Drops 30 extra wood.";
                var plugin = new Plugin { catalogLanguage = "English" };
                plugin.available.Add(definition); plugin.RefreshEffectCards(); plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredEffects[0].Title == "Lumberjack +30", "effect name follows English game text");
                Program.Assert(plugin.filteredEffects[0].Description == "Drops 30 extra wood.", "Lumberjack keeps the EpicLoot description instead of replacing it with Turkish spawner guidance");
                Program.Assert(plugin.filteredEffects[0].Note.StartsWith("Spawner notu:") && plugin.filteredEffects[0].Note.Contains("30 ekstra odun"), "Turkish spawner-specific rules remain a separate annotated note");
                plugin.effectSearch = "Drops"; plugin.effectFilterDirty = true; plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredEffects.Length == 1, "English description participates in effect search");
                Localization.instance.Language = "Turkish";
                Localization.instance.Text[definition.Title] = "Oduncu +30";
                Localization.instance.Text[definition.Description] = "30 ekstra odun düşürür.";
                plugin.effectSearch = ""; plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredEffects[0].Title == "Oduncu +30" && plugin.filteredEffects[0].Description == "30 ekstra odun düşürür.", "changing game language invalidates both cached effect name and description");
                definition.Description = null;
                Program.Assert(EffectDisplay.Description(definition, 30).Contains("açıklama sağlamıyor"), "missing optional EpicLoot description has a Turkish interface fallback");
                definition.Type = "OtherEffect";
                Program.Assert(EffectDisplay.SpawnerNote(definition, 30) == "", "other effects acquire no Lumberjack-specific note");
            }
            finally
            {
                Localization.instance.Language = previousLanguage;
                Localization.instance.Text.Remove("$spawner_test_wood_title"); Localization.instance.Text.Remove("$spawner_test_wood_description");
            }
        }

        public static void CheckRetentionAndHistory()
        {
            Player.m_localPlayer.Inventory.Items.Clear();
            var prefab = new UnityEngine.GameObject { name = "SwordIron" };
            var item = new ItemDrop.ItemData { m_dropPrefab = prefab };
            Player.m_localPlayer.Inventory.Items.Add(item);
            var plugin = new Plugin(); plugin.builder.Prefab = prefab;
            var entry = new InventoryEntry { Item = item, Prefab = prefab };
            for (int i = 0; i < 200; i++)
            {
                plugin.QueueDesignAction(() => plugin.LoadInventoryItem(entry));
                while (plugin.pendingActions.Count > 0) plugin.pendingActions.Dequeue()();
            }
            Program.Assert(plugin.inventorySources.Count == 60, "source registry follows bounded history");
            string active = plugin.inventoryKey;
            plugin.RestoreHistory(true);
            Program.Assert(plugin.inventoryKey != active && plugin.EditingSource.Item == item, "undo retains correct source identity");
            plugin.RestoreHistory(false);
            Program.Assert(plugin.inventoryKey == active && plugin.EditingSource.Item == item, "redo source remains available");
            plugin.QueueDesignAction(() => plugin.inventoryKey = null);
            while (plugin.pendingActions.Count > 0) plugin.pendingActions.Dequeue()();
            Program.Assert(plugin.inventorySources.Count == 59, "obsolete source removed when history shifts");
            plugin.RestoreHistory(true);
            Program.Assert(plugin.EditingSource.Item == item, "undo production-mode switch recovers source");
            plugin.QueueDesignAction(() => plugin.builder.Quality = "2");
            while (plugin.pendingActions.Count > 0) plugin.pendingActions.Dequeue()();
            Program.Assert(!plugin.history.CanRedo && plugin.inventorySources.Count <= 60 && plugin.EditingSource.Item == item, "history branch cleans discarded references");

            var snapshotPlugin = new Plugin(); snapshotPlugin.builder.State.Prefab = "SwordIron";
            snapshotPlugin.RecordDesign(); snapshotPlugin.builder.Quality = "2"; snapshotPlugin.RecordDesign(); snapshotPlugin.RestoreHistory(true);
            snapshotPlugin.ObserveDesign();
            Spawner.DraftCalls = 0;
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++) snapshotPlugin.DrawHistory();
            double cachedMs = timer.Elapsed.TotalMilliseconds;
            Program.Assert(Spawner.DraftCalls == 0, "history GUI performs zero draft serialization calls");
            timer.Restart();
            for (int i = 0; i < 10000; i++) { snapshotPlugin.DesignSnapshot(); snapshotPlugin.DesignSnapshot(); }
            double legacyMs = timer.Elapsed.TotalMilliseconds;
            Program.Assert(Spawner.DraftCalls == 20000, "historic snapshot workload matches two calls per event");
            Console.WriteLine("HISTORY_BENCHMARK " + JsonConvert.SerializeObject(new { iterations = 10000, legacyMs, cachedMs, legacyDraftCalls = Spawner.DraftCalls, cachedDraftCalls = 0 }));
        }
        public static void CheckSortModes()
        {
            var plugin = new Plugin(); plugin.catalogLanguage = Localization.instance.GetSelectedLanguage();
            string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SpawnerSort-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(folder);
            string path = System.IO.Path.Combine(folder, "favorites.json");
            try
            {
                plugin.favorites = new FavoriteStore(path); plugin.favorites.Load();
                var armor = new UnityEngine.GameObject { name = "Armor", Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
                armor.Drop.m_itemData.m_shared.m_name = "A Armor"; armor.Drop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Chest;
                var sword = new UnityEngine.GameObject { name = "Sword", Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
                sword.Drop.m_itemData.m_shared.m_name = "Z Sword";
                plugin.items.Add(armor); plugin.items.Add(sword);
                plugin.itemSort = 0; plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredItems[0] == armor, "alphabetic sort by display name");
                plugin.itemSort = 1; plugin.libraryDirty = true; plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredItems[0] == sword, "item-type sorting groups weapons first");
                plugin.favorites.Toggle("Sword"); plugin.itemSort = 2; plugin.libraryDirty = true; plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredItems[0] == sword, "favorites-first sorting");
                plugin.category = 2; plugin.libraryDirty = true; plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredItems.Length == 1 && plugin.filteredItems[0] == armor, "sort preserves category filter");
            }
            finally { Program.CleanupFolder(folder); }
        }
    }
}
