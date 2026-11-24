using System;
using EpicLoot;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private InventoryOperation lastInventoryOperation;
        private bool showInventoryUndo;
        private string inventoryUndoReason, inventoryUndoTooltip, inventoryUndoComparison, inventoryUndoPreviewState;
        private int inventoryUndoTab;
        private float inventoryUndoDue;

        private void UpdateInventoryUndo()
        {
            if (lastInventoryOperation == null) return;
            var inventory = Player.m_localPlayer.GetInventory();
            if (!ReferenceEquals(inventory, lastInventoryOperation.Inventory))
            {
                lastInventoryOperation = null; showInventoryUndo = false; return;
            }
            if (!IsOpen || UnityEngine.Time.unscaledTime < inventoryUndoDue) return;
            inventoryUndoDue = UnityEngine.Time.unscaledTime + 0.5f;
            try
            {
                inventoryUndoReason = production != null ? "Önce devam eden üretimi durdur." : null;
                if (inventoryUndoReason == null) InventoryOperations.CheckUndo(inventory, lastInventoryOperation);
            }
            catch (Exception ex) { inventoryUndoReason = ex.Message; }
            if (!showInventoryUndo) return;
            try
            {
                string state = InventoryOperations.Signature(lastInventoryOperation.Target) + "|"
                    + lastInventoryOperation.Target.m_durability.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|"
                    + lastInventoryOperation.Target.GetMaxDurability().ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|" + Localization.instance.GetSelectedLanguage();
                if (state == inventoryUndoPreviewState) return;
                var candidate = InventoryOperations.PreviewUndo(lastInventoryOperation);
                string tooltip = Localization.instance.Localize(API.GetItemDisplayName(candidate)) + "\n\n" + candidate.GetTooltip();
                string comparison = DescribeComparison(lastInventoryOperation.Target, candidate);
                inventoryUndoTooltip = tooltip; inventoryUndoComparison = comparison; inventoryUndoPreviewState = state;
            }
            catch (Exception ex) { inventoryUndoReason = ex.Message; inventoryUndoPreviewState = null; }
        }

        private void OpenInventoryUndo()
        {
            if (production != null) throw new InvalidOperationException("Önce devam eden üretimi durdur.");
            if (lastInventoryOperation == null) throw new InvalidOperationException("Geri alınacak işlem yok.");
            inventoryUndoPreviewState = null; inventoryUndoTooltip = inventoryUndoComparison = ""; inventoryUndoTab = 0;
            showInventoryUndo = true;
            inventoryUndoDue = 0; UpdateInventoryUndo();
        }

        private void UndoInventoryOperation()
        {
            if (production != null) throw new InvalidOperationException("Önce devam eden üretimi durdur.");
            var operation = lastInventoryOperation;
            InventoryOperations.Undo(Player.m_localPlayer.GetInventory(), operation);
            // Clear immediately, before refreshing UI: even a refresh error must not
            // permit a second undo or discard the successful material transaction.
            lastInventoryOperation = null; showInventoryUndo = false;
            var prefab = operation.Target.m_dropPrefab;
            if (!prefab && EditingSource?.Item == operation.Target) prefab = EditingSource.Prefab;
            if (prefab) LoadInventoryItem(new InventoryEntry { Item = operation.Target, Prefab = prefab });
            status = operation.Label + " geri alındı. İade edilen " + operation.Returned.Count + " taş envanterden alındı.";
            inventoryDue = 0;
        }
    }
}
