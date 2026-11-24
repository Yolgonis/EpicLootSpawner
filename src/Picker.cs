using System;
using System.Globalization;
using EpicLoot;
using UnityEngine;

namespace EpicLootSpawner
{
    // One overlay for choosing effects, shardstones and special identities. Every card shows the
    // full game description so a choice can be made without leaving the design page.
    public sealed partial class Plugin
    {
        private const float PickerWidth = 760;

        private void OpenPicker(PickerKind kind)
        {
            if (!builder.Prefab) throw new InvalidOperationException("Önce bir eşya seç.");
            picker = kind;
            if (kind == PickerKind.Effect) { effectScroll = Vector2.zero; RefreshEffects(); }
            else if (kind == PickerKind.Stone) { stoneScroll = Vector2.zero; stoneFilterDirty = true; }
            else if (kind == PickerKind.Special) { specialScroll = Vector2.zero; specialCatalogSignature = null; }
        }

        private void ClosePicker() => picker = PickerKind.None;

        private void DrawPicker(int id)
        {
            string title = picker == PickerKind.Effect ? "EFEKT EKLE" : picker == PickerKind.Stone ? "TAŞ TAK" : "LEGENDARY / SET";
            GUILayout.BeginHorizontal();
            DrawIcon(builder.Prefab, 44);
            GUILayout.BeginVertical();
            GUILayout.Label(title, heading);
            GUILayout.Label(builder.Prefab ? Label(builder.Prefab) + "  •  " + Localization.instance.Localize(API.GetRarityDisplayNameByIndex((int)builder.Rarity)) : "", muted);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("×", "Kapat (Esc)"), theme.button, GUILayout.Width(42), GUILayout.Height(40))) QueueAction(ClosePicker);
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
            if (picker == PickerKind.Effect) DrawEffectPicker();
            else if (picker == PickerKind.Stone) DrawStonePicker();
            else DrawSpecialSelector();
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(status ?? "", status ?? ""), statusBar, GUILayout.MinWidth(0), GUILayout.ExpandWidth(true), GUILayout.Height(40));
            if (GUILayout.Button("Bitti", primary, GUILayout.Width(140), GUILayout.Height(40))) QueueAction(ClosePicker);
            GUILayout.EndHorizontal();
        }

        private void DrawEffectPicker()
        {
            string search = SearchInput(effectSearch, "Efekt ara: ad veya açıklama");
            if (search != effectSearch) QueueAction(() => { effectSearch = search; effectFilterDirty = true; effectScroll = Vector2.zero; });
            string limit = builder.Unlimited
                ? "Limitsiz mod: " + builder.Effects.Count + " efekt seçili, değerler normal aralığın dışına çıkabilir."
                : builder.Effects.Count + " / " + builder.EffectLimit + " efekt seçili. Liste eşya türüne, nadirliğe ve seçili efektlere göre süzülür.";
            GUILayout.Label(limit, muted);
            effectScroll = GUILayout.BeginScrollView(effectScroll, GUILayout.ExpandHeight(true));
            bool enabled = GUI.enabled;
            foreach (var entry in filteredEffects)
            {
                var definition = entry.Definition;
                GUILayout.BeginVertical(card);
                GUILayout.BeginHorizontal();
                GUILayout.Label(entry.Title, section);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+ Ekle", primary, GUILayout.Width(110), GUILayout.Height(34)))
                {
                    string name = entry.Title;
                    QueueDesignAction(() => { builder.AddEffect(definition); RefreshEffects(); status = "Efekt eklendi: " + name; });
                }
                GUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(entry.Description)) GUILayout.Label(entry.Description, body);
                if (!string.IsNullOrEmpty(entry.Note)) GUILayout.Label(entry.Note, muted);
                GUILayout.Label(entry.Range, muted);
                GUILayout.EndVertical();
            }
            GUI.enabled = enabled;
            if (available.Count == 0)
                GUILayout.Label(builder.Effects.Count >= builder.MaxEffects
                    ? (builder.Unlimited ? "Efekt üst sınırına ulaşıldı." : "Bu nadirliğin efekt sınırı doldu. Daha fazlası için üstteki Limitsiz modu aç ya da bir efekti kaldır.")
                    : "Bu tasarıma eklenebilecek başka efekt yok. Nadirliği veya seviyeyi değiştirmeyi dene.", muted);
            else if (filteredEffects.Length == 0) GUILayout.Label("Aramaya uyan efekt yok.", muted);
            GUILayout.EndScrollView();
        }

        private void DrawStonePicker()
        {
            int sockets = int.TryParse(builder.SocketCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : 0;
            int free = sockets - builder.Stones.Count;
            GUILayout.Label("Taşın nadirliği (eşyanınkinden bağımsız)", caption);
            int rarity = RarityChips((int)stoneRarity);
            if (rarity != (int)stoneRarity) QueueAction(() => { stoneRarity = (ItemRarity)rarity; stoneFilterDirty = true; stoneScroll = Vector2.zero; });
            string search = SearchInput(stoneSearch, "Taş ara");
            if (search != stoneSearch) QueueAction(() => { stoneSearch = search; stoneFilterDirty = true; stoneScroll = Vector2.zero; });
            GUILayout.BeginHorizontal();
            GUILayout.Label(free > 0 ? "Boş soket: " + free : "Boş soket yok.", free > 0 ? muted : warnHint, GUILayout.Height(34));
            bool enabled = GUI.enabled;
            if (free <= 0)
            {
                GUI.enabled = enabled && sockets < builder.MaxSockets;
                if (GUILayout.Button(new GUIContent("Soket ekle", sockets < builder.MaxSockets ? "Soket sayısını bir artır" : "Soket sınırı dolu. Limitsiz mod daha fazlasına izin verir."), GUILayout.Width(130), GUILayout.Height(34)))
                    QueueDesignAction(() => { builder.SocketCount = (sockets + 1).ToString(CultureInfo.InvariantCulture); RefreshEffects(); });
                GUI.enabled = enabled;
            }
            GUILayout.EndHorizontal();
            stoneScroll = GUILayout.BeginScrollView(stoneScroll, GUILayout.ExpandHeight(true));
            foreach (var prefab in filteredStones)
            {
                stonePreviews.TryGetValue(prefab.name, out var info);
                GUILayout.BeginVertical(card);
                GUILayout.BeginHorizontal();
                DrawIcon(prefab, 40);
                GUILayout.Label(Label(prefab), section, GUILayout.Height(40));
                GUILayout.FlexibleSpace();
                GUI.enabled = enabled && free > 0 && PreviewValid && info != null && info.Allowed;
                if (GUILayout.Button("+ Tak", GUI.enabled ? primary : disabledPrimary, GUILayout.Width(110), GUILayout.Height(34)))
                {
                    string stone = prefab.name, name = Label(prefab);
                    QueueDesignAction(() =>
                    {
                        builder.Stones.Add(stone);
                        try { builder.Build(); } catch { builder.Stones.RemoveAt(builder.Stones.Count - 1); throw; }
                        RefreshEffects();
                        status = "Taş takıldı: " + name;
                    });
                }
                GUI.enabled = enabled;
                GUILayout.EndHorizontal();
                if (info == null) GUILayout.Label(PreviewValid ? "Etkisi hesaplanıyor…" : "Etkiyi görmek için önce tasarımdaki hataları düzelt.", muted);
                else
                {
                    if (!string.IsNullOrEmpty(info.Title)) GUILayout.Label(info.Title, body);
                    if (!string.IsNullOrEmpty(info.Description)) GUILayout.Label(info.Description, body);
                    if (!string.IsNullOrEmpty(info.Note)) GUILayout.Label(info.Note, muted);
                    GUILayout.Label(info.Reason, info.Allowed ? muted : errorHint);
                }
                GUILayout.EndVertical();
            }
            if (filteredStones.Length == 0) GUILayout.Label("Bu nadirlikte veya aramaya uyan taş yok.", muted);
            GUILayout.EndScrollView();
        }
    }
}
