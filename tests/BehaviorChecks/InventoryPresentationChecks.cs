using System;
using System.Globalization;
using System.Linq;
using EpicLoot;
using EpicLoot.ShardStones;
using UnityEngine;

namespace EpicLootSpawner
{
    public partial class Plugin
    {
        private static string RejectionText(Action action)
        {
            try { action(); } catch (InvalidOperationException ex) { return ex.Message; }
            throw new Exception("Expected rejection missing");
        }

        public static void CheckInventoryPresentation()
        {
            var oldLanguage = Localization.instance.Language;
            var oldNames = API.DisplayNameOverride;
            var oldDamage = MagicItemEffectDefinitions.AllDefinitions.TryGetValue("Damage", out var savedDamage) ? savedDamage : null;
            var rare = StonePrefab("RareStone"); var other = StonePrefab("OtherStone");
            API.DisplayNameOverride = item => item.m_shared.m_name;
            MagicItemEffectDefinitions.AllDefinitions["Damage"] = new MagicItemEffectDefinition { Type = "Damage", Title = "$socket_damage", Description = "$socket_description" };
            Localization.instance.Text["RareStone"] = "Rare shardstone";
            Localization.instance.Text["OtherStone"] = "Other shardstone";
            Localization.instance.Text["$socket_damage"] = "Damage +{0}";
            Localization.instance.Text["$socket_description"] = "Increases damage.";
            try
            {
                var source = RemovalItem(); string before = InventoryOperations.Signature(source);
                var active = SocketCard.From(source.Magic.Sockets[0], 0);
                var inert = SocketCard.From(source.Magic.Sockets[1], 1);
                Program.Assert(active.Name == "Soket 1 • Rare shardstone" && active.Title == "Damage +10" && active.Description == "Increases damage.", "socket cards use localized game names effect titles descriptions");
                Program.Assert(inert.Name.StartsWith("Soket 2") && inert.Title == "Bu eşyada etkisiz" && inert.Description.Contains("iade"), "inert socket explains contribution and return without hiding stone");
                Program.Assert(InventoryOperations.Signature(source) == before, "building socket cards never mutates equipment");
                var unknown = SocketCard.From(new SocketedEffect { SourcePrefab = "MissingStone", Effect = new MagicItemEffect { EffectType = "<Unknown>", EffectValue = 12.5f } }, 2);
                Program.Assert(unknown.Name.Contains("MissingStone") && unknown.Title.Contains("‹Unknown›") && unknown.Title.Contains(12.5f.ToString("G9", CultureInfo.CurrentCulture)), "unknown socket renders safe raw effect value and prefab fallback");
                Program.Assert(SocketCard.From(null, 0).Title == "Bu eşyada etkisiz", "unavailable socket display remains readable");

                var sword = new GameObject { name = "Sword" }; source.m_dropPrefab = sword;
                Player.m_localPlayer.Inventory = new Inventory { Capacity = 1 };
                Time.unscaledTime = 0;
                var plugin = EditingPlugin(source, DesignedItem(sword, false));
                plugin.OpenRemoval(0);
                Program.Assert(plugin.showDisenchant && plugin.disenchantReason != null && plugin.removalStoneCards.Length == 2, "full inventory still permits viewing and choosing stones");
                Program.Assert(plugin.disenchantComparison.Contains("Toplam etkiler") && plugin.disenchantComparison.Contains("Damage +10"), "removal confirmation includes localized total effect change summary");
                string originalMagic = API.GetMagicItemJson(source);
                GUI.enabled = true; GUILayout.ParentRoom = 400; GUILayout.ClickTooltip = "Taşı seç • Soket 2\n" + plugin.removalStoneCards[1].Name;
                plugin.DrawDisenchant(1);
                Program.Assert(plugin.pendingActions.Count == 1 && plugin.disenchantPlan.SocketIndex == 0 && API.GetMagicItemJson(source) == originalMagic, "stone card click queues selection without mutating GUI iteration or item");
                while (plugin.pendingActions.Count > 0) plugin.Run(plugin.pendingActions.Dequeue());
                Program.Assert(plugin.disenchantPlan.SocketIndex == 1 && plugin.disenchantReason != null && source.Magic.Sockets.Count == 2, "second stone remains selectable even when return capacity blocks confirm");

                int removeCalls = ShardSocketManager.RemoveCalls;
                Time.unscaledTime = 2; plugin.UpdateDisenchant();
                Program.Assert(ShardSocketManager.RemoveCalls == removeCalls, "unchanged removal modal reuses socket preview outside GUI");
                source.m_durability = source.GetMaxDurability() * 0.8f;
                Time.unscaledTime = 3; plugin.UpdateDisenchant();
                Program.Assert(Math.Abs(plugin.disenchantPlan.NormalItem.m_durability / plugin.disenchantPlan.NormalItem.GetMaxDurability() - 0.8f) < 0.0001f, "removal modal refreshes repaired item wear even when inventory is full");
                Program.Assert(ShardSocketManager.RemoveCalls == removeCalls + 1, "changed removal wear recomputes preview once");
                Localization.instance.Language = "tr";
                Localization.instance.Text["RareStone"] = "Nadir taş";
                Localization.instance.Text["$socket_damage"] = "Hasar +{0}";
                Localization.instance.Text["$socket_description"] = "Hasarı artırır.";
                Time.unscaledTime = 4; plugin.UpdateDisenchant();
                Program.Assert(plugin.removalStoneCards[0].Name.Contains("Nadir taş") && plugin.removalStoneCards[0].Title == "Hasar +10", "open removal modal refreshes cards after game language change");
                Program.Assert(plugin.disenchantComparison.Contains("Hasar +10") && API.GetMagicItemJson(source) == originalMagic, "localized comparison is refreshed");

                // Undo preview and actual undo share the same preparation method.
                source = RemovalItem(); var inventory = new Inventory(); inventory.Items.Add(source);
                InventoryOperation operation = null;
                DisenchantService.Apply(inventory, DisenchantService.Prepare(source), x => operation = x);
                string after = InventoryOperations.Signature(source);
                source.m_durability = source.GetMaxDurability() * 0.7f;
                var preview = InventoryOperations.PreviewUndo(operation);
                Program.Assert(Math.Abs(preview.m_durability / preview.GetMaxDurability() - 0.7f) < 0.0001f && !preview.m_shared.m_useDurability, "undo preview uses current wear and previous indestructibility");
                Program.Assert(InventoryOperations.Signature(source) == after && source.Magic == null && !operation.Undone, "undo preview does not apply old magic or consume receipt");
                InventoryOperations.Undo(inventory, operation);
                Program.Assert(API.GetMagicItemJson(source) == API.GetMagicItemJson(preview) && source.m_durability == preview.m_durability, "actual undo matches prepared preview magic and durability");

                source = RemovalItem(); source.Magic.Sockets[1].SourcePrefab = "OtherStone";
                inventory = new Inventory(); inventory.Items.Add(source);
                DisenchantService.Apply(inventory, DisenchantService.Prepare(source), x => operation = x);
                inventory.Items.RemoveAll(x => x != source);
                string reason = RejectionText(() => InventoryOperations.CheckUndo(inventory, operation));
                Program.Assert(reason.Contains("Nadir taş: gereken 1, mevcut 0, eksik 1") && reason.Contains("Other shardstone: gereken 1, mevcut 0, eksik 1"), "undo reports all missing stone types with exact required available and missing counts");
                inventory.Items.Add(operation.Returned[0].Clone());
                reason = RejectionText(() => InventoryOperations.CheckUndo(inventory, operation));
                Program.Assert(!reason.Contains("Nadir taş:") && reason.Contains("Other shardstone:"), "undo requirements shrink when one stone is restored");
                Player.m_localPlayer.Inventory = inventory;
                var undoPlugin = new Plugin { lastInventoryOperation = operation };
                undoPlugin.OpenInventoryUndo();
                Program.Assert(undoPlugin.showInventoryUndo && undoPlugin.inventoryUndoReason.Contains("Other shardstone:") && undoPlugin.inventoryUndoComparison.Contains("Nadirlik"), "blocked undo opens useful comparison and missing material details");
                source.m_durability = source.GetMaxDurability() * 0.4f;
                Time.unscaledTime = 6; undoPlugin.UpdateInventoryUndo();
                Program.Assert(undoPlugin.inventoryUndoPreviewState.Contains(source.m_durability.ToString("R", CultureInfo.InvariantCulture)), "open undo preview state tracks live repairs or wear");
                undoPlugin.DrawInventoryUndo(2);
                Program.Assert(source.Magic == null && inventory.Items.Count == 2 && !operation.Undone && GUI.enabled, "drawing blocked undo preserves items receipt and enabled state");
                inventory.Items.Add(operation.Returned[1].Clone());
                Time.unscaledTime = 7; undoPlugin.UpdateInventoryUndo();
                Program.Assert(undoPlugin.inventoryUndoReason == null, "restored required stone enables confirmation in open undo modal");
                undoPlugin.UndoInventoryOperation();
                Program.Assert(source.Magic.Sockets.Count == 2 && inventory.Items.Count == 1 && Math.Abs(source.m_durability / source.GetMaxDurability() - 0.4f) < 0.0001f, "undo after restoring missing material consumes both stones and preserves current wear");
            }
            finally
            {
                API.DisplayNameOverride = oldNames; Localization.instance.Language = oldLanguage;
                foreach (string key in new[] { "RareStone", "OtherStone", "$socket_damage", "$socket_description" }) Localization.instance.Text.Remove(key);
                if (oldDamage == null) MagicItemEffectDefinitions.AllDefinitions.Remove("Damage"); else MagicItemEffectDefinitions.AllDefinitions["Damage"] = oldDamage;
                ObjectDB.instance.Items.Remove("RareStone"); ObjectDB.instance.Items.Remove("OtherStone");
                GUILayout.ClickTooltip = null; GUI.enabled = true; Time.unscaledTime = 0;
                Player.m_localPlayer.Inventory = new Inventory();
            }
        }
    }
}
