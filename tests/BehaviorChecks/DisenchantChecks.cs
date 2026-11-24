using System;
using System.Linq;
using EpicLoot;
using EpicLoot.MagicItemEffects;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicLoot.ShardStones
{
    // The installed API reconstructs from SourcePrefab, preserving rarity through
    // the shard prefab. Its signature and implementation were inspected locally.
    public static class ShardSocketManager
    {
        public static int RemoveCalls;
        public static ItemDrop.ItemData RemoveShard(ItemDrop.ItemData equipment, int index)
        {
            RemoveCalls++;
            var magic = equipment.Magic;
            if (magic == null || index < 0 || index >= magic.Sockets.Count) return null;
            var socket = magic.Sockets[index]; magic.Sockets.RemoveAt(index);
            // Fixture value change is deliberately observable: production must call
            // EpicLoot's RemoveShard rather than merely deleting a list entry.
            foreach (var remaining in magic.Sockets)
                remaining.Effect = new MagicItemEffect { EffectType = "Damage", EffectValue = 17 };
            API.ApplyMagicItemJson(equipment, JsonConvert.SerializeObject(magic));
            return ReconstructShardItem(socket);
        }
        public static ItemDrop.ItemData ReconstructShardItem(SocketedEffect socket)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(socket.SourcePrefab);
            if (prefab == null) return null;
            var item = prefab.Drop.m_itemData.Clone();
            item.m_dropPrefab = prefab; item.m_stack = 1;
            return item;
        }
    }
}

namespace EpicLootSpawner
{
    public partial class Plugin
    {
        private static void RejectRemoval(Action action, string label)
        {
            bool failed = false;
            try { action(); } catch (InvalidOperationException) { failed = true; }
            Program.Assert(failed, label);
        }

        private static ItemDrop.ItemData RemovalItem(bool stones = true)
        {
            var item = new ItemDrop.ItemData { m_quality = 30, m_variant = 7, m_gridPos = new Vector2 { x = 3, y = 2 } };
            item.m_shared.m_maxStackSize = 1;
            item.m_customData["other.mod"] = "preserved";
            item.m_customData[ExactWoodDrops.Marker] = "1";
            var magic = new MagicItem { Rarity = ItemRarity.Epic, DisplayName = "Enchanted", SocketCount = 3 };
            magic.Effects.Add(new MagicItemEffect { EffectType = MagicEffectType.Indestructible, EffectValue = 1 });
            if (stones)
            {
                magic.Sockets.Add(new SocketedEffect { SourcePrefab = "RareStone", Effect = new MagicItemEffect { EffectType = "Damage", EffectValue = 10 } });
                magic.Sockets.Add(new SocketedEffect { SourcePrefab = "RareStone", Effect = null });
            }
            API.ApplyMagicItemJson(item, JsonConvert.SerializeObject(magic));
            item.m_durability = item.GetMaxDurability() * 0.25f;
            return item;
        }

