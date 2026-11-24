using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot;
using EpicLoot.MagicItemEffects;
using EpicLoot.ShardStones;

namespace EpicLootSpawner
{
    internal sealed class DisenchantPlan
    {
        public ItemDrop.ItemData Source, NormalItem;
        public string Fingerprint;
        public int SocketIndex = -1;
        public readonly List<ItemDrop.ItemData> ReturnedStones = new List<ItemDrop.ItemData>();
    }

    internal static class DisenchantService
    {
        private static string Fingerprint(ItemDrop.ItemData item)
            => InventoryOperations.Signature(item);

        private static void RemoveMagic(ItemDrop.ItemData item)
        {
            var baseline = ItemCopies.Baseline(item);
            Indestructible.Revert(item);
            item.RemoveMagicItem();
            item.m_shared = baseline;
            item.m_customData.Remove(ExactWoodDrops.Marker);
            if (item.GetMagicItem() != null) throw new InvalidOperationException("Büyü verisi kaldırılamadı.");
        }

        public static DisenchantPlan Prepare(ItemDrop.ItemData source, int socketIndex = -1)
        {
            if (source == null || source.m_shared == null) throw new InvalidOperationException("Önce envanterden bir eşya seç.");
            if (source.m_equipped) throw new InvalidOperationException("Büyüyü kaldırmadan önce kullanılan eşyayı çıkar.");
            if (source.m_stack != 1) throw new InvalidOperationException("Yığın halindeki eşyanın büyüsü kaldırılamaz.");
            var magic = source.GetMagicItem();
            if (magic == null) throw new InvalidOperationException("Bu eşyada kaldırılacak büyü yok.");
            if (socketIndex < -1 || socketIndex >= magic.Sockets.Count)
                throw new InvalidOperationException("Seçili taş artık sokette değil. Yeniden seç.");
            var plan = new DisenchantPlan { Source = source, Fingerprint = Fingerprint(source), SocketIndex = socketIndex };
            float ratio = Rules.DurabilityRatio(source.m_durability, source.GetMaxDurability());
            plan.NormalItem = ItemCopies.Clone(source);
            if (socketIndex >= 0)
            {
                // EpicLoot's clone hooks can share the MagicItem instance. Replace it
                // with independently deserialized data before RemoveShard mutates it.
                if (!API.ApplyMagicItemJson(plan.NormalItem, API.GetMagicItemJson(source)))
                    throw new InvalidOperationException("Taş çıkarma önizlemesi hazırlanamadı.");
                var stone = ShardSocketManager.RemoveShard(plan.NormalItem, socketIndex);
                if (stone == null || stone.m_shared == null || stone.m_stack != 1)
                    throw new InvalidOperationException("Seçili taş geri oluşturulamadı. İşlem yapılmadı.");
                plan.ReturnedStones.Add(stone);
            }
            else
            {
                foreach (var socket in magic.Sockets)
                {
                    var stone = ShardSocketManager.ReconstructShardItem(socket);
                    if (stone == null || stone.m_shared == null || stone.m_stack != 1)
                        throw new InvalidOperationException("Takılı taş geri oluşturulamadı. Büyü kaldırılmadı.");
                    plan.ReturnedStones.Add(stone);
                }
                RemoveMagic(plan.NormalItem);
            }
            plan.NormalItem.m_durability = ratio * plan.NormalItem.GetMaxDurability();
            return plan;
        }

        public static void CheckSource(Inventory inventory, DisenchantPlan plan)
        {
            if (plan == null || !inventory.GetAllItems().Contains(plan.Source))
                throw new InvalidOperationException("Eşya artık envanterde değil. Tekrar seç.");
            if (plan.Fingerprint != Fingerprint(plan.Source))
                throw new InvalidOperationException("Asıl eşya değişti. Büyü kaldırma önizlemesini yeniden aç.");
            if (plan.Source.m_equipped) throw new InvalidOperationException("Büyüyü kaldırmadan önce kullanılan eşyayı çıkar.");
        }

        public static void CheckSpace(Inventory inventory, DisenchantPlan plan)
        {
            CheckSource(inventory, plan);
            if (plan.ReturnedStones.Count == 0) return;
            InventoryOperations.CheckSpace(inventory, plan.ReturnedStones);
        }

