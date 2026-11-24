using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EpicLoot;

namespace EpicLootSpawner
{
    internal sealed class PresetCard
    {
        public string Name, Summary;
        public GameObject Prefab;
    }
    public sealed partial class Plugin
    {
        private string presetSearch = "", presetTagFilter = "", presetTags = "", selectedPreset = "", pendingDelete;
        private bool showPresetManager;
        private Vector2 presetManagerScroll;
        private readonly PresetQuery presetQuery = new PresetQuery();
        private readonly Dictionary<string, PresetCard> presetCards = new Dictionary<string, PresetCard>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> exportSelection = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string[] visiblePresets = Array.Empty<string>();
        private int presetRevision = -1;
        private string presetLanguage;

        private void UpdatePresetViews()
        {
            if (!IsOpen || presets == null) return;
            string language = Localization.instance.GetSelectedLanguage();
            if (presetRevision != presets.Revision || presetLanguage != language)
            {
                presetRevision = presets.Revision; presetLanguage = language; presetCards.Clear();
                exportSelection.RemoveWhere(x => !presets.Contains(x));
                if (presets.Ready) foreach (string name in presets.Names)
                {
                    var recipe = presets.Get(name);
                    var prefab = ObjectDB.instance.GetItemPrefab(recipe.Prefab);
                    string rarity = Localization.instance.Localize(API.GetRarityDisplayNameByIndex(recipe.Rarity));
                    string tags = string.Join(", ", presets.GetTags(name));
                    presetCards[name] = new PresetCard { Name = name, Prefab = prefab,
                        Summary = (prefab ? Label(prefab) : recipe.Prefab) + " • " + rarity + " • Sv. " + recipe.Quality
                            + "\n" + recipe.Effects.Count + " efekt • " + recipe.Stones.Count + "/" + recipe.SocketCount + " taş"
                            + (tags.Length == 0 ? "" : "\n" + tags) };
                }
            }
            visiblePresets = presetQuery.Get(presets, presetSearch, presetTagFilter);
        }
        private void OpenPresetManager(bool fresh)
        {
            pendingDelete = null;
            if (fresh) { selectedPreset = ""; presetName = ""; presetTags = ""; }
            else { presetName = selectedPreset; presetTags = string.Join(", ", presets.GetTags(selectedPreset)); }
            showPresetManager = true;
        }
        private void DrawPresets()
        {
            bool enabled = GUI.enabled;
            string search = SearchInput(presetSearch, "Şablon ara • ad / prefab / etiket");
            if (search != presetSearch) QueueAction(() => { presetSearch = search; presetScroll = Vector2.zero; });
            GUILayout.Label("Etiket filtresi • virgülle birleştir", muted);
            string tags = GUILayout.TextField(presetTagFilter);
            if (tags != presetTagFilter) QueueAction(() => { presetTagFilter = tags; presetScroll = Vector2.zero; });
            GUILayout.BeginHorizontal();
            GUI.enabled = enabled && presets.Ready && PreviewValid;
            if (GUILayout.Button("Yeni kaydet")) QueueAction(() => OpenPresetManager(true));
            GUI.enabled = enabled && presets.Contains(selectedPreset);
            if (GUILayout.Button("Düzenle")) QueueAction(() => OpenPresetManager(false));
            GUI.enabled = enabled; GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Koleksiyon")) QueueAction(OpenCollection);
            if (GUILayout.Button("Pano paylaşımı")) QueueAction(() => { showTransfer = true; transferStatus = "Tek tasarımı kopyala veya içe al."; });
            GUILayout.EndHorizontal();
            presetScroll = GUILayout.BeginScrollView(presetScroll, GUILayout.ExpandHeight(true));
            GUI.enabled = enabled && presets.Ready;
            foreach (string name in visiblePresets)
            {
                if (!presetCards.TryGetValue(name, out var entry)) continue;
                GUILayout.BeginVertical(card); GUILayout.BeginHorizontal();
                DrawIcon(entry.Prefab, 44);
                if (NameButton(SafeLabel(name), name, name == selectedPreset, 40, true))
                    QueueDesignAction(() => LoadPreset(name));
                bool chosen = GUILayout.Toggle(exportSelection.Contains(name), "✓", GUILayout.Width(40));
                if (chosen != exportSelection.Contains(name)) QueueAction(() => { if (chosen) exportSelection.Add(name); else exportSelection.Remove(name); });
                GUILayout.EndHorizontal(); GUILayout.Label(SafeLabel(entry.Summary), muted);
                GUILayout.EndVertical();
            }
            GUI.enabled = enabled;
            if (!presets.Ready)
            {
                GUILayout.Label(presets.Error ?? "Şablonlar hazır değil.", errorHint);
                if (GUILayout.Button("Yedekleri incele")) QueueAction(() => { recoveryKind = 0; showRecovery = true; recoveryCandidate = null; });
            }
            else if (visiblePresets.Length == 0)
            {
                GUILayout.Label(presets.Names.Length == 0 ? "İlk tasarımını Yeni kaydet ile sakla." : "Arama veya etiketlere uygun şablon yok.", muted);
                if (presets.Names.Length > 0 && GUILayout.Button("Filtreleri temizle")) QueueAction(() => { presetSearch = presetTagFilter = ""; });
            }
            GUILayout.EndScrollView();
            GUILayout.Label(visiblePresets.Length + "/" + presets.Names.Length + " şablon • " + exportSelection.Count + " seçili", muted);
        }
        private void DrawPresetManager(int id)
        {
            bool enabled = GUI.enabled;
            GUILayout.Label("ŞABLON YÖNETİMİ", heading);
            presetManagerScroll = GUILayout.BeginScrollView(presetManagerScroll, GUILayout.ExpandHeight(true));
            GUILayout.Label(string.IsNullOrEmpty(selectedPreset) ? "Yeni kayıt" : "Seçili: " + SafeLabel(selectedPreset), section);
            GUILayout.Label("Kayıt adı / yeni ad", muted); presetName = GUILayout.TextField(presetName);
            GUILayout.Label("Etiketler • virgülle ayır • en fazla 8", muted); presetTags = GUILayout.TextField(presetTags);
            GUILayout.BeginHorizontal();
            GUI.enabled = enabled && presets.Ready && PreviewValid && !string.IsNullOrWhiteSpace(presetName);
            if (GUILayout.Button("Tasarımı kaydet / güncelle", primary))
            {
                string name = presetName.Trim(), tags = presetTags;
                QueueAction(() => { _ = builder.RequestedCount; builder.Build(); presets.Save(name, builder.Export(), tags); selectedPreset = name; status = "Şablon kaydedildi: " + name; });
            }
            GUI.enabled = enabled && presets.Contains(selectedPreset);
            if (GUILayout.Button("Etiketleri kaydet"))
            {
                string name = selectedPreset, tags = presetTags;
                QueueAction(() => { presets.SetTags(name, tags); status = "Etiketler kaydedildi."; });
            }
            GUILayout.EndHorizontal(); GUILayout.BeginHorizontal();
            GUI.enabled = enabled && presets.Contains(selectedPreset) && !string.IsNullOrWhiteSpace(presetName);
            if (GUILayout.Button("Adını değiştir"))
            {
                string oldName = selectedPreset, newName = presetName.Trim();
                QueueAction(() => { presets.Rename(oldName, newName); if (exportSelection.Remove(oldName)) exportSelection.Add(newName); selectedPreset = newName; pendingDelete = null; status = "Şablon yeniden adlandırıldı."; });
            }
            if (GUILayout.Button("Farklı adla kopyala"))
            {
                string source = selectedPreset, target = presetName.Trim();
                QueueAction(() => { presets.Duplicate(source, target); selectedPreset = target; presetTags = string.Join(", ", presets.GetTags(target)); status = "Şablon kopyalandı: " + target; });
            }
            GUI.enabled = enabled && presets.Contains(selectedPreset);
            if (GUILayout.Button("Sil")) { string target = selectedPreset; QueueAction(() => pendingDelete = target); }
            GUI.enabled = enabled; GUILayout.EndHorizontal();
            GUILayout.Label("Kopyalamak için üstte farklı bir ad yaz. Şablon işlemleri asıl envanter eşyasını değiştirmez.", muted);
            if (pendingDelete != null)
            {
                GUILayout.Label("“" + SafeLabel(pendingDelete) + "” silinsin mi?", errorHint);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Evet, sil"))
                {
                    string target = pendingDelete;
                    QueueAction(() => { presets.Delete(target); exportSelection.Remove(target); pendingDelete = null; selectedPreset = presetName = ""; status = "Şablon silindi. Tasarım korundu."; });
                }
                if (GUILayout.Button("Vazgeç")) QueueAction(() => pendingDelete = null);
                GUILayout.EndHorizontal();
            }
            GUILayout.Label(status, muted);
            GUILayout.EndScrollView();
            if (GUILayout.Button("Kütüphaneye dön", primary)) QueueAction(() => { showPresetManager = false; pendingDelete = null; });
            GUI.enabled = enabled;
        }
    }
}
