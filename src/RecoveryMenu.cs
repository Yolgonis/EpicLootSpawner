using System;
using System.IO;
using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private bool showRecovery;
        private int recoveryKind;
        private string recoveryStatus = "Yedeği incele veya geçerli kayıtları kurtar.";
        private RecoveryCandidate recoveryCandidate;
        private Vector2 recoveryScroll;
        private static readonly string[] RecoveryKinds = { "Şablonlar", "Favoriler", "Taslak" };
        private string RecoveryPath => Path.Combine(BepInEx.Paths.ConfigPath, recoveryKind == 0 ? "EpicLootSpawner.presets.json" : recoveryKind == 1 ? "EpicLootSpawner.favorites.json" : "EpicLootSpawner.draft.json");
        private void InspectRecovery(int source)
        {
            recoveryCandidate = null;
            try
            {
                string path = RecoveryPath;
                recoveryCandidate = RecoveryService.Inspect(path, source == 1 ? path + ".bak" : source == 2 ? path + ".v1.bak" : path, RecoveryKinds[recoveryKind], RarityCount, source == 0);
                recoveryStatus = "Önizleme hazır. " + recoveryCandidate.Kept + " kayıt alınacak; " + recoveryCandidate.Skipped + " geçersiz kayıt atlanacak.";
            }
            catch (Exception ex) { recoveryStatus = ex.Message; RecordError(ex); }
        }
        private void ApplyRecovery()
        {
            try
            {
                var candidate = recoveryCandidate;
                string preserved = RecoveryService.Restore(candidate);
                recoveryCandidate = null;
                if (candidate.Kind == "Şablonlar") presets.Load();
                else if (candidate.Kind == "Favoriler") { favorites.Load(); libraryDirty = true; }
                else
                {
                    loadedDraft = draftStore.Load(); restoreDraftPending = loadedDraft != null;
                    draftSaveBlocked = true; blockedRecipeSignature = Newtonsoft.Json.JsonConvert.SerializeObject(builder.Draft());
                    Close();
                    recoveryStatus = "Taslak geri yüklendi. Sonraki panel açılışında tasarıma alınacak.";
                }
                status = "Kayıt kurtarıldı. Eski dosya korundu: " + (preserved ?? "Önceki dosya yoktu");
                recoveryStatus = status; lastOperation = "Kayıt kurtarıldı";
            }
            catch (Exception ex) { recoveryStatus = ex.Message; RecordError(ex); Logger.LogWarning(ex); }
        }
        private void DrawRecovery(int id)
        {
            GUILayout.Label("YEDEK VE KAYIT KURTARMA", heading);
            recoveryScroll = GUILayout.BeginScrollView(recoveryScroll, GUILayout.ExpandHeight(true));
            int kind = GUILayout.SelectionGrid(recoveryKind, RecoveryKinds, 3);
            if (kind != recoveryKind) QueueAction(() => { recoveryKind = kind; recoveryCandidate = null; recoveryStatus = "İncelemek istediğin kaynağı seç."; });
            GUILayout.Label("Dosya: " + RecoveryPath, muted);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(".bak yedeğini incele")) QueueAction(() => InspectRecovery(1));
            bool enabled = GUI.enabled; GUI.enabled = enabled && recoveryKind != 2;
            if (GUILayout.Button("Dosyadaki geçerli kayıtları kurtar")) QueueAction(() => InspectRecovery(0));
            GUI.enabled = enabled; GUILayout.EndHorizontal();
            if (recoveryKind == 0 && GUILayout.Button("İlk v1 şablon yedeğini incele")) QueueAction(() => InspectRecovery(2));
            GUILayout.Label(SafeLabel(recoveryStatus));
            if (recoveryCandidate != null)
            {
                GUILayout.Label("Kaynak: " + recoveryCandidate.Source, muted);
                GUILayout.Label("Alınacak: " + recoveryCandidate.Kept + " • Atlanacak: " + recoveryCandidate.Skipped, section);
                GUILayout.Label(recoveryKind == 2
                    ? "Onayladığında mevcut dosya ayrıca .corrupt-… adıyla korunur. Panel kapanır; sonraki F8 açılışında kurtarılan taslak yüklenir."
                    : "Onayladığında mevcut dosya ayrıca .corrupt-… adıyla korunur. Tasarım ve asıl envanter eşyası değişmez.", muted);
                if (GUILayout.Button("ÖNİZLEMEYİ ONAYLA VE DOSYAYI GERİ YÜKLE", primary)) QueueAction(ApplyRecovery);
                if (GUILayout.Button("Önizlemeden vazgeç")) QueueAction(() => recoveryCandidate = null);
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Tasarıma dön", primary)) QueueAction(() => showRecovery = false);
        }
    }
}
