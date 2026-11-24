using System;
using System.Globalization;
using EpicLoot;
using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private void DrawEditor()
        {
            GUILayout.BeginVertical(panel, GUILayout.Width(layout.EditorWidth), GUILayout.Height(layout.PanelHeight));
            DrawEditorHeader();
            recipeScroll = GUILayout.BeginScrollView(recipeScroll, GUILayout.ExpandHeight(true));
            if (!builder.Prefab)
            {
                GUILayout.Space(20);
                GUILayout.Label("Soldaki listeden bir eşya seç.", section);
                GUILayout.Label("Ardından nadirliği, seviyeyi, efektleri ve taşları bu sayfada ayarlarsın. "
                    + "Envanterindeki bir eşyayı düzenlemek için üstteki Envanterim'e geç.", body);
            }
            else
            {
                DrawBasics();
                GUILayout.Space(14);
                DrawEffects();
                GUILayout.Space(14);
                DrawSockets();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawEditorHeader()
        {
            GUILayout.BeginHorizontal();
            DrawIcon(builder.Prefab, 52);
            GUILayout.BeginVertical();
            DrawItemHeading(builder.Prefab ? Label(builder.Prefab) : "Eşya seçilmedi", layout.EditorWidth - 260);
            string identity = builder.IsSpecial
                ? "<color=" + API.GetRarityColorByIndex((int)builder.Rarity) + ">" + SafeLabel(SpecialItems.Name(builder.LegendaryID)) + "</color>"
                    + (string.IsNullOrEmpty(builder.SetID) ? "" : "  •  Set: " + SafeLabel(SpecialItems.SetName(builder.SetID)))
                : "Sıradan büyülü eşya";
            GUILayout.Label((EditingInventory ? "Envanter düzenleme  •  " : "Yeni eşya  •  ") + identity, muted);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && builder.Prefab;
            string tip = EditingInventory ? "Envanter düzenlemesinde kimlik korunur; listeyi yalnızca inceleyebilirsin."
                : "Bu eşya için uygun Legendary / Set kimliklerini açıklamalarıyla gör ve seç";
            if (GUILayout.Button(new GUIContent("Legendary / Set…", tip), GUILayout.Width(170), GUILayout.Height(40))) QueueAction(() => OpenPicker(PickerKind.Special));
            GUI.enabled = enabled;
            GUILayout.EndHorizontal();
            if (fieldErrors.TryGetValue("Identity", out string identityError)) WrappedHint(identityError, errorHint, layout.EditorWidth - 40, 20);
            GUILayout.Space(8);
        }

        private void DrawBasics()
        {
            GUILayout.Label("Nadirlik", caption);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && !builder.IsSpecial;
            int rarity = RarityChips((int)builder.Rarity);
            GUI.enabled = enabled;
            if (rarity != (int)builder.Rarity) QueueDesignAction(() => { builder.Rarity = (ItemRarity)rarity; RefreshEffects(); });
            if (builder.IsSpecial) GUILayout.Label("Özel eşyanın nadirliği kimliğiyle sabittir.", muted);

            GUILayout.Space(6);
            int maxQuality = Math.Max(1, builder.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxQuality);
            float column = (layout.EditorWidth - 60) / 3;
            GUILayout.BeginHorizontal();
            Stepper("Seviye  (normal 1–" + maxQuality + ")", builder.Quality, 1, int.MaxValue, x => builder.Quality = x, column);
            if (!EditingInventory) Stepper("Adet  (1–100)", builder.Count, 1, 100, x => builder.Count = x, column);
            Stepper("Soket  (" + (builder.Unlimited ? "en fazla " + builder.MaxSockets : "normal 0–" + builder.SocketLimit) + ")",
                builder.SocketCount, 0, builder.MaxSockets, x => builder.SocketCount = x, column);
            GUILayout.EndHorizontal();
            foreach (string key in new[] { "Quality", "Count", "SocketCount" })
                if (fieldErrors.TryGetValue(key, out string error)) WrappedHint(error, errorHint, layout.EditorWidth - 60, 20);
            GUILayout.Space(6);
            Field("Özel ad", ref builder.Name, "Name", 70);
        }

        private void DrawEffects()
        {
            GUILayout.BeginHorizontal();
            string count = builder.Unlimited
                ? builder.Effects.Count + "   (normal en fazla " + builder.EffectLimit + ")"
                : builder.Effects.Count + " / " + builder.EffectLimit;
            GUILayout.Label("Efektler   " + count, section, GUILayout.Height(36));
            GUILayout.FlexibleSpace();
            bool enabled = GUI.enabled;
            bool full = builder.Effects.Count >= builder.MaxEffects;
            GUI.enabled = enabled && !full;
            if (GUILayout.Button(new GUIContent("+ Efekt ekle", full ? "Efekt sınırı dolu. Limitsiz mod daha fazlasına izin verir." : "Bu eşyaya uygun efektleri açıklamalarıyla listele"),
                    primary, GUILayout.Width(150), GUILayout.Height(36)))
                QueueAction(() => OpenPicker(PickerKind.Effect));
            GUI.enabled = enabled;
            GUILayout.EndHorizontal();
            if (fieldErrors.TryGetValue("Effects", out string effectError)) WrappedHint(effectError, errorHint, layout.EditorWidth - 60, 20);
            if (builder.Effects.Count == 0) GUILayout.Label("Henüz efekt yok. + Efekt ekle ile başla.", muted);
            for (int i = 0; i < builder.Effects.Count; i++) DrawEffectRow(i);
        }

        private void DrawEffectRow(int index)
        {
            var choice = builder.Effects[index];
            MagicItemEffectDefinitions.AllDefinitions.TryGetValue(choice.Type, out var definition);
            MagicItemEffectDefinition.ValueDef range = null;
            string rangeError = null;
            try { if (definition != null) range = builder.EffectRange(definition); }
            catch (InvalidOperationException ex) { rangeError = ex.Message; }
            float value = EffectDisplay.Value(choice.Value, range?.MinValue ?? 1);

            GUILayout.BeginVertical(card);
            GUILayout.BeginHorizontal();
            GUILayout.Label(definition == null ? SafeLabel(choice.Type) : EffectDisplay.Title(definition, value), section);
            bool enabled = GUI.enabled;
            bool protectedPower = builder.IsProtected(choice.Type);
            GUI.enabled = enabled && !protectedPower;
            bool remove = GUILayout.Button(new GUIContent("×", protectedPower ? "EpicLoot bu özel gücün kaldırılmasına izin vermiyor" : "Efekti kaldır"), theme.button, GUILayout.Width(38), GUILayout.Height(34));
            GUI.enabled = enabled;
            GUILayout.EndHorizontal();
            if (definition != null)
            {
                GUILayout.Label(EffectDisplay.Description(definition, value), body);
                string note = EffectDisplay.SpawnerNote(definition, value);
                if (note.Length > 0) GUILayout.Label(note, muted);
                if (builder.IsRequired(choice.Type))
                    GUILayout.Label(protectedPower ? "Kimliğe ait sabit güç • değeri düzenlenebilir" : "Özel kimliğin başlangıç gücü", muted);
            }
            fieldErrors.TryGetValue("Effect." + index, out string error);
            if (rangeError != null) WrappedHint(rangeError, errorHint, layout.EditorWidth - 80, 20);
            else if (range != null) DrawEffectValue(choice, range, value, error);
            else
            {
                GUILayout.Label("Aç/kapat etkisi  •  değer 1", muted);
                if (error != null)
                {
                    WrappedHint(error, errorHint, layout.EditorWidth - 80, 20);
                    if (GUILayout.Button("Değeri 1 yap", GUILayout.Width(140))) QueueDesignAction(() => { choice.Value = "1"; RefreshEffects(); });
                }
            }
            GUILayout.EndVertical();
            if (remove) QueueDesignAction(() => { builder.Effects.RemoveAt(index); RefreshEffects(); });
        }

        private void DrawEffectValue(EffectChoice choice, MagicItemEffectDefinition.ValueDef range, float value, string error)
        {
            bool validRange = Rules.ValidRange(range.MinValue, range.MaxValue);
            float step = range.Increment > 0 && !float.IsInfinity(range.Increment) ? range.Increment
                : validRange && range.MaxValue > range.MinValue ? Math.Max(0.01f, (range.MaxValue - range.MinValue) / 20) : 1;
            bool parsed = float.TryParse((choice.Value ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float current);
            string Format(float x) => x.ToString("G9", CultureInfo.InvariantCulture);
            float Limit(float x) => builder.Unlimited || !validRange ? x : Mathf.Clamp(x, range.MinValue, range.MaxValue);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("−", GUILayout.Width(34), GUILayout.Height(32)))
            { string next = Format(Limit((parsed ? current : range.MinValue) - step)); QueueDesignAction(() => choice.Value = next); }
            var oldColor = GUI.color;
            if (error != null) GUI.color = new Color(1f, 0.72f, 0.56f);
            string entered = GUILayout.TextField(choice.Value, GUILayout.Width(84), GUILayout.Height(32));
            GUI.color = oldColor;
            if (entered != choice.Value) { choice.Value = entered; currentPreviewSignature = null; }
            if (GUILayout.Button("+", GUILayout.Width(34), GUILayout.Height(32)))
            { string next = Format(Limit((parsed ? current : range.MinValue) + step)); QueueDesignAction(() => choice.Value = next); }
            if (validRange && range.MaxValue > range.MinValue)
            {
                GUILayout.BeginVertical();
                GUILayout.Space(13);
                float clamped = Mathf.Clamp(value, range.MinValue, range.MaxValue);
                float slid = GUILayout.HorizontalSlider(clamped, range.MinValue, range.MaxValue, GUILayout.MinWidth(60));
                GUILayout.EndVertical();
                if (slid != clamped)
                {
                    float adjusted = Rules.SliderValue(slid, range.MinValue, range.MaxValue, range.Increment);
                    QueueAction(() => choice.Value = Format(adjusted));
                }
                if (GUILayout.Button(new GUIContent("Min", "Normal aralığın en düşük değeri"), chip, GUILayout.Width(46), GUILayout.Height(32))) QueueDesignAction(() => choice.Value = Format(range.MinValue));
                if (GUILayout.Button(new GUIContent("Maks", "Normal aralığın en yüksek değeri"), chip, GUILayout.Width(52), GUILayout.Height(32))) QueueDesignAction(() => choice.Value = Format(range.MaxValue));
            }
            else GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            bool outside = parsed && validRange && (current < range.MinValue || current > range.MaxValue);
            if (error != null) WrappedHint(error, errorHint, layout.EditorWidth - 80, 20);
            else if (outside) GUILayout.Label("Normal aralık " + Format(range.MinValue) + " – " + Format(range.MaxValue) + "  •  bu değer aralığın dışında (limitsiz)", warnHint);
            else GUILayout.Label("Normal aralık " + Format(range.MinValue) + " – " + Format(range.MaxValue), muted);
        }

        private void DrawSockets()
        {
            int sockets = int.TryParse(builder.SocketCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? Math.Max(0, parsed) : 0;
            GUILayout.Label("Soketler   " + builder.Stones.Count + " / " + sockets, section, GUILayout.Height(30));
            if (sockets == 0 && builder.Stones.Count == 0)
            {
                GUILayout.Label("Taş takmak için yukarıdan soket sayısını artır.", muted);
                return;
            }
            bool enabled = GUI.enabled;
            int slots = Math.Max(sockets, builder.Stones.Count);
            for (int i = 0; i < slots; i++)
            {
                GUILayout.BeginHorizontal(card);
                if (i < builder.Stones.Count)
                {
                    var prefab = ObjectDB.instance.GetItemPrefab(builder.Stones[i]);
                    DrawIcon(prefab, 36);
                    GUILayout.Label(prefab ? Label(prefab) : SafeLabel(builder.Stones[i]), body, GUILayout.Height(36));
                    GUILayout.FlexibleSpace();
                    int index = i;
                    if (GUILayout.Button(new GUIContent("×", "Taşı tasarımdan çıkar"), theme.button, GUILayout.Width(38), GUILayout.Height(34)))
                        QueueDesignAction(() => { builder.Stones.RemoveAt(index); RefreshEffects(); });
                }
                else
                {
                    GUILayout.Label("Soket " + (i + 1) + "  •  boş", muted, GUILayout.Height(36));
                    GUILayout.FlexibleSpace();
                    GUI.enabled = enabled && PreviewValid;
                    if (GUILayout.Button(new GUIContent("+ Taş tak", PreviewValid ? "Takılabilir taşları etkileriyle listele" : "Önce tasarımdaki hataları düzelt"), GUILayout.Width(120), GUILayout.Height(34)))
                        QueueAction(() => OpenPicker(PickerKind.Stone));
                    GUI.enabled = enabled;
                }
                GUILayout.EndHorizontal();
                if (fieldErrors.TryGetValue("Stone." + i, out string stoneError)) WrappedHint(stoneError, errorHint, layout.EditorWidth - 60, 20);
            }
        }
    }
}