        public static int Apply(Inventory inventory, DisenchantPlan plan, Action<InventoryOperation> committed = null)
        {
            CheckSource(inventory, plan);
            // Rebuild from the live item so repairs/wear since the preview are retained.
            var current = Prepare(plan.Source, plan.SocketIndex);
            var operation = InventoryOperations.Apply(inventory, current.Source, current.NormalItem,
                current.ReturnedStones, current.SocketIndex >= 0 ? "Taş çıkarma" : "Büyü kaldırma");
            committed?.Invoke(operation);
            return current.ReturnedStones.Count;
        }
    }

    public sealed partial class Plugin
    {
        private bool showDisenchant;
        private DisenchantPlan disenchantPlan;
        private string disenchantReason, disenchantTooltip, disenchantComparison, disenchantPreviewState;
        private SocketCard[] removalStoneCards = Array.Empty<SocketCard>();
        private int removalTab;
        private float disenchantDue;

        private void OpenDisenchant() => OpenRemoval(-1);

        private void OpenRemoval(int socketIndex)
        {
            string reason = EditBlockedReason();
            if (reason != null) throw new InvalidOperationException(reason);
            RefreshRemovalPresentation(DisenchantService.Prepare(EditingSource.Item, socketIndex));
            removalTab = socketIndex >= 0 ? 0 : 1;
            showDisenchant = true;
            disenchantDue = 0;
            UpdateDisenchant();
        }

        private static string RemovalPreviewState(DisenchantPlan plan)
            => plan.Source.m_durability.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|"
                + plan.Source.GetMaxDurability().ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|" + Localization.instance.GetSelectedLanguage();

        private void RefreshRemovalPresentation(DisenchantPlan plan)
        {
            // Build everything before publishing a new selection, so a failed native
            // tooltip cannot pair an old preview with a different selected socket.
            string tooltip = Localization.instance.Localize(API.GetItemDisplayName(plan.NormalItem)) + "\n\n" + plan.NormalItem.GetTooltip();
            string comparison = DescribeComparison(plan.Source, plan.NormalItem);
            var cards = plan.Source.GetMagicItem().Sockets.Select(SocketCard.From).ToArray();
            disenchantTooltip = tooltip; disenchantComparison = comparison; removalStoneCards = cards;
            disenchantPlan = plan; disenchantPreviewState = RemovalPreviewState(plan);
        }

        private void UpdateDisenchant()
        {
            if (!showDisenchant || UnityEngine.Time.unscaledTime < disenchantDue) return;
            disenchantDue = UnityEngine.Time.unscaledTime + 0.5f;
            try
            {
                disenchantReason = EditBlockedReason();
                if (disenchantReason == null)
                {
                    var inventory = Player.m_localPlayer.GetInventory();
                    DisenchantService.CheckSource(inventory, disenchantPlan);
                    if (RemovalPreviewState(disenchantPlan) != disenchantPreviewState)
                        RefreshRemovalPresentation(DisenchantService.Prepare(disenchantPlan.Source, disenchantPlan.SocketIndex));
                    DisenchantService.CheckSpace(inventory, disenchantPlan);
                }
            }
            catch (Exception ex) { disenchantReason = ex.Message; }
        }

        private void ApplyDisenchant()
        {
            string reason = EditBlockedReason();
            if (reason != null) throw new InvalidOperationException(reason);
            var source = EditingSource;
            if (disenchantPlan == null || source.Item != disenchantPlan.Source)
                throw new InvalidOperationException("Önizleme seçili eşyaya ait değil. Yeniden aç.");
            int stones = DisenchantService.Apply(Player.m_localPlayer.GetInventory(), disenchantPlan, operation => lastInventoryOperation = operation);
            bool singleStone = disenchantPlan.SocketIndex >= 0;
            inventoryUndoDue = 0;
            showDisenchant = false; disenchantPlan = null;
            source.Fingerprint = ItemFingerprint(source.Item);
            LoadInventoryItem(new InventoryEntry { Item = source.Item, Prefab = source.Prefab });
            status = (singleStone ? "Seçili taş çıkarıldı; büyü ve boş soket korundu. " : "Büyü kaldırıldı. ") + stones + " taş envantere iade edildi. Eşya seviyesi ve dayanıklılık oranı korundu.";
        }
    }
}
