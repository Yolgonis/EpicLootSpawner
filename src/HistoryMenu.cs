using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicLootSpawner
{
    internal sealed class DesignState
    {
        public Recipe Recipe;
        public string InventoryKey;
    }

    public sealed partial class Plugin
    {
        private readonly DesignHistory history = new DesignHistory();
        private string observedDesign;
        private float historyDue;
        private bool historyCanUndo, historyCanRedo;
        private string DesignSnapshot() => JsonConvert.SerializeObject(new DesignState { Recipe = builder.Draft(), InventoryKey = inventoryKey });
        private void RecordDesign()
        {
            observedDesign = DesignSnapshot();
            bool changed = history.Current != observedDesign;
            history.Record(observedDesign);
            if (changed) PruneInventorySources();
            historyCanUndo = history.CanUndo; historyCanRedo = history.CanRedo;
        }
        private void PruneInventorySources()
        {
            var keys = new HashSet<string>(history.Snapshots.Select(x => JsonConvert.DeserializeObject<DesignState>(x).InventoryKey).Where(x => x != null));
            if (inventoryKey != null) keys.Add(inventoryKey);
            foreach (string key in inventorySources.Keys.Where(x => !keys.Contains(x)).ToArray()) inventorySources.Remove(key);
        }
        private void QueueDesignAction(Action action) => QueueAction(() =>
        {
            RecordDesign();
            try { action(); }
            finally { RecordDesign(); }
        });
        private void ObserveDesign()
        {
            if (!IsOpen) return;
            string snapshot = DesignSnapshot();
            if (history.Current == null) { RecordDesign(); return; }
            if (snapshot != observedDesign) { observedDesign = snapshot; historyDue = Time.unscaledTime + 0.35f; }
            if (snapshot != history.Current && Time.unscaledTime >= historyDue) { history.Record(snapshot); PruneInventorySources(); }
            historyCanUndo = history.CanUndo || (history.Current != null && history.Current != snapshot);
            historyCanRedo = history.CanRedo && history.Current == snapshot;
        }
        private void RestoreHistory(bool undo)
        {
            RecordDesign();
            string snapshot = undo ? history.PeekUndo() : history.PeekRedo();
            var state = JsonConvert.DeserializeObject<DesignState>(snapshot);
            builder.RestoreDraft(state.Recipe);
            inventoryKey = state.InventoryKey;
            inventoryDue = 0;
            if (undo) history.Undo(); else history.Redo();
            observedDesign = snapshot;
            historyCanUndo = history.CanUndo; historyCanRedo = history.CanRedo;
            RefreshEffects();
            status = undo ? "Tasarım değişikliği geri alındı." : "Tasarım değişikliği tekrar uygulandı.";
        }
        private void DrawHistory()
        {
            bool enabled = GUI.enabled;
            GUILayout.BeginHorizontal();
            GUI.enabled = enabled && historyCanUndo;
            if (GUILayout.Button("← Geri", GUILayout.Width(80), GUILayout.Height(38))) QueueAction(() => RestoreHistory(true));
            GUI.enabled = enabled && historyCanRedo;
            if (GUILayout.Button("İleri →", GUILayout.Width(80), GUILayout.Height(38))) QueueAction(() => RestoreHistory(false));
            GUI.enabled = enabled;
            GUILayout.EndHorizontal();
        }
    }
}
