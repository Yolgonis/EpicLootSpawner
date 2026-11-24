using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EpicLoot;
using EpicLoot.MagicItemEffects;
using Newtonsoft.Json;

namespace EpicLootSpawner
{
    // A single session-local receipt. Inventory snapshots are used only during a
    // transaction failure; undo must never overwrite the player's other items.
    internal sealed class InventoryOperation
    {
        public Inventory Inventory;
        public ItemDrop.ItemData Target, Before;
        public string After, Label;
        public bool Undone;
        public readonly Dictionary<string, ItemDrop.ItemData> MaterialKinds = new Dictionary<string, ItemDrop.ItemData>();
        public readonly List<KeyValuePair<string, int>> MaterialDebits = new List<KeyValuePair<string, int>>();
        public readonly List<ItemDrop.ItemData> Returned = new List<ItemDrop.ItemData>();
    }

    internal static class InventoryOperations
    {
        public static string Signature(ItemDrop.ItemData item) => Signature(item, item.m_stack);

        private static string Signature(ItemDrop.ItemData item, int stack)
            => JsonConvert.SerializeObject(new object[] { item.m_dropPrefab?.name, item.m_shared.m_name,
                item.m_quality, item.m_variant, item.m_worldLevel, stack, API.GetMagicItemJson(item),
                item.m_customData.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new[] { x.Key, x.Value }).ToArray() });

        private static string StoneKey(ItemDrop.ItemData item)
        {
            // Stack quantity and grid location can change after a return. Other
            // metadata must match: spending an unrelated stone is not acceptable.
            return Signature(item, 1);
        }

        public static void Write(ItemDrop.ItemData target, ItemDrop.ItemData state, float durability)
        {
            var baseline = ItemCopies.Baseline(state);
            string magic = API.GetMagicItemJson(state);
            Indestructible.Revert(target);
            target.RemoveMagicItem();
            target.m_shared = baseline;
            target.m_quality = state.m_quality;
            try
            {
                if (!string.IsNullOrEmpty(magic) && !API.ApplyMagicItemJson(target, magic))
                    throw new InvalidOperationException("Büyü verisi uygulanamadı.");
            }
            finally
            {
                target.m_customData = new Dictionary<string, string>(state.m_customData);
                target.m_durability = durability;
            }
        }

        public static void CheckSpace(Inventory inventory, IEnumerable<ItemDrop.ItemData> returned)
        {
            var stones = returned.ToArray();
            if (stones.Length == 0) return;
            var trial = new Inventory(inventory.GetName(), inventory.GetBkg(), inventory.GetWidth(), inventory.GetHeight());
            foreach (var item in inventory.GetAllItems()) trial.GetAllItems().Add(ItemCopies.Clone(item));
            foreach (var stone in stones)
                if (!trial.AddItem(ItemCopies.Clone(stone)))
                    throw new InvalidOperationException("Taşları iade etmek için envanterde yeterli yer yok. Önce yer aç.");
        }

        private static void CheckTarget(Inventory inventory, ItemDrop.ItemData target, string signature)
        {
            if (!inventory.GetAllItems().Contains(target)) throw new InvalidOperationException("Eşya artık envanterde değil. Tekrar seç.");
            if (target.m_equipped) throw new InvalidOperationException("Önce kullanılan eşyayı çıkar.");
            if (target.m_stack != 1 || Signature(target) != signature)
                throw new InvalidOperationException("Asıl eşya değişti. İşlem önizlemesini yeniden aç.");
        }

        private static void Transaction(Inventory inventory, ItemDrop.ItemData target, Action materialChange, Action check, Action write)
        {
            var items = inventory.GetAllItems().ToArray();
            var stacks = items.Select(x => x.m_stack).ToArray();
            var positions = items.Select(x => x.m_gridPos).ToArray();
            var before = ItemCopies.Clone(target);
            bool touched = false;
            try
            {
                materialChange();
                check();
                touched = true;
                write();
                inventory.Changed();
            }
            catch (Exception operationError)
            {
                try
                {
                    inventory.GetAllItems().Clear(); inventory.GetAllItems().AddRange(items);
                    for (int i = 0; i < items.Length; i++) { items[i].m_stack = stacks[i]; items[i].m_gridPos = positions[i]; }
                    if (touched) Write(target, before, before.m_durability);
                    inventory.Changed();
                }
                catch (Exception rollbackError) { throw new AggregateException("İşlem ve geri yükleme sırasında hata oluştu.", operationError, rollbackError); }
                throw;
            }
        }

