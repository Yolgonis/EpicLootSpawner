using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value) => value != null; }
    public enum CursorLockMode { None, Locked, Confined }
    public static class Cursor { public static CursorLockMode lockState; public static bool visible; }
}
public class Player : UnityEngine.Object { public static Player m_localPlayer = new Player(); }
public class ZNetScene : UnityEngine.Object { public static ZNetScene instance = new ZNetScene(); }
public class ObjectDB : UnityEngine.Object { public static ObjectDB instance = new ObjectDB(); }
public class Localization { public static Localization instance = new Localization(); }

namespace EpicLootSpawner
{
    internal enum PickerKind { None, Effect, Stone, Special }
    public sealed partial class Plugin
    {
        private static bool IsOpen;
        private PickerKind picker;
        private readonly Queue<Action> pendingUiActions = new Queue<Action>();
        private void QueueAction(Action action) => pendingUiActions.Enqueue(action);
        private bool showPreview, showTransfer, showAppearance, showPresetManager, showCollection, showRecovery;
        private object recoveryCandidate, disenchantPlan;
        private bool showDisenchant, showInventoryUndo;
        private string pendingDelete, status, lastError, lastErrorDetail, lastOperation;
        private string OpenKeyName = "F8";
        private CursorLockMode previousLock;
        private bool previousVisible;
        private bool stopProduction;
        private bool failCatalog, failDraft, failRecord, failSave, failAppearance;
        private int records, saves, appearances, disposedThemes, resizeCancels;
        private void CancelWindowResize() => resizeCancels++;
        private class Log { public int Warnings; public void LogWarning(object value) => Warnings++; }
        private readonly Log Logger = new Log();
        private void RefreshCatalog() { if (failCatalog) throw new Exception("catalog"); status = "catalog ready"; }
        private void RestoreSessionDraft() { if (failDraft) throw new Exception("draft"); }
        private void RecordDesign() { records++; if (failRecord) throw new Exception("record"); }
        private void SaveSessionDraft() { saves++; if (failSave) throw new Exception("save"); }
        private void SaveAppearance() { appearances++; if (failAppearance) throw new Exception("appearance"); }
        private void DisposeTheme() => disposedThemes++;

        public static void Main()
        {
            int checks = 0;
            void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
            void Closed(string label) => Check(!IsOpen && Cursor.lockState == CursorLockMode.Locked && !Cursor.visible, label);
            var plugin = new Plugin(); Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            Player.m_localPlayer = null; plugin.Run(plugin.OpenPanel); Closed("no player leaves cursor/input unchanged");
            Player.m_localPlayer = new Player(); ZNetScene.instance = null; plugin.Run(plugin.OpenPanel); Closed("no world leaves panel closed");
            ZNetScene.instance = new ZNetScene(); ObjectDB.instance = null; plugin.Run(plugin.OpenPanel); Closed("no catalog leaves panel closed");
            ObjectDB.instance = new ObjectDB(); Localization.instance = null; plugin.Run(plugin.OpenPanel); Closed("no localization leaves panel closed");
            Localization.instance = new Localization(); plugin.failCatalog = true; plugin.Run(plugin.OpenPanel); Closed("catalog exception cannot lock input");
            Check(plugin.lastError == "catalog" && plugin.Logger.Warnings == 5, "open failures are logged");
            plugin.failCatalog = false; plugin.failDraft = true; plugin.Run(plugin.OpenPanel);
            Check(IsOpen && plugin.lastError == "draft", "invalid draft allows panel recovery");
            plugin.showRecovery = plugin.showCollection = plugin.showPresetManager = plugin.showAppearance = plugin.showTransfer = plugin.showPreview = true;
            plugin.recoveryCandidate = new object(); plugin.pendingDelete = "name";
            plugin.showInventoryUndo = true; plugin.showDisenchant = true; plugin.disenchantPlan = new object();
            plugin.picker = PickerKind.Effect;
            bool staleActionRan = false; plugin.QueueAction(() => staleActionRan = true);
            plugin.failRecord = plugin.failSave = plugin.failAppearance = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true; plugin.Close();
            Closed("record/save failures still restore cursor and input");
            Check(plugin.resizeCancels == 1, "closing cancels captured resize even when saving fails");
            Check(plugin.pendingUiActions.Count == 0 && !staleActionRan, "closing discards unexecuted GUI actions");
            Check(!plugin.showRecovery && !plugin.showCollection && !plugin.showPresetManager && !plugin.showAppearance && !plugin.showTransfer && !plugin.showPreview, "closing clears all modals");
            Check(!plugin.showInventoryUndo, "closing discards inventory undo confirmation");
            Check(plugin.picker == PickerKind.None, "closing dismisses the effect/stone/identity picker");
            Check(!plugin.showDisenchant && plugin.disenchantPlan == null, "closing discards disenchant confirmation");
            Check(plugin.recoveryCandidate == null && plugin.pendingDelete == null, "closing clears recovery/delete confirmation");
            Check(plugin.records == 1 && plugin.saves == 1 && plugin.appearances == 1, "all closing work attempted despite failures");
            plugin.Close(); Check(plugin.records == 1 && plugin.saves == 1, "closing already closed panel is idempotent");
            plugin.failDraft = plugin.failRecord = plugin.failSave = plugin.failAppearance = false;
            plugin.OpenPanel(); Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            plugin.QueueAction(() => staleActionRan = true);
            plugin.DeferPanelFailure(new Exception("GUI fault"));
            Check(IsOpen && plugin.pendingUiActions.Count == 1, "GUI fault is deferred until Update");
            plugin.DeferPanelFailure(new Exception("latest GUI fault"));
            Check(plugin.pendingUiActions.Count == 1, "repeated GUI faults queue one recovery");
            while (plugin.pendingUiActions.Count > 0) plugin.Run(plugin.pendingUiActions.Dequeue());
            Closed("deferred GUI fault restores input");
            Check(!staleActionRan && plugin.lastError == "latest GUI fault", "fault discards old actions and preserves diagnostic");
            Check(plugin.status.Contains("F8") && plugin.status.Contains("latest GUI fault"), "fault explains recovery");
            Check(plugin.disposedThemes == 1, "fault discards a possibly incomplete GUI theme");
            Check(plugin.stopProduction, "panel fault requests active production to stop");
            plugin.OpenPanel(); Check(IsOpen, "panel can reopen after fault"); plugin.Close();
            plugin.OpenKeyName = "F9";
            ObjectDB.instance = null; plugin.Run(plugin.OpenPanel);
            Check(plugin.lastError.Contains("F9") && !plugin.lastError.Contains("F8"), "not-ready message follows configured opening key");
            ObjectDB.instance = new ObjectDB(); plugin.OpenPanel();
            var detailed = new InvalidOperationException("action failed", new Exception("original cause"));
            plugin.FailPanel(detailed);
            Check(plugin.status.Contains("F9") && !plugin.status.Contains("F8"), "fault recovery follows configured opening key");
            Check(plugin.lastError == "action failed" && plugin.lastErrorDetail.Contains("InvalidOperationException") && plugin.lastErrorDetail.Contains("original cause"), "short status and full nested exception are retained separately");
            plugin.Run(() => throw new ArgumentException("next action"));
            Check(plugin.lastError == "next action" && plugin.lastErrorDetail.Contains("ArgumentException") && !plugin.lastErrorDetail.Contains("original cause"), "latest failed action replaces matching diagnostic details");
            Console.WriteLine("PASS: " + checks + " linked panel lifecycle checks (Unity/state fixtures)");
        }
    }
}
