using System;
using EpicLoot;
using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private bool showTransfer;
        private string transferText = "", transferStatus = "";
        private Vector2 transferScroll;
        private int RarityCount => Enum.GetValues(typeof(ItemRarity)).Length;

        private void RunTransfer(Action action)
        {
            try { action(); lastOperation = transferStatus; }
            catch (Exception ex) { transferStatus = ex.Message; RecordError(ex); Logger.LogWarning(ex); }
        }

        private void DrawTransfer(int id)
        {
            GUILayout.Label("ŞABLON PAYLAŞIMI", heading);
            GUILayout.Label("Tasarımı kopyala veya paylaşılan metni yapıştır. İçe alma önce tasarım paneline yükler.", muted);
            GUILayout.BeginHorizontal();
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && PreviewValid;
            if (GUILayout.Button("Tasarımı kopyala")) QueueAction(() => RunTransfer(() =>
            {
                _ = builder.RequestedCount; builder.Build();
                transferText = RecipeTransfer.Encode(builder.Export(), RarityCount);
                GUIUtility.systemCopyBuffer = transferText;
                transferStatus = "Tasarım panoya kopyalandı. Bu metni paylaşabilirsin.";
            }));
            GUI.enabled = enabled;
            if (GUILayout.Button("Panodan yapıştır")) QueueAction(() => RunTransfer(() =>
            {
                string text = GUIUtility.systemCopyBuffer ?? "";
                if (text.Length > RecipeTransfer.MaxLength) throw new InvalidOperationException("Panodaki metin çok büyük.");
                transferText = text; transferStatus = "Metin alındı. Tasarıma yükle ile doğrula.";
            }));
            GUILayout.EndHorizontal();
            transferScroll = GUILayout.BeginScrollView(transferScroll, GUILayout.ExpandHeight(true));
            transferText = GUILayout.TextArea(transferText, RecipeTransfer.MaxLength, GUILayout.MinHeight(410));
            GUILayout.EndScrollView();
            GUILayout.Label(transferStatus, muted, GUILayout.Height(55));
            GUILayout.BeginHorizontal();
            GUI.enabled = enabled && !string.IsNullOrWhiteSpace(transferText);
            if (GUILayout.Button("Tasarıma yükle", primary))
            {
                string text = transferText;
                QueueAction(() => RunTransfer(() =>
                {
                    var recipe = RecipeTransfer.Decode(text, RarityCount);
                    // Import validates a detached candidate before changing the current design.
                    RecordDesign();
                    builder.Import(recipe); inventoryKey = null; RecordDesign();
                    RefreshEffects(); showTransfer = false;
                    status = "Paylaşılan şablon tasarıma yüklendi. Saklamak için Şablonlar sekmesinden kaydet.";
                }));
            }
            GUI.enabled = enabled;
            if (GUILayout.Button("Tasarıma dön")) QueueAction(() => showTransfer = false);
            GUILayout.EndHorizontal();
        }
    }
}
