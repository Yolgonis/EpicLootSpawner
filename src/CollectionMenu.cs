using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpicLoot;
using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private bool showCollection;
        private string exportPath = "", importPath = "", collectionStatus = "";
        private Vector2 collectionScroll;
        private List<ImportRow> importRows = new List<ImportRow>();
        private readonly Dictionary<ImportRow, string> importErrors = new Dictionary<ImportRow, string>();
        private void OpenCollection()
        {
            showCollection = true;
            if (exportPath.Length == 0) exportPath = Path.Combine(BepInEx.Paths.ConfigPath, "EpicLootSpawnerExports", "koleksiyon-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
            collectionStatus = "Kütüphanedeki ✓ kutuları dışa alınacak kayıtları seçer.";
        }
        private void ExportCollection()
        {
            if (!presets.Ready) throw new InvalidOperationException("Şablon deposu hazır değil.");
            string target = Path.GetFullPath(exportPath);
            if (!string.Equals(Path.GetExtension(target), ".json", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(".json uzantılı bir dosya adı yaz.");
            if (File.Exists(target)) throw new InvalidOperationException("Bu dosya zaten var. Farklı bir dosya adı yaz.");
            var entries = presets.Names.Where(x => exportSelection.Contains(x)).Select(x => new CollectionEntry { Name = x, Recipe = presets.Get(x), Tags = presets.GetTags(x) });
            string text = CollectionTransfer.Encode(entries, RarityCount);
            AtomicFile.Write(target, text);
            collectionStatus = exportSelection.Count + " kayıt dışa alındı: " + target;
            lastOperation = "Koleksiyon dışa alındı";
        }
        private void ReadCollection()
        {
            importRows.Clear(); importErrors.Clear();
            var rows = CollectionTransfer.Decode(LocalJson.Read(Path.GetFullPath(importPath)), RarityCount);
            var errors = new Dictionary<ImportRow, string>();
            foreach (var row in rows)
            {
                string error = row.Error;
                if (error == null) try { var candidate = new Spawner(); candidate.Import(row.Entry.Recipe); }
                    catch (Exception ex) { error = ex.Message; }
                errors[row] = error;
            }
            importRows = rows; importErrors.Clear();
            foreach (var pair in errors) importErrors[pair.Key] = pair.Value;
            RefreshImportRows();
            collectionStatus = rows.Count + " kayıt incelendi. Hatalı/çakışan kayıtlar seçilmez.";
        }
        private void RefreshImportRows()
            => CollectionTransfer.RefreshRows(importRows, presets, importErrors);
        private void ImportSelectedCollection()
        {
            if (!presets.Ready) throw new InvalidOperationException("Şablon deposu hazır değil.");
            RefreshImportRows();
            var chosen = importRows.Where(x => x.Selected && x.Error == null).Select(x => x.Entry).ToArray();
            if (chosen.Length == 0) throw new InvalidOperationException("Geçerli kayıtları seç.");
            foreach (var entry in chosen) { var candidate = new Spawner(); candidate.Import(entry.Recipe); }
            presets.ImportEntries(chosen);
            foreach (var row in importRows) row.Selected = false;
            RefreshImportRows();
            collectionStatus = chosen.Length + " kayıt alındı. Mevcut tasarım korundu.";
            lastOperation = "Koleksiyon içe alındı";
        }
        private void CollectionAction(Action action)
        {
            try { action(); }
            catch (Exception ex) { collectionStatus = ex.Message; RecordError(ex); Logger.LogWarning(ex); }
        }
        private void DrawCollection(int id)
        {
            bool enabled = GUI.enabled;
            GUILayout.Label("ŞABLON KOLEKSİYONU", heading);
            collectionScroll = GUILayout.BeginScrollView(collectionScroll, GUILayout.ExpandHeight(true));
            GUILayout.Label("Dışa alma • " + exportSelection.Count + " kayıt seçili", section);
            GUILayout.BeginHorizontal();
            GUI.enabled = enabled && presets.Ready;
            if (GUILayout.Button("Görünenleri seç")) QueueAction(() => { foreach (string name in visiblePresets) exportSelection.Add(name); });
            if (GUILayout.Button("Seçimi temizle")) QueueAction(() => exportSelection.Clear());
            GUI.enabled = enabled; GUILayout.EndHorizontal();
            GUILayout.Label("Yeni JSON dosyasının tam yolu", muted); exportPath = GUILayout.TextField(exportPath);
            GUI.enabled = enabled && presets.Ready && exportSelection.Count > 0;
            if (GUILayout.Button("Seçilenleri dosyaya yaz")) QueueAction(() => CollectionAction(ExportCollection));
            GUI.enabled = enabled;
            GUILayout.Space(8); GUILayout.Label("Dosyadan içe alma", section);
            GUILayout.Label("JSON dosyasının tam yolu", muted); importPath = GUILayout.TextField(importPath);
            if (GUILayout.Button("Dosyayı oku / önizle")) QueueAction(() => CollectionAction(ReadCollection));
            if (importRows.Count > 0)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Geçerli kayıtları seç")) QueueAction(() => { RefreshImportRows(); foreach (var row in importRows) row.Selected = row.Error == null; });
                if (GUILayout.Button("Seçimleri kaldır")) QueueAction(() => { foreach (var row in importRows) row.Selected = false; });
                GUILayout.EndHorizontal();
            }
            foreach (var row in importRows)
            {
                GUILayout.BeginVertical(card);
                if (row.Entry == null) GUILayout.Label("Okunamayan kayıt", section);
                else
                {
                    GUILayout.Label("Kayıt adı • Çakışmada yeniden adlandır", muted);
                    string name = GUILayout.TextField(row.Entry.Name ?? "");
                    if (name != row.Entry.Name) QueueAction(() => { row.Entry.Name = name; RefreshImportRows(); });
                    GUILayout.Label(SafeLabel(row.Entry.Recipe?.Prefab ?? "Tasarım eksik") + " • " + SafeLabel(string.Join(", ", row.Entry.Tags ?? Array.Empty<string>())), muted);
                }
                GUI.enabled = enabled && row.Error == null;
                bool selectedRow = GUILayout.Toggle(row.Selected, "Bu kaydı al");
                if (selectedRow != row.Selected) QueueAction(() => row.Selected = selectedRow);
                GUI.enabled = enabled;
                GUILayout.Label(row.Error == null ? "Geçerli • Tasarım üretilmez, şablon deposuna eklenir" : SafeLabel(row.Error), row.Error == null ? muted : errorHint);
                GUILayout.EndVertical();
            }
            GUI.enabled = enabled && presets.Ready && importRows.Any(x => x.Selected && x.Error == null);
            if (GUILayout.Button("SEÇİLEN KAYITLARI AL", primary)) QueueAction(() => CollectionAction(ImportSelectedCollection));
            GUI.enabled = enabled;
            GUILayout.Label(SafeLabel(collectionStatus), muted);
            GUILayout.EndScrollView();
            if (GUILayout.Button("Kütüphaneye dön", primary)) QueueAction(() => showCollection = false);
        }
    }
}
