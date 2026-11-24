using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private Vector2 inventoryUndoScroll;

        private void DrawInventoryUndo(int id)
        {
            GUILayout.Label("SON ENVANTER İŞLEMİNİ GERİ AL", heading);
            var operation = lastInventoryOperation;
            GUILayout.Label(operation?.Label ?? "Geri alınacak işlem yok.", section);
            GUILayout.Label("Bu oturumdaki son işlem geri alınır. İade edilen taşlar envanterden alınır; mevcut konum ve dayanıklılık oranı korunur.", muted);
            int next = GUILayout.SelectionGrid(inventoryUndoTab, new[] { "Değişiklikler", "Geri yüklenecek eşya", "Gerekli taşlar" }, 3, tabs, GUILayout.Height(36));
            if (next != inventoryUndoTab) QueueAction(() => { inventoryUndoTab = next; inventoryUndoScroll = Vector2.zero; });
            inventoryUndoScroll = GUILayout.BeginScrollView(inventoryUndoScroll, GUILayout.ExpandHeight(true));
            if (operation != null)
            {
                if (inventoryUndoTab == 0) GUILayout.Label(inventoryUndoComparison);
                else if (inventoryUndoTab == 1) GUILayout.Label(inventoryUndoTooltip);
                else
                {
                    DrawReturnedStones(operation.Returned);
                    if (operation.Returned.Count == 0) GUILayout.Label("Bu işlemi geri almak için taş gerekmez.", muted);
                    GUILayout.Label("Taşlar envanterde aynı özelliklerle bulunmalı. Eksik veya değiştirilmiş taşlar aşağıda belirtilir.", muted);
                }
            }
            if (inventoryUndoReason != null) GUILayout.Label(SafeLabel(inventoryUndoReason), errorHint);
            GUILayout.EndScrollView();
            GUILayout.Label("Geri alma kaydı dünyadan/karakterden çıkınca temizlenir. Tasarım geçmişi ayrı çalışır.", muted);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && operation != null && inventoryUndoReason == null;
            if (GUILayout.Button("İşlemi geri al", primary, GUILayout.Height(46))) QueueDesignAction(UndoInventoryOperation);
            GUI.enabled = enabled;
            if (GUILayout.Button("Vazgeç", GUILayout.Height(36))) QueueAction(() => showInventoryUndo = false);
        }
    }
}
