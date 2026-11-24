using System;
using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private void OpenPanel()
        {
            if (!Player.m_localPlayer || !ZNetScene.instance) throw new InvalidOperationException("Önce dünyaya gir; oyuncu henüz hazır değil.");
            if (!ObjectDB.instance || Localization.instance == null) throw new InvalidOperationException("Eşya veya dil veritabanı henüz hazır değil. " + OpenKeyName + " ile tekrar dene.");
            // A failed catalog must not leave TakeInput disabled behind an unusable window.
            RefreshCatalog();
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            IsOpen = true;
            Run(RestoreSessionDraft);
        }

        private void Close()
        {
            if (!IsOpen) return;
            try { Run(RecordDesign); }
            finally
            {
                CancelWindowResize();
                IsOpen = false;
                showPreview = showTransfer = showAppearance = false;
                picker = PickerKind.None;
                showPresetManager = showCollection = showRecovery = false;
                recoveryCandidate = null; pendingDelete = null;
                showInventoryUndo = false;
                showDisenchant = false; disenchantPlan = null;
                pendingUiActions.Clear();
                Cursor.lockState = previousLock; Cursor.visible = previousVisible;
            }
            Run(SaveSessionDraft);
            Run(SaveAppearance);
        }

        private void DeferPanelFailure(Exception error)
        {
            // GUI events can happen several times before Update. Keep only the recovery action.
            pendingUiActions.Clear();
            QueueAction(() => FailPanel(error));
        }

        private void FailPanel(Exception error)
        {
            stopProduction = true;
            Close();
            Run(DisposeTheme);
            RecordError(error);
            status = "Spawner paneli bir hata nedeniyle kapatıldı. " + OpenKeyName + " ile tekrar açabilirsin. " + error.Message;
            Logger.LogWarning(error);
        }

        private void RecordError(Exception error)
        {
            lastError = error.Message;
            lastErrorDetail = error.ToString();
        }

        private void Run(Action action)
        {
            string previousStatus = status;
            try { action(); if (status != previousStatus && !string.IsNullOrEmpty(status)) lastOperation = status; }
            catch (Exception ex) { status = ex.Message; RecordError(ex); Logger.LogWarning(ex); }
        }
    }
}
