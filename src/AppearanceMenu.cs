using BepInEx.Configuration;
using EpicLoot;
using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private ConfigEntry<float> uiScale, windowX, windowY, windowWidth, windowHeight;
        private float preferredWidth, preferredHeight;
        private ConfigEntry<bool> performanceLog;
        private ConfigEntry<int> sortSetting;
        private Vector2 appearanceScroll;
        private bool showAppearance;
        private int appearanceTab;
        private float selectedScale = 1;
        private UiLayout layout = UiLayout.ForScreen(1920, 1080, 1);
        private float lastCatalogMs, lastPreviewMs;

        private void InitializeAppearance()
        {
            uiScale = Config.Bind("Arayüz", "Ölçek", 1f, new ConfigDescription("İstenen menü ölçeği; küçük ekranda sığacak şekilde azaltılır.", new AcceptableValueRange<float>(0.75f, 1.5f)));
            windowX = Config.Bind("Arayüz", "Pencere X", 30f, "Menünün son yatay konumu.");
            windowY = Config.Bind("Arayüz", "Pencere Y", 30f, "Menünün son dikey konumu.");
            windowWidth = Config.Bind("Arayüz", "Pencere genişliği", 0f, "Son seçilen pencere genişliği. 0: ekrana göre varsayılan.");
            windowHeight = Config.Bind("Arayüz", "Pencere yüksekliği", 0f, "Son seçilen pencere yüksekliği. 0: ekrana göre varsayılan.");
            preferredWidth = windowWidth.Value; preferredHeight = windowHeight.Value;
            performanceLog = Config.Bind("Tanılama", "Menü sürelerini kaydet", false, "Katalog ve önizleme sürelerini BepInEx günlüğüne yaz.");
            sortSetting = Config.Bind("Arayüz", "Eşya sıralama", 0, new ConfigDescription("0: düşükten yükseğe tier ve ad, 1: tür gruplarında tier ve ad, 2: favoriler önce, ardından tier ve ad.", new AcceptableValueRange<int>(0, 2)));
            itemSort = sortSetting.Value;
            window.x = windowX.Value; window.y = windowY.Value;
            UpdateAppearance();
        }
        private void UpdateAppearance()
        {
            var next = UiLayout.ForScreen(Screen.width, Screen.height, uiScale.Value, preferredWidth, preferredHeight);
            if (windowResize.Active && (Screen.width != resizeScreenWidth || Screen.height != resizeScreenHeight || next.Scale != resizeScale)) CancelWindowResize();
            if (next.Width != layout.Width) nameHeights.Clear();
            layout = next;
            window.width = layout.Width; window.height = layout.Height;
            window.x = UiLayout.Position(window.x, Screen.width / layout.Scale, window.width);
            window.y = UiLayout.Position(window.y, Screen.height / layout.Scale, window.height);
        }
        private void SaveAppearance()
        {
            if (windowX == null) return;
            windowX.Value = window.x; windowY.Value = window.y;
            windowWidth.Value = preferredWidth; windowHeight.Value = preferredHeight;
            sortSetting.Value = itemSort;
        }
        private void DrawAppearance(int id)
        {
            GUILayout.Label("GÖRÜNÜM VE YARDIM", heading);
            int nextTab = GUILayout.SelectionGrid(appearanceTab, new[] { "Görünüm", "Tanılama", "Kullanım" }, 3, tabs, GUILayout.Height(36));
            if (nextTab != appearanceTab) QueueAction(() => { appearanceTab = nextTab; appearanceScroll = Vector2.zero; });
            appearanceScroll = GUILayout.BeginScrollView(appearanceScroll, GUILayout.ExpandHeight(true));
            if (appearanceTab == 0) DrawAppearanceSettings();
            else if (appearanceTab == 1) DrawDiagnostics();
            else DrawQuickGuide();
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (appearanceTab == 0)
            {
                if (GUILayout.Button("Varsayılan")) QueueAction(() => { selectedScale = 1; uiScale.Value = 1; preferredWidth = preferredHeight = 0; window.x = window.y = 30; UpdateAppearance(); SaveAppearance(); });
                if (GUILayout.Button("Ölçeği uygula", primary))
                {
                    float scale = selectedScale;
                    QueueAction(() => { uiScale.Value = UiLayout.NormalizeScale(scale); showAppearance = false; });
                }
            }
            if (GUILayout.Button("Panele dön")) QueueAction(() => showAppearance = false);
            GUILayout.EndHorizontal();
        }

        private void DrawAppearanceSettings()
        {
            GUILayout.Label("Menü ölçeği", section);
            selectedScale = GUILayout.HorizontalSlider(selectedScale, 0.75f, 1.5f);
            GUILayout.Label("Seçilen: %" + Mathf.RoundToInt(selectedScale * 100) + "  •  Şu an kullanılan: %" + Mathf.RoundToInt(layout.Scale * 100), muted);
            GUILayout.Label("Küçük ekranlarda panel otomatik olarak sıkıştırılır ve ekrana sığdırılır. Sağ alt köşeyi sürükleyerek pencereyi boyutlandırabilirsin. Konum, boyut ve ölçek sonraki açılışta hatırlanır.");
            var target = UiLayout.ForScreen(Screen.width, Screen.height, selectedScale, preferredWidth, preferredHeight);
            GUILayout.Label("Uygulayınca kullanılacak: %" + Mathf.RoundToInt(target.Scale * 100) + "  •  Ekran: " + Screen.width + "×" + Screen.height, muted);
            if (target.Scale < UiLayout.NormalizeScale(selectedScale))
                GUILayout.Label("Seçtiğin ölçek ekrana sığmıyor; panel %" + Mathf.RoundToInt(target.Scale * 100) + " ölçeğine indirilecek.", errorHint);
            GUILayout.Label("Pencere: " + Mathf.RoundToInt(layout.Width * layout.Scale) + "×" + Mathf.RoundToInt(layout.Height * layout.Scale) + " px", muted);
            if (GUILayout.Button("Pencere boyutunu sıfırla")) QueueAction(() => { preferredWidth = preferredHeight = 0; UpdateAppearance(); SaveAppearance(); });
            GUILayout.Label("Menü dili: Türkçe • Eşya, nadirlik ve efekt metinleri: oyun dili", muted);
            bool logging = GUILayout.Toggle(performanceLog.Value, "Menü sürelerini günlüğe yaz");
            if (logging != performanceLog.Value) QueueAction(() => performanceLog.Value = logging);
        }
    }
}
