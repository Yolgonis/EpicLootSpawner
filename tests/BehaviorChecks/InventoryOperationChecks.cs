using System;
using System.Linq;
using EpicLoot;
using EpicLoot.ShardStones;
using UnityEngine;

namespace EpicLootSpawner
{
    public partial class Plugin
    {
        private static GameObject StonePrefab(string name)
        {
            var prefab = new GameObject { name = name, Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
            prefab.Drop.m_itemData.m_shared.m_name = name;
            ObjectDB.instance.Items[name] = prefab;
            return prefab;
        }

        public static void CheckInventoryOperations()
        {
            var rare = StonePrefab("RareStone"); var other = StonePrefab("OtherStone");
            foreach (bool share in new[] { false, true })
            foreach (bool sync in new[] { false, true })
            {
                ItemDrop.ItemData.ShareCloneMagic = share; ItemDrop.ItemData.ForceCloneSync = sync;
                var source = RemovalItem();
                string original = API.GetMagicItemJson(source);
                var inventory = new Inventory { Capacity = 2 }; inventory.Items.Add(source);
                int removeCalls = ShardSocketManager.RemoveCalls;
                var plan = DisenchantService.Prepare(source, 0);
                Program.Assert(ShardSocketManager.RemoveCalls == removeCalls + 1, "single removal delegates to native socket API");
                Program.Assert(API.GetMagicItemJson(source) == original && source.Magic.Sockets.Count == 2, "single removal preview protects shared clone magic");
                var magic = plan.NormalItem.Magic;
                Program.Assert(magic.Sockets.Count == 1 && magic.Sockets[0].Effect.EffectValue == 17, "remaining socket recalculated by API");
                Program.Assert(magic.SocketCount == 3 && magic.Rarity == ItemRarity.Epic && magic.DisplayName == "Enchanted" && magic.Effects.Count == 1, "single removal retains socket capacity rarity name rolled effects");
                Program.Assert(plan.ReturnedStones.Count == 1 && plan.ReturnedStones[0].m_dropPrefab == rare, "single removal reconstructs only selected stone");
                InventoryOperation receipt = null;
                Program.Assert(DisenchantService.Apply(inventory, plan, x => receipt = x) == 1, "single removal returns exactly one stone");
                Program.Assert(source.Magic.Sockets.Count == 1 && source.Magic.SocketCount == 3 && inventory.Items.Count == 2, "single removal commits on original identity and leaves empty socket");
                Program.Assert(source.m_quality == 30 && source.m_variant == 7 && source.m_customData["other.mod"] == "preserved" && source.m_customData.ContainsKey(ExactWoodDrops.Marker), "single removal preserves quality variant foreign data and wood marker");
                RejectRemoval(() => DisenchantService.Apply(inventory, plan), "single removal rejects stale double confirmation");
                source.m_durability = source.GetMaxDurability() * 0.6f;
                source.m_gridPos = new Vector2 { x = 1, y = 4 };
                InventoryOperations.Undo(inventory, receipt);
                Program.Assert(API.GetMagicItemJson(source) == original && inventory.Items.Count == 1, "undo reinserts exact raw sockets and consumes returned stone");
                Program.Assert(Math.Abs(source.m_durability / source.GetMaxDurability() - 0.6f) < 0.0001f && source.m_gridPos.x == 1 && source.m_gridPos.y == 4, "undo keeps current wear and moved grid position");
                Program.Assert(receipt.Undone && !source.m_shared.m_useDurability, "undo receipt consumed and indestructibility baseline restored");
                RejectRemoval(() => InventoryOperations.Undo(inventory, receipt), "undo cannot run twice");
            }
            ItemDrop.ItemData.ShareCloneMagic = false; ItemDrop.ItemData.ForceCloneSync = false;

            var target = RemovalItem(); var full = new Inventory { Capacity = 1 }; full.Items.Add(target);
            string before = API.GetMagicItemJson(target);
            RejectRemoval(() => DisenchantService.Apply(full, DisenchantService.Prepare(target, 1)), "single removal full inventory rejected");
            Program.Assert(API.GetMagicItemJson(target) == before && full.AddCalls == 0, "blocked single removal preserves inert socket and all effects");
            RejectRemoval(() => DisenchantService.Prepare(target, 2), "single removal invalid socket index rejected");
            RejectRemoval(() => DisenchantService.Prepare(target, -2), "invalid negative socket index rejected");
            target.Magic.Sockets[1].SourcePrefab = "MissingStone";
            RejectRemoval(() => DisenchantService.Prepare(target, 1), "single removal missing prefab rejected before writing");
            Program.Assert(target.Magic.Sockets.Count == 2, "failed preview keeps sockets");

            target = RemovalItem(); var inventory2 = new Inventory { Capacity = 4 }; inventory2.Items.Add(target);
            var existing = rare.Drop.m_itemData.Clone(); existing.m_stack = 10;
            existing.m_customData["third.party"] = "stack metadata";
            inventory2.Items.Add(existing);
            InventoryOperation operation = null;
            before = API.GetMagicItemJson(target);
            DisenchantService.Apply(inventory2, DisenchantService.Prepare(target), x => operation = x);
            Program.Assert(existing.m_stack == 12, "native-style merge preserves destination metadata");
            var unrelated = new ItemDrop.ItemData { m_stack = 5 }; inventory2.Items.Add(unrelated);
            InventoryOperations.Undo(inventory2, operation);
            Program.Assert(existing.m_stack == 10 && inventory2.Items.Contains(unrelated) && unrelated.m_stack == 5, "undo debits merged destination and retains later inventory items");
            Program.Assert(API.GetMagicItemJson(target) == before && existing.m_customData["third.party"] == "stack metadata", "undo restores enchantment without overwriting foreign stack data");

            target = RemovalItem(); var missing = new Inventory(); missing.Items.Add(target);
            DisenchantService.Apply(missing, DisenchantService.Prepare(target), x => operation = x);
            var payout = missing.Items.Single(x => x != target);
            payout.m_stack = 1;
            RejectRemoval(() => InventoryOperations.Undo(missing, operation), "missing one returned stone blocks all undo writes");
            Program.Assert(target.Magic == null && payout.m_stack == 1 && !operation.Undone, "blocked undo keeps receipt and inventory unchanged");
            payout.m_stack = 2; payout.m_worldLevel = 2;
            RejectRemoval(() => InventoryOperations.Undo(missing, operation), "different world-level stone cannot replace returned stone");
            payout.m_worldLevel = 0; payout.m_customData["foreign"] = "changed";
            RejectRemoval(() => InventoryOperations.Undo(missing, operation), "changed stone metadata blocks undo");
            payout.m_customData.Clear(); target.m_equipped = true;
            RejectRemoval(() => InventoryOperations.Undo(missing, operation), "equipped source blocks undo");
            target.m_equipped = false; target.m_quality++;
            RejectRemoval(() => InventoryOperations.Undo(missing, operation), "externally changed quality blocks undo");
            target.m_quality--; target.m_customData["other.mod"] = "changed";
            RejectRemoval(() => InventoryOperations.Undo(missing, operation), "externally changed target data blocks undo");
            target.m_customData["other.mod"] = "preserved";
            RejectRemoval(() => InventoryOperations.Undo(new Inventory(), operation), "other inventory cannot undo old session");
            missing.Items.Remove(target);
            RejectRemoval(() => InventoryOperations.Undo(missing, operation), "missing target blocks undo");
            missing.Items.Add(target); InventoryOperations.Undo(missing, operation);
            Program.Assert(target.Magic != null && missing.Items.Count == 1, "restoring missing materials permits retry once");

            target = RemovalItem(); target.Magic.Sockets[1].SourcePrefab = "OtherStone";
            var failure = new Inventory(); failure.Items.Add(target);
            before = API.GetMagicItemJson(target);
            DisenchantService.Apply(failure, DisenchantService.Prepare(target), x => operation = x);
            var paidItems = failure.Items.ToArray();
            failure.FailRemoveAt = 2;
            RejectRemoval(() => InventoryOperations.Undo(failure, operation), "partial undo material removal failure rejected");
            Program.Assert(failure.Items.SequenceEqual(paidItems) && paidItems.Skip(1).All(x => x.m_stack == 1) && target.Magic == null && !operation.Undone, "partial undo failure restores exact item references and stacks");
            failure.FailRemoveAt = 0;
            InventoryOperations.Undo(failure, operation);
            Program.Assert(failure.Items.Count == 1 && API.GetMagicItemJson(target) == before, "retry after failed undo restores all sockets without losing stones");

            foreach (bool failApi in new[] { false, true })
            {
                target = RemovalItem(); var late = new Inventory(); late.Items.Add(target);
                DisenchantService.Apply(late, DisenchantService.Prepare(target), x => operation = x);
                var paid = late.Items.Single(x => x != target);
                if (failApi) { API.FailTarget = target; API.FailWrites = 1; }
                else late.FailChangedAt = late.ChangedCalls + 2;
                RejectRemoval(() => InventoryOperations.Undo(late, operation), "undo write/notification failure rejected");
                Program.Assert(target.Magic == null && target.m_shared.m_useDurability && paid.m_stack == 2 && late.Items.Contains(paid) && !operation.Undone, "late undo failure restores paid stones and unenchanted state");
                API.FailTarget = null; late.FailChangedAt = 0;
                InventoryOperations.Undo(late, operation);
                Program.Assert(target.Magic != null && late.Items.Count == 1, "late undo failure can retry safely");
            }

            var prefab = new GameObject { name = "Sword" };
            target = new ItemDrop.ItemData { m_dropPrefab = prefab };
            var plugin = EditingPlugin(target, DesignedItem(prefab, true, 30));
            plugin.ApplyInventoryEdit(); var firstReceipt = plugin.lastInventoryOperation;
            Program.Assert(firstReceipt != null && firstReceipt.Returned.Count == 0, "inventory apply creates undo receipt");
            plugin.builder.Designed = DesignedItem(prefab, false, 4); plugin.ApplyInventoryEdit();
            Program.Assert(plugin.lastInventoryOperation != firstReceipt, "new apply replaces previous single-level receipt");
            plugin.UndoInventoryOperation();
            Program.Assert(target.m_quality == 30 && target.HasMagicEffect(MagicEffectType.Indestructible) && plugin.lastInventoryOperation == null, "plugin undo restores previous applied state and clears receipt");
            plugin.builder.Designed = DesignedItem(prefab, false, 5); plugin.ApplyInventoryEdit();
            var retainedReceipt = plugin.lastInventoryOperation;
            plugin.builder.Designed = DesignedItem(prefab, true, 7);
            API.FailTarget = target; API.FailWrites = 1;
            RejectRemoval(plugin.ApplyInventoryEdit, "failed new apply rejected"); API.FailTarget = null;
            Program.Assert(plugin.lastInventoryOperation == retainedReceipt && target.m_quality == 5, "failed new apply retains previous undo receipt");
            plugin.UndoInventoryOperation();
            Program.Assert(target.m_quality == 30 && target.HasMagicEffect(MagicEffectType.Indestructible), "previous receipt remains usable after apply rollback");

            var single = RemovalItem(); var singleInventory = new Inventory(); singleInventory.Items.Add(single);
            string singleBefore = API.GetMagicItemJson(single);
            var singlePlan = DisenchantService.Prepare(single, 1);
            API.FailTarget = single; API.FailWrites = 1;
            RejectRemoval(() => DisenchantService.Apply(singleInventory, singlePlan), "single stone write failure rejected"); API.FailTarget = null;
            Program.Assert(singleInventory.Items.Count == 1 && API.GetMagicItemJson(single) == singleBefore, "single stone failure restores full original enchantment and removes payout");
            DisenchantService.Apply(singleInventory, singlePlan);
            Program.Assert(single.Magic.Sockets.Count == 1 && singleInventory.Items.Single(x => x != single).m_stack == 1, "single stone write failure can retry without duplicate payout");

            var retained = new Plugin { lastInventoryOperation = firstReceipt };
            Player.m_localPlayer.Inventory = new Inventory(); retained.UpdateInventoryUndo();
            Program.Assert(retained.lastInventoryOperation == null && !retained.showInventoryUndo, "changing character inventory clears session receipt");
            ObjectDB.instance.Items.Remove("RareStone"); ObjectDB.instance.Items.Remove("OtherStone");
        }
    }
}