        public static InventoryOperation Apply(Inventory inventory, ItemDrop.ItemData target, ItemDrop.ItemData candidate,
            IEnumerable<ItemDrop.ItemData> returned, string label)
        {
            string signature = Signature(target);
            CheckTarget(inventory, target, signature);
            var operation = new InventoryOperation { Inventory = inventory, Target = target,
                Before = ItemCopies.Clone(target), Label = label };
            operation.Returned.AddRange(returned.Select(ItemCopies.Clone));
            CheckSpace(inventory, operation.Returned);
            var initialStacks = inventory.GetAllItems().ToDictionary(x => x, x => x.m_stack);
            Transaction(inventory, target, () =>
            {
                foreach (var stone in operation.Returned)
                    if (!inventory.AddItem(ItemCopies.Clone(stone)))
                        throw new InvalidOperationException("Taş iadesi tamamlanamadı. İşlem geri alındı.");
                // Valheim merges by shared name, quality and world level, even when
                // a destination stack has different custom metadata. Debit the actual
                // destination kind on undo, rather than assuming a new stack was made.
                foreach (var item in inventory.GetAllItems())
                {
                    if (item == target) continue;
                    int added = item.m_stack - (initialStacks.TryGetValue(item, out int oldStack) ? oldStack : 0);
                    if (added > 0)
                    {
                        string key = StoneKey(item);
                        operation.MaterialDebits.Add(new KeyValuePair<string, int>(key, added));
                        if (!operation.MaterialKinds.ContainsKey(key)) operation.MaterialKinds[key] = ItemCopies.Clone(item);
                    }
                }
                if (operation.MaterialDebits.Sum(x => x.Value) != operation.Returned.Sum(x => x.m_stack))
                    throw new InvalidOperationException("Taş iadesi sırasında envanter değişti. İşlem geri alındı.");
            }, () => CheckTarget(inventory, target, signature), () => Write(target, candidate, candidate.m_durability));
            operation.After = Signature(target);
            return operation;
        }

        private static List<KeyValuePair<ItemDrop.ItemData, int>> UndoMaterials(Inventory inventory, InventoryOperation operation)
        {
            if (operation == null || operation.Undone) throw new InvalidOperationException("Geri alınacak işlem yok.");
            if (!ReferenceEquals(inventory, operation.Inventory)) throw new InvalidOperationException("İşlem başka bir oyun oturumuna ait.");
            CheckTarget(inventory, operation.Target, operation.After);
            var result = new List<KeyValuePair<ItemDrop.ItemData, int>>();
            var missing = new StringBuilder();
            foreach (var group in operation.MaterialDebits.GroupBy(x => x.Key))
            {
                int needed = group.Sum(x => x.Value), remaining = needed;
                foreach (var item in inventory.GetAllItems())
                {
                    if (item == operation.Target || item.m_equipped || StoneKey(item) != group.Key) continue;
                    int count = Math.Min(item.m_stack, remaining);
                    if (count > 0) result.Add(new KeyValuePair<ItemDrop.ItemData, int>(item, count));
                    remaining -= count;
                    if (remaining == 0) break;
                }
                if (remaining > 0)
                {
                    string name = operation.MaterialKinds.TryGetValue(group.Key, out var kind)
                        ? Localization.instance.Localize(API.GetItemDisplayName(kind)) : "İade edilen taş";
                    missing.AppendLine(name + ": gereken " + needed + ", mevcut " + (needed - remaining) + ", eksik " + remaining);
                }
            }
            if (missing.Length > 0) throw new InvalidOperationException("Geri almak için eksik taşları envantere getir:\n" + missing.ToString().TrimEnd());
            return result;
        }

        public static void CheckUndo(Inventory inventory, InventoryOperation operation) => UndoMaterials(inventory, operation);

        public static ItemDrop.ItemData PreviewUndo(InventoryOperation operation)
        {
            if (operation == null || operation.Undone) throw new InvalidOperationException("Geri alınacak işlem yok.");
            var candidate = ItemCopies.Clone(operation.Before);
            string magic = API.GetMagicItemJson(operation.Before);
            if (!string.IsNullOrEmpty(magic) && !API.ApplyMagicItemJson(candidate, magic))
                throw new InvalidOperationException("Geri alma önizlemesi hazırlanamadı.");
            float ratio = Rules.DurabilityRatio(operation.Target.m_durability, operation.Target.GetMaxDurability());
            candidate.m_durability = ratio * candidate.GetMaxDurability();
            candidate.m_gridPos = operation.Target.m_gridPos;
            return candidate;
        }

        public static void Undo(Inventory inventory, InventoryOperation operation)
        {
            var materials = UndoMaterials(inventory, operation);
            var materialKeys = materials.ToDictionary(x => x.Key, x => StoneKey(x.Key));
            var candidate = PreviewUndo(operation);
            Transaction(inventory, operation.Target, () =>
            {
                foreach (var material in materials)
                {
                    if (!inventory.GetAllItems().Contains(material.Key) || material.Key.m_stack < material.Value || StoneKey(material.Key) != materialKeys[material.Key])
                        throw new InvalidOperationException("Taş yığını geri alma sırasında değişti.");
                    if (!inventory.RemoveItem(material.Key, material.Value))
                        throw new InvalidOperationException("İade edilen taşlar alınamadı. Geri alma tamamlanmadı.");
                }
            }, () => CheckTarget(inventory, operation.Target, operation.After), () =>
            {
                Write(operation.Target, candidate, candidate.m_durability);
            });
            operation.Undone = true;
        }
    }
}
