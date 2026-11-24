using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EpicLoot;
using EpicLoot.ShardStones;
using HarmonyLib;
using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private GUISkin theme;
        private GUIStyle heading, muted, card, panel, primary, disabledPrimary, selected, section, errorHint, warnHint, tabs, chip, badge, statusBar, tooltipStyle, placeholder, caption, body;
        private readonly List<Texture2D> themeTextures = new List<Texture2D>();
        private int libraryTab;
        private PickerKind picker;

        private Texture2D Solid(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color); texture.Apply(); themeTextures.Add(texture);
            return texture;
        }

        private Texture2D Surface(string fillHex, string outlineHex, int radius)
        {
            const int size = 32;
            ColorUtility.TryParseHtmlString(fillHex, out Color fill);
            ColorUtility.TryParseHtmlString(outlineHex, out Color outline);
            var texture = new Texture2D(size, size) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - radius), 0);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - radius), 0);
                float distance = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                Color pixel = Color.Lerp(outline, fill, Mathf.Clamp01(-distance - 0.5f));
                pixel.a *= Mathf.Clamp01(0.5f - distance);
                pixels[y * size + x] = pixel;
            }
            texture.SetPixels(pixels); texture.Apply(); themeTextures.Add(texture);
            return texture;
        }

        private void EnsureTheme()
        {
            if (theme) return;
            theme = Instantiate(GUI.skin); theme.hideFlags = HideFlags.HideAndDontSave;
            var background = Surface("#101820", "#44515C", 10);
            var panelBackground = Surface("#18232D", "#2D3B46", 8);
            var cardBackground = Surface("#1F2C37", "#354550", 6);
            var hover = Surface("#2B3B49", "#607382", 6);
            var field = Surface("#111B24", "#394B59", 6);
            var focus = Surface("#182734", "#C9A267", 6);
            var gold = Surface("#C8A267", "#E2BF88", 6);
            var goldHover = Surface("#DDBB86", "#F3D4A4", 6);
            var activeTab = Surface("#334337", "#BBA06B", 6);
            var selectedRow = Surface("#304039", "#C5A46D", 6);
            theme.window.normal.background = background;
            theme.window.onNormal.background = background;
            theme.window.padding = new RectOffset(20, 20, 18, 14);
            theme.window.border = new RectOffset(12, 12, 12, 12);
            theme.window.normal.textColor = Color.clear;
            theme.box.normal.background = cardBackground; theme.box.padding = new RectOffset(12, 12, 10, 10);
            theme.box.margin = new RectOffset(0, 0, 4, 4);
            theme.box.border = new RectOffset(8, 8, 8, 8);
            theme.label.fontSize = 14; theme.label.richText = true; theme.label.wordWrap = true;
            theme.label.normal.textColor = new Color(0.9f, 0.92f, 0.94f);
            theme.button.fontSize = 14; theme.button.richText = true; theme.button.wordWrap = true;
            theme.button.padding = new RectOffset(12, 12, 9, 9);
            theme.button.margin = new RectOffset(3, 3, 3, 3);
            theme.button.border = new RectOffset(8, 8, 8, 8);
            theme.button.normal.background = cardBackground; theme.button.hover.background = hover;
            theme.button.active.background = focus; theme.button.onNormal.background = activeTab;
            theme.button.onHover.background = selectedRow; theme.button.onActive.background = focus;
            theme.button.normal.textColor = theme.button.hover.textColor = theme.button.active.textColor = Color.white;
            theme.button.onNormal.textColor = theme.button.onHover.textColor = theme.button.onActive.textColor = Color.white;
            theme.textField.fontSize = 14; theme.textField.padding = new RectOffset(10, 10, 8, 8);
            theme.textField.border = new RectOffset(8, 8, 8, 8);
            theme.textField.normal.background = field; theme.textField.focused.background = focus;
            theme.textField.hover.background = hover; theme.textField.active.background = focus;
            theme.textField.normal.textColor = theme.textField.focused.textColor = Color.white;
            theme.textField.richText = false;
            theme.textArea = new GUIStyle(theme.textField) { wordWrap = true };
            theme.verticalScrollbar.normal.background = Solid("#101922");
            theme.verticalScrollbarThumb.normal.background = Surface("#637887", "#637887", 5);
            theme.verticalScrollbarThumb.border = new RectOffset(6, 6, 6, 6);
            theme.horizontalSlider.normal.background = Solid("#111B24");
            theme.horizontalSlider.border = new RectOffset();
            theme.horizontalSlider.fixedHeight = 6;
            theme.horizontalSliderThumb.normal.background = Solid("#C8A267");
            theme.horizontalSliderThumb.hover.background = theme.horizontalSliderThumb.active.background = Solid("#E2BF88");
            theme.horizontalSliderThumb.border = new RectOffset();
            theme.horizontalSliderThumb.fixedWidth = 12;
            theme.horizontalSliderThumb.fixedHeight = 14;
            heading = new GUIStyle(theme.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            section = new GUIStyle(theme.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            muted = new GUIStyle(theme.label) { fontSize = 12 };
            muted.normal.textColor = new Color(0.72f, 0.78f, 0.82f);
            errorHint = new GUIStyle(muted); errorHint.normal.textColor = new Color(0.95f, 0.63f, 0.47f);
            card = new GUIStyle(theme.box);
            panel = new GUIStyle(theme.box) { padding = new RectOffset(14, 14, 14, 12), margin = new RectOffset(), border = new RectOffset(10, 10, 10, 10) };
            panel.normal.background = panelBackground;
            primary = new GUIStyle(theme.button); primary.normal.background = gold;
            primary.hover.background = goldHover; primary.fontStyle = FontStyle.Bold;
            primary.normal.textColor = primary.hover.textColor = primary.active.textColor = new Color(0.08f, 0.12f, 0.15f);
            primary.active.background = gold;
            disabledPrimary = new GUIStyle(theme.button); disabledPrimary.normal.textColor = muted.normal.textColor;
            selected = new GUIStyle(theme.button); selected.normal.background = selectedRow;
            selected.normal.textColor = new Color(1f, 0.86f, 0.63f);
            tabs = new GUIStyle(theme.button) { fontSize = 12, padding = new RectOffset(5, 5, 7, 7) };
            tabs.normal.textColor = muted.normal.textColor; tabs.onNormal.textColor = new Color(1f, 0.86f, 0.63f);
            badge = new GUIStyle(muted) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(8, 8, 5, 5), border = new RectOffset(8, 8, 8, 8) };
            badge.normal.background = cardBackground;
            statusBar = new GUIStyle(muted) { padding = new RectOffset(12, 12, 8, 8), border = new RectOffset(8, 8, 8, 8) };
            statusBar.normal.background = field;
            statusBar.richText = false;
            tooltipStyle = new GUIStyle(theme.box) { fontSize = 14, wordWrap = true, richText = false, alignment = TextAnchor.UpperLeft };
            tooltipStyle.normal.background = field; tooltipStyle.normal.textColor = Color.white;
            warnHint = new GUIStyle(muted); warnHint.normal.textColor = new Color(0.96f, 0.8f, 0.45f);
            chip = new GUIStyle(tabs) { fontSize = 12, padding = new RectOffset(4, 4, 6, 6), margin = new RectOffset(2, 2, 2, 2), wordWrap = false };
            placeholder = new GUIStyle(muted) { padding = new RectOffset(10, 10, 8, 8), wordWrap = false, clipping = TextClipping.Clip };
            caption = new GUIStyle(muted) { fontStyle = FontStyle.Bold, padding = new RectOffset(2, 2, 8, 2) };
            caption.normal.textColor = new Color(0.86f, 0.74f, 0.52f);
            body = new GUIStyle(theme.label) { fontSize = 13 };
            body.normal.textColor = new Color(0.82f, 0.86f, 0.89f);
        }

        private void DisposeTheme()
        {
            foreach (var texture in themeTextures) if (texture) Destroy(texture);
            themeTextures.Clear(); if (theme) Destroy(theme); theme = null;
            nameHeights.Clear();
        }

        private void OnGUI()
        {
            if (!IsOpen || !Player.m_localPlayer) return;
            var oldSkin = GUI.skin; var oldMatrix = GUI.matrix; bool oldEnabled = GUI.enabled; var oldColor = GUI.color;
            try
            {
                EnsureTheme();
                GUI.color = Color.white;
                float scale = layout.Scale;
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
                GUI.skin = theme;
                HandleWindowResize();
                GUI.enabled = oldEnabled && !HasModal;
                window = GUILayout.Window(190413, window, DrawWindow, "", GUILayout.Width(layout.Width), GUILayout.Height(layout.Height));
                GUI.enabled = oldEnabled;
                DrawResizeGrip();
                float modalX = window.x + (window.width - 660) / 2;
                float modalHeight = Mathf.Min(670, layout.Height - 40);
                float modalY = window.y + (window.height - modalHeight) / 2;
                if (picker != PickerKind.None)
                {
                    float pickerHeight = Mathf.Min(820, layout.Height - 40);
                    var rect = new Rect(window.x + (window.width - PickerWidth) / 2, window.y + (window.height - pickerHeight) / 2, PickerWidth, pickerHeight);
                    GUILayout.Window(190422, rect, DrawPicker, "", GUILayout.Width(PickerWidth), GUILayout.Height(pickerHeight));
                }
                else if (showInventoryUndo)
                    GUILayout.Window(190421, new Rect(modalX, modalY, 660, modalHeight), DrawInventoryUndo, "", GUILayout.Width(660), GUILayout.Height(modalHeight));
                else if (showDisenchant)
                    GUILayout.Window(190420, new Rect(modalX, modalY, 660, modalHeight), DrawDisenchant, "", GUILayout.Width(660), GUILayout.Height(modalHeight));
                else if (showRecovery)
                    GUILayout.Window(190419, new Rect(modalX, modalY, 660, modalHeight), DrawRecovery, "", GUILayout.Width(660), GUILayout.Height(modalHeight));
                else if (showCollection)
                    GUILayout.Window(190418, new Rect(modalX, modalY, 660, modalHeight), DrawCollection, "", GUILayout.Width(660), GUILayout.Height(modalHeight));
                else if (showPresetManager)
                    GUILayout.Window(190417, new Rect(modalX, modalY, 660, modalHeight), DrawPresetManager, "", GUILayout.Width(660), GUILayout.Height(modalHeight));
                else if (showAppearance)
                    GUILayout.Window(190416, new Rect(modalX, modalY, 660, modalHeight), DrawAppearance, "", GUILayout.Width(660), GUILayout.Height(modalHeight));
                else if (showTransfer)
                    GUILayout.Window(190415, new Rect(modalX, modalY, 660, modalHeight), DrawTransfer, "", GUILayout.Width(660), GUILayout.Height(modalHeight));
                else if (showPreview && !string.IsNullOrEmpty(VisiblePreview))
                    GUILayout.Window(190414, new Rect(modalX, modalY, 660, modalHeight), DrawPreview, "", GUILayout.Width(660), GUILayout.Height(modalHeight));
                else showPreview = false;
                DrawTooltip();
            }
            catch (ExitGUIException) { throw; }
            catch (Exception ex) { DeferPanelFailure(ex); }
            finally { GUI.skin = oldSkin; GUI.enabled = oldEnabled; GUI.matrix = oldMatrix; GUI.color = oldColor; }
        }

        private void DrawWindow(int id)
        {
            DrawTopBar();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            DrawLibrary(); GUILayout.Space(12); DrawEditor(); GUILayout.Space(12); DrawLivePreview();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            DrawActionBar();
            // Called last: buttons in the top bar have already consumed their clicks.
            GUI.DragWindow(new Rect(0, 0, window.width, 60));
        }

        private bool EditingInventory => inventoryKey != null;

        private void DrawTopBar()
        {
            GUILayout.BeginHorizontal();
            int mode = libraryTab == 2 || EditingInventory ? 1 : 0;
            int nextMode = GUILayout.Toolbar(mode, new[] { "Yeni eşya", "Envanterim" }, tabs, GUILayout.Width(250), GUILayout.Height(38));
            if (nextMode != mode)
            {
                if (nextMode == 1) QueueAction(() => libraryTab = 2);
                else QueueDesignAction(() =>
                {
                    libraryTab = 0;
                    if (inventoryKey == null) return;
                    // Keeps the loaded design as a starting point for a new item.
                    inventoryKey = null; RefreshEffects();
                    status = "Yeni eşya modu. Envanterden alınan tasarım yeni eşya için korunuyor.";
                });
            }
            GUILayout.Space(8);
            DrawHistory();
            GUILayout.Space(8);
            if (GUILayout.Button(new GUIContent("Şablonlar", "Kayıtlı tasarımları aç, kaydet veya paylaş"), libraryTab == 1 ? selected : theme.button, GUILayout.Width(110), GUILayout.Height(38)))
                QueueAction(() => libraryTab = libraryTab == 1 ? (EditingInventory ? 2 : 0) : 1);
            GUILayout.FlexibleSpace();
            bool free = unlimited.Value;
            if (GUILayout.Button(new GUIContent(free ? "● Limitsiz: Açık" : "○ Limitsiz: Kapalı",
                    "Açıkken efekt değerleri normal aralığın dışına çıkabilir, en fazla " + Spawner.UnlimitedEffects + " efekt ve " + Spawner.UnlimitedSockets
                    + " soket kullanılabilir. EpicLoot'un efekt içi tavanları (direnç sınırı, %100 kırpmaları vb.) da kalkar. Eşya türü uyumluluğu ve özel güç kuralları yine uygulanır."), free ? selected : theme.button, GUILayout.Width(170), GUILayout.Height(38)))
                QueueAction(() => SetUnlimited(!free));
            if (GUILayout.Button(new GUIContent("Ayarlar", "Ölçek, tanılama ve kullanım rehberi"), GUILayout.Width(90), GUILayout.Height(38)))
                QueueAction(() => { selectedScale = uiScale.Value; appearanceTab = 0; appearanceScroll = Vector2.zero; showAppearance = true; });
            if (GUILayout.Button(new GUIContent("Yenile", "Eşya listesini, şablonları ve favorileri yeniden yükle"), GUILayout.Width(80), GUILayout.Height(38)))
            { QueueAction(presets.Load); QueueAction(() => { favorites.Load(); libraryDirty = true; }); QueueAction(RefreshCatalog); }
            if (GUILayout.Button(new GUIContent("×", "Kapat (" + OpenKeyName + " / Esc)"), GUILayout.Width(42), GUILayout.Height(38))) QueueAction(Close);
            GUILayout.EndHorizontal();
        }

        private void SetUnlimited(bool value)
        {
            unlimited.Value = value;
            builder.Unlimited = value;
            EffectCaps.Lifted = value;
            RefreshEffects();
            status = value ? "Limitsiz mod açık: değer, efekt sayısı, soket ve efekt içi tavanlar kaldırıldı." : "Limitsiz mod kapalı: EpicLoot'un normal sınırları geçerli.";
        }

        private void DrawLibrary()
        {
            GUILayout.BeginVertical(panel, GUILayout.Width(layout.LibraryWidth), GUILayout.Height(layout.PanelHeight));
            if (libraryTab == 1) { GUILayout.Label("Şablonlar", section); DrawPresets(); }
            else if (libraryTab == 2) DrawInventoryLibrary();
            else DrawItemLibrary();
            GUILayout.EndVertical();
        }

        private void DrawActionBar()
        {
            GUILayout.BeginHorizontal(panel, GUILayout.Height(62));
            bool enabled = GUI.enabled;
            if (production != null)
            {
                StatusLine();
                if (GUILayout.Button($"Üretimi durdur  •  {produced} / {requested}", primary, GUILayout.Width(280), GUILayout.Height(40))) stopProduction = true;
            }
            else if (EditingInventory)
            {
                bool usable = inventoryBlockReason == null;
                GUI.enabled = enabled && usable && EditingSource?.Item.GetMagicItem() != null;
                if (GUILayout.Button(new GUIContent("Büyüyü kaldır…", "Tüm büyüyü kaldırır; takılı taşlar envantere döner. Önce onay ekranı açılır."), GUILayout.Height(40))) QueueAction(OpenDisenchant);
                GUI.enabled = enabled && usable && EditingSource?.Item.GetMagicItem()?.Sockets.Count > 0;
                if (GUILayout.Button(new GUIContent("Taş çıkar…", "Büyüyü koruyarak seçtiğin taşı çıkarır ve envantere iade eder"), GUILayout.Height(40))) QueueAction(() => OpenRemoval(0));
                GUI.enabled = enabled;
                DrawUndoOperation();
                if (!usable)
                {
                    GUILayout.Label(new GUIContent(inventoryBlockReason, inventoryBlockReason), errorHint, GUILayout.MinWidth(0), GUILayout.ExpandWidth(true), GUILayout.Height(40));
                    if (EditingSource != null && GUILayout.Button("Yeniden seç", GUILayout.Height(40)))
                        QueueDesignAction(() => LoadInventoryItem(new InventoryEntry { Item = EditingSource.Item, Prefab = EditingSource.Prefab }));
                }
                else StatusLine();
                GUI.enabled = enabled && PreviewValid && usable;
                if (GUILayout.Button("Asıl eşyaya uygula", GUI.enabled ? primary : disabledPrimary, GUILayout.Width(220), GUILayout.Height(40))) QueueAction(ApplyInventoryEdit);
                GUI.enabled = enabled;
            }
            else
            {
                GUILayout.Label("Teslim", muted, GUILayout.Width(48), GUILayout.Height(40));
                int delivery = GUILayout.Toolbar(toInventory ? 0 : 1, new[] { "Envantere", "Yere bırak" }, tabs, GUILayout.Width(220), GUILayout.Height(40));
                if (delivery != (toInventory ? 0 : 1)) QueueAction(() => toInventory = delivery == 0);
                DrawUndoOperation();
                StatusLine();
                int count = int.TryParse(builder.Count, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : 1;
                GUI.enabled = enabled && PreviewValid;
                if (GUILayout.Button(count > 1 ? count + " eşya üret" : "Eşyayı üret", GUI.enabled ? primary : disabledPrimary, GUILayout.Width(220), GUILayout.Height(40))) QueueAction(BeginSpawn);
                GUI.enabled = enabled;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawUndoOperation()
        {
            if (lastInventoryOperation == null) return;
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && production == null;
            string tip = inventoryUndoReason == null ? "Bu oturumdaki son envanter işlemini geri al: " + lastInventoryOperation.Label
                : "Geri alma şu an engelli; ayrıntıları görmek için aç. " + inventoryUndoReason;
            if (GUILayout.Button(new GUIContent(inventoryUndoReason == null ? "Son işlemi geri al…" : "Geri al (engelli)…", tip), GUILayout.Height(40))) QueueAction(OpenInventoryUndo);
            GUI.enabled = enabled;
        }

        private void StatusLine()
        {
            string text = string.IsNullOrEmpty(status) ? (builder.Prefab ? "Hazır." : "Soldan bir eşya seçerek başla.") : status;
            GUILayout.Label(new GUIContent(text, text), statusBar, GUILayout.MinWidth(0), GUILayout.ExpandWidth(true), GUILayout.Height(40));
            if (GUILayout.Button(new GUIContent("?", "Ayrıntı ve tanılama"), GUILayout.Width(40), GUILayout.Height(40)))
                QueueAction(() => { selectedScale = uiScale.Value; appearanceTab = 1; appearanceScroll = Vector2.zero; showAppearance = true; });
        }

        private int RarityChips(int current)
        {
            var names = Enum.GetNames(typeof(ItemRarity));
            GUILayout.BeginHorizontal();
            for (int i = 0; i < names.Length; i++)
            {
                string name = Localization.instance.Localize(API.GetRarityDisplayNameByIndex(i));
                string color = API.GetRarityColorByIndex(i);
                if (GUILayout.Button("<color=" + color + ">" + name + "</color>", current == i ? selected : chip, GUILayout.MinWidth(0), GUILayout.Height(34))) current = i;
            }
            GUILayout.EndHorizontal();
            return current;
        }

        // Single-line text input; the error appears underneath only when there is one.
        private void Field(string label, ref string value, string key, float labelWidth = 90)
        {
            fieldErrors.TryGetValue(key, out string error);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, muted, GUILayout.Width(labelWidth), GUILayout.Height(34));
            var oldColor = GUI.color;
            if (error != null) GUI.color = new Color(1f, 0.72f, 0.56f);
            string entered = GUILayout.TextField(value, GUILayout.Height(34));
            GUI.color = oldColor;
            if (entered != value) { value = entered; currentPreviewSignature = null; }
            GUILayout.EndHorizontal();
            if (error != null) WrappedHint(error, errorHint, layout.EditorWidth - 60, 20);
        }

        // Whole-number input with − / + buttons. Buttons queue the change so history records it.
        private void Stepper(string label, string value, int min, int max, Action<string> set, float width)
        {
            GUILayout.BeginVertical(GUILayout.Width(width));
            GUILayout.Label(label, muted, GUILayout.Height(20));
            GUILayout.BeginHorizontal();
            bool parsed = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && parsed && number > min;
            if (GUILayout.Button("−", GUILayout.Width(34), GUILayout.Height(34)))
            { string next = Math.Min(max, number - 1).ToString(CultureInfo.InvariantCulture); QueueDesignAction(() => set(next)); }
            GUI.enabled = enabled;
            string entered = GUILayout.TextField(value, GUILayout.MinWidth(36), GUILayout.Height(34));
            if (entered != value) { set(entered); currentPreviewSignature = null; }
            GUI.enabled = enabled && (!parsed || number < max);
            if (GUILayout.Button("+", GUILayout.Width(34), GUILayout.Height(34)))
            { string next = (parsed ? Math.Max(min, number + 1) : min).ToString(CultureInfo.InvariantCulture); QueueDesignAction(() => set(next)); }
            GUI.enabled = enabled;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void DrawIcon(GameObject prefab, float size)
        {
            // Reserve the same layout slot on every event, including when no icon exists.
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            if (Event.current.type != EventType.Repaint) return;
            GUI.Box(rect, GUIContent.none);
            rect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8);
            if (!prefab || !prefab.TryGetComponent(out ItemDrop drop)) { GUI.Label(rect, "·"); return; }
            var data = drop.m_itemData;
            var icons = data?.m_shared?.m_icons;
            int index = Rules.IconIndex(icons?.Length ?? 0, data?.m_variant ?? 0);
            if (index < 0) { GUI.Label(rect, "·"); return; }
            var sprite = icons[index];
            if (!sprite) sprite = icons.FirstOrDefault(x => x);
            if (!sprite || !sprite.texture) return;
            Rect source;
            try { source = sprite.textureRect; } catch (UnityException) { return; } catch (InvalidOperationException) { return; }
            var uv = new Rect(source.x / sprite.texture.width, source.y / sprite.texture.height, source.width / sprite.texture.width, source.height / sprite.texture.height);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv);
        }

        private void DrawPreview(int id)
        {
            GUILayout.Label(inventoryKey == null ? "ÜRETİLECEK EŞYA" : (comparisonTab == 0 ? "DEĞİŞİKLİKLER" : comparisonTab == 2 ? "MEVCUT EŞYA" : "YENİ TASARIM"), heading);
            previewScroll = GUILayout.BeginScrollView(previewScroll, GUILayout.ExpandHeight(true));
            GUILayout.Label(VisiblePreview); GUILayout.EndScrollView();
            if (GUILayout.Button("Tasarıma dön", primary, GUILayout.Height(38))) showPreview = false;
        }

        [HarmonyPatch(typeof(Player), "TakeInput")]
        private static class PlayerInput { private static void Postfix(ref bool __result) { if (IsOpen) __result = false; } }
        [HarmonyPatch(typeof(PlayerController), "TakeInput")]
        private static class ControllerInput { private static void Postfix(ref bool __result) { if (IsOpen) __result = false; } }
    }
}
