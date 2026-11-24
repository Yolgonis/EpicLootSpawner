using System;
using System.Linq;
using EpicLoot;
using EpicLoot.LegendarySystem;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicLootSpawner
{
    public partial class Plugin
    {
        public static void CheckSpecialInventory()
        {
            UniqueLegendaryHelper.Clear();
            var info = new LegendaryInfo { ID = "MossStaff", Name = "$moss_staff", Description = "$moss_desc", IsSetItem = true, Requirements = new MagicItemEffectRequirements { RequiredPrefab = "SwordIron" } };
            var set = new LegendarySetInfo { ID = "MossSet", Name = "$moss_set" }; set.LegendaryIDs.Add(info.ID);
            set.SetBonuses.Add(new SetBonusInfo { Count = 2, Effect = new GuaranteedMagicEffect { Type = "Damage" } });
            UniqueLegendaryHelper.LegendaryInfo[info.ID] = info;
            UniqueLegendaryHelper.LegendarySets[set.ID] = set;
            UniqueLegendaryHelper.LegendaryItemsToSetMap[info.ID] = set;
            var prefab = new GameObject { name = "SwordIron", Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
            prefab.Drop.m_itemData.m_dropPrefab = prefab; ObjectDB.instance.Items[prefab.name] = prefab;
            StonePrefab("RareStone"); StonePrefab("OtherStone");
            Localization.instance.Text[info.Name] = "Message of Lindisfarne";
            Localization.instance.Text[info.Description] = "A mossbound staff.";
            Localization.instance.Text[set.Name] = "Moss armor";
            foreach (bool cloneSync in new[] { false, true })
            {
                ItemDrop.ItemData.ForceCloneSync = cloneSync;
                Player.m_localPlayer.Inventory = new Inventory { Capacity = 12 };
                var source = RemovalItem(); source.m_dropPrefab = prefab;
                source.Magic.LegendaryID = info.ID; source.Magic.SetID = set.ID; source.Magic.Rarity = ItemRarity.Legendary;
                source.Magic.TypeNameOverride = "staff";
                API.ApplyMagicItemJson(source, JsonConvert.SerializeObject(source.Magic));
                Program.Assert(UnsupportedReason(source) == null, "known legendary/set item is selectable");
                var designed = ItemCopies.Clone(source); designed.m_quality = 35;
                var plugin = EditingPlugin(source, designed);
                plugin.LoadInventoryItem(new InventoryEntry { Item = source, Prefab = prefab });
                Program.Assert(plugin.builder.State.LegendaryID == info.ID && plugin.builder.State.SetID == set.ID && plugin.builder.State.Stones.Count == 2, "loading inventory draft retains special identity and sockets");
                var preview = PrepareInventoryEdit(source, designed);
                Program.Assert(preview.Magic.LegendaryID == info.ID && preview.Magic.SetID == set.ID && preview.Magic.TypeNameOverride == "staff", "inventory candidate retains special identity and type name");
                Program.Assert(source.m_quality == 30 && source.Magic.SetID == set.ID, "special preview isolates original");
                plugin.ApplyInventoryEdit();
                Program.Assert(source.m_quality == 35 && source.Magic.LegendaryID == info.ID && source.Magic.SetID == set.ID, "Apply writes original preserving membership");
                plugin.UndoInventoryOperation();
                Program.Assert(source.m_quality == 30 && source.Magic.LegendaryID == info.ID && source.Magic.SetID == set.ID, "edit undo restores full special state");
                var plain = DesignedItem(prefab, false);
                RejectRemoval(() => PrepareInventoryEdit(source, plain), "editing cannot silently discard special identity");
                var single = DisenchantService.Prepare(source, 0);
                Program.Assert(single.NormalItem.Magic.LegendaryID == info.ID && single.NormalItem.Magic.SetID == set.ID && source.Magic.Sockets.Count == 2, "single stone preview preserves set identity and isolates original");
                var removal = DisenchantService.Prepare(source);
                Program.Assert(removal.NormalItem.Magic == null && removal.ReturnedStones.Count == 2, "full disenchant removes unique magic and returns stones");
                string diff = plugin.DescribeComparison(source, removal.NormalItem);
                Program.Assert(diff.Contains("Özel kimlik:") && diff.Contains("Message of Lindisfarne") && diff.Contains("Set:") && diff.Contains("Moss armor"), "disenchant comparison explains identity and set loss using localized names");
                var transaction = InventoryOperations.Apply(Player.m_localPlayer.Inventory, source, removal.NormalItem, removal.ReturnedStones, "Special disenchant");
                InventoryOperations.Undo(Player.m_localPlayer.Inventory, transaction);
                Program.Assert(source.Magic.LegendaryID == info.ID && source.Magic.SetID == set.ID && source.Magic.Sockets.Count == 2, "disenchant undo restores identity and all sockets");
                source.Magic.LegendaryID = "Missing";
                Program.Assert(UnsupportedReason(source).Contains("tanımı bulunamadı"), "missing native config blocks editing explicitly");
                source.Magic.LegendaryID = info.ID; source.Magic.SetID = "Wrong";
                Program.Assert(UnsupportedReason(source).Contains("eşleşmiyor"), "inconsistent native set identity remains blocked");
            }
            ItemDrop.ItemData.ForceCloneSync = false;
            var ui = new Plugin(); ui.builder.Prefab = prefab; ui.picker = PickerKind.Special;
            ui.UpdateSpecialCatalog();
            Program.Assert(ui.specialCards.Length == 1 && ui.specialCards[0].Title == "Message of Lindisfarne" && ui.specialCards[0].Description == "A mossbound staff.", "special catalog uses game-localized names and descriptions");
            Program.Assert(ui.specialCards[0].Bonuses.Contains("2 parça"), "catalog shows set bonus thresholds");
            GUI.enabled = true; GUILayout.ClickTooltip = "Kimlik: " + info.ID + "\nA mossbound staff.";
            ui.DrawSpecialSelector();
            Program.Assert(ui.builder.LegendaryID == null && ui.pendingActions.Count == 1, "special selection is queued outside OnGUI");
            ui.pendingActions.Dequeue()();
            Program.Assert(ui.builder.LegendaryID == info.ID && ui.builder.SetID == set.ID && ui.history.CanUndo, "queued special selection records design history");
            ui.inventoryKey = "editing"; GUILayout.ClickTooltip = "Kimlik: " + info.ID + "\nA mossbound staff.";
            ui.DrawSpecialSelector();
            Program.Assert(ui.pendingActions.Count == 0 && GUI.enabled, "inventory identity picker is locked without leaking GUI enabled state");
            GUILayout.ClickTooltip = null;
            UniqueLegendaryHelper.Clear();
        }
    }
}