        public static void CheckDisenchant()
        {
            var stonePrefab = new GameObject { name = "RareStone", Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
            stonePrefab.Drop.m_itemData.m_shared.m_name = "RareStone";
            ObjectDB.instance.Items["RareStone"] = stonePrefab;
            foreach (bool cloneSync in new[] { false, true })
            {
                ItemDrop.ItemData.ForceCloneSync = cloneSync;
                var source = RemovalItem();
                var inventory = new Inventory { Capacity = 2 }; inventory.Items.Add(source);
                string oldMagic = API.GetMagicItemJson(source);
                var plan = DisenchantService.Prepare(source);
                Program.Assert(source.Magic.Sockets.Count == 2 && API.GetMagicItemJson(source) == oldMagic && !source.m_shared.m_useDurability, "removal preview never mutates source");
                Program.Assert(plan.NormalItem.Magic == null && plan.NormalItem.m_shared.m_useDurability && plan.NormalItem.m_quality == 30, "normal preview removes magic but keeps high quality and durability baseline");
                Program.Assert(plan.ReturnedStones.Count == 2 && plan.ReturnedStones.All(x => x.m_dropPrefab == stonePrefab && x.m_stack == 1), "both active and inert stones are reconstructed");
                DisenchantService.CheckSpace(inventory, plan);
                Program.Assert(inventory.Items.Count == 1, "capacity simulation does not give real stones");
                int returned = DisenchantService.Apply(inventory, plan);
                Program.Assert(returned == 2 && source.Magic == null && !source.m_customData.ContainsKey("magic"), "enchantment data fully removed");
                Program.Assert(source.m_quality == 30 && source.m_variant == 7 && source.m_gridPos.x == 3 && source.m_gridPos.y == 2 && inventory.Items[0] == source, "removal preserves original identity quality position and variant");
                Program.Assert(source.m_shared.m_useDurability && Math.Abs(source.m_durability / source.GetMaxDurability() - 0.25f) < 0.0001f, "removing indestructibility preserves wear ratio");
                Program.Assert(source.m_customData["other.mod"] == "preserved" && !source.m_customData.ContainsKey(ExactWoodDrops.Marker), "foreign custom data preserved and owned marker cleared");
                Program.Assert(inventory.Items.Single(x => x != source).m_stack == 2 && source.Magic == null, "same-color stones merge without duplication");
                RejectRemoval(() => DisenchantService.Apply(inventory, plan), "repeated confirmation cannot duplicate returned stones");
                RejectRemoval(() => DisenchantService.Prepare(source), "plain item has no removable enchantment");
                Program.Assert(inventory.Items.Single(x => x != source).m_stack == 2, "rejected repeat keeps exact return count");
            }
            ItemDrop.ItemData.ForceCloneSync = false;
            var target = RemovalItem();
            var full = new Inventory { Capacity = 1 }; full.Items.Add(target);
            string before = API.GetMagicItemJson(target);
            var fullPlan = DisenchantService.Prepare(target);
            RejectRemoval(() => DisenchantService.Apply(full, fullPlan), "full inventory blocks removal before any write");
            Program.Assert(API.GetMagicItemJson(target) == before && full.Items.Count == 1 && full.AddCalls == 0, "full inventory retains original enchantment and stones");

            var freeStack = stonePrefab.Drop.m_itemData.Clone(); freeStack.m_stack = 48;
            full.Items.Add(freeStack); full.Capacity = 2;
            Program.Assert(DisenchantService.Apply(full, fullPlan) == 2 && freeStack.m_stack == 50 && full.Items.Count == 2, "full slot layout can return into available stack space");

            target = RemovalItem(); before = API.GetMagicItemJson(target);
            var rollback = new Inventory { Capacity = 3, FailAt = 2 };
            var existing = stonePrefab.Drop.m_itemData.Clone(); existing.m_stack = 10;
            rollback.Items.Add(target); rollback.Items.Add(existing);
            var rollbackPlan = DisenchantService.Prepare(target);
            RejectRemoval(() => DisenchantService.Apply(rollback, rollbackPlan), "partial add failure rejected");
            Program.Assert(rollback.Items.Count == 2 && rollback.Items[1] == existing && existing.m_stack == 10 && API.GetMagicItemJson(target) == before, "failed stone return rolls back merged stacks and enchantment");
            rollback.FailAt = 0;
            Program.Assert(DisenchantService.Apply(rollback, rollbackPlan) == 2 && existing.m_stack == 12, "retry returns stones exactly once");

            target = RemovalItem(); before = API.GetMagicItemJson(target);
            var lateFailure = new Inventory { Capacity = 3, FailChangedAt = 3 }; lateFailure.Items.Add(target);
            var latePlan = DisenchantService.Prepare(target);
            RejectRemoval(() => DisenchantService.Apply(lateFailure, latePlan), "post-removal notification failure rejected");
            Program.Assert(lateFailure.Items.Count == 1 && API.GetMagicItemJson(target) == before && !target.m_shared.m_useDurability && target.m_customData["other.mod"] == "preserved", "late failure restores magic baseline foreign data and inventory");
            lateFailure.FailChangedAt = 0;
            Program.Assert(DisenchantService.Apply(lateFailure, latePlan) == 2 && target.m_shared.m_useDurability, "restored magic can subsequently be removed correctly");

            target = RemovalItem(false); var noStones = new Inventory { Capacity = 1 }; noStones.Items.Add(target);
            Program.Assert(DisenchantService.Apply(noStones, DisenchantService.Prepare(target)) == 0 && noStones.Items.Count == 1, "no-stone removal works with full inventory");
            target = RemovalItem(); var stale = new Inventory(); stale.Items.Add(target); var stalePlan = DisenchantService.Prepare(target);
            target.m_quality++;
            RejectRemoval(() => DisenchantService.Apply(stale, stalePlan), "changed source rejects stale preview");
            target.m_quality--; target.m_equipped = true;
            RejectRemoval(() => DisenchantService.Apply(stale, stalePlan), "equipped source rejects confirmation");
            RejectRemoval(() => DisenchantService.Prepare(target), "equipped item rejects opening");
            target.m_equipped = false; stale.Items.Clear();
            RejectRemoval(() => DisenchantService.Apply(stale, stalePlan), "missing source rejects confirmation");
            target.m_stack = 2;
            RejectRemoval(() => DisenchantService.Prepare(target), "stacked equipment rejects removal");
            target.m_stack = 1; target.Magic.Sockets[0].SourcePrefab = "MissingStone";
            RejectRemoval(() => DisenchantService.Prepare(target), "unknown stone aborts preview without losing stones");
            Program.Assert(target.Magic.Sockets.Count == 2, "failed reconstruction leaves all original sockets");
            var otherPrefab = new GameObject { name = "OtherStone", Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
            otherPrefab.Drop.m_itemData.m_shared.m_name = "OtherStone";
            ObjectDB.instance.Items["OtherStone"] = otherPrefab;
            target = RemovalItem(); target.Magic.Sockets[1].SourcePrefab = "OtherStone";
            var oneSlot = new Inventory { Capacity = 2 }; oneSlot.Items.Add(target);
            RejectRemoval(() => DisenchantService.Apply(oneSlot, DisenchantService.Prepare(target)), "two distinct stones cannot reuse one free slot");
            Program.Assert(oneSlot.Items.Count == 1 && target.Magic.Sockets.Count == 2, "collective space failure performs no real mutation");
            target = RemovalItem(); var duringReturn = new Inventory { Capacity = 3 }; duringReturn.Items.Add(target);
            var duringPlan = DisenchantService.Prepare(target);
            duringReturn.OnAdd = () => target.m_quality = 31;
            RejectRemoval(() => DisenchantService.Apply(duringReturn, duringPlan), "source change during return prevents removal");
            Program.Assert(duringReturn.Items.Count == 1 && target.Magic.Sockets.Count == 2 && target.m_quality == 31, "callback source change retained while returned stones roll back");
            var natural = new ItemDrop.ItemData(); natural.m_shared.m_useDurability = false;
            API.ApplyMagicItemJson(natural, JsonConvert.SerializeObject(new MagicItem { Effects = new System.Collections.Generic.List<MagicItemEffect> { new MagicItemEffect { EffectType = MagicEffectType.Indestructible, EffectValue = 1 } } }));
            var naturalInventory = new Inventory(); naturalInventory.Items.Add(natural);
            DisenchantService.Apply(naturalInventory, DisenchantService.Prepare(natural));
            Program.Assert(!natural.m_shared.m_useDurability && natural.Magic == null, "naturally nondurable equipment stays nondurable");
            ObjectDB.instance.Items.Remove("OtherStone");
            ObjectDB.instance.Items.Remove("RareStone");
        }
    }
}
