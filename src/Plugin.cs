using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using EpicLoot;
using EpicLoot.ShardStones;
using HarmonyLib;
using Jotunn.Utils;
using UnityEngine;

namespace EpicLootSpawner
{
    [BepInPlugin(Id, "EpicLoot Spawner", Version)]
    [BepInDependency("randyknapp.mods.epicloot", "0.15.6")]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public sealed partial class Plugin : BaseUnityPlugin
    {
        public const string Id = "local.epicloot.spawner";
        public const string Version = "1.5.0";
        internal static bool IsOpen;
        private readonly Spawner builder = new Spawner();
        private readonly List<GameObject> items = new List<GameObject>();
        private readonly List<GameObject> stones = new List<GameObject>();
        private readonly Queue<Action> pendingUiActions = new Queue<Action>();
        private void QueueAction(Action action) => pendingUiActions.Enqueue(action);
        private List<MagicItemEffectDefinition> available = new List<MagicItemEffectDefinition>();
        private ConfigEntry<KeyCode> hotkey;
        private ConfigEntry<bool> unlimited;
        private Harmony harmony;
        private PresetStore presets;
        private string presetName = "";
        private Vector2 presetScroll;
        private Rect window = new Rect(30, 30, 1200, 840);
        private Vector2 itemScroll, effectScroll, stoneScroll, recipeScroll;
        private Vector2 previewScroll;
        private bool showPreview;
        private string itemSearch = "", effectSearch = "", stoneSearch = "", status = "";
        private string lastError = "", lastErrorDetail = "", lastOperation = "";
        private string OpenKeyName => hotkey?.Value.ToString() ?? "F8";

        private ItemRarity stoneRarity = ItemRarity.Epic;
        private string preview = "";
        private bool toInventory = true;
        private CursorLockMode previousLock;
        private bool previousVisible;

        private void Awake()
        {
            hotkey = Config.Bind("Arayüz", "Açma tuşu", KeyCode.F8, "Spawner panelini aç/kapat.");
            unlimited = Config.Bind("Tasarım", "Limitsiz mod", false, "Efekt değer aralığı, efekt sayısı ve soket sayısı sınırlarını ve EpicLoot'un efekt içi tavanlarını (direnç sınırı, %100 kırpmaları vb.) kaldırır. Eşya türü uyumluluğu yine denetlenir.");
            builder.Unlimited = unlimited.Value;
            InitializeAppearance();
            harmony = new Harmony(Id);
            ExactWoodDrops.Warning = ex => Logger.LogWarning("Ekstra odun hesabı tamamlanamadı: " + ex.Message);
            harmony.PatchAll(typeof(Plugin).Assembly);
            EffectCaps.Apply(harmony, Logger.LogWarning);
            EffectCaps.Lifted = unlimited.Value;
            presets = new PresetStore(Path.Combine(BepInEx.Paths.ConfigPath, "EpicLootSpawner.presets.json"), Enum.GetValues(typeof(ItemRarity)).Length);
            Run(presets.Load);
            favorites = new FavoriteStore(Path.Combine(BepInEx.Paths.ConfigPath, "EpicLootSpawner.favorites.json"));
            Run(favorites.Load);
            InitializeDraftStore();
            Logger.LogInfo("EpicLoot Spawner " + Version + " hazır. Panel: " + hotkey.Value);
        }

        private void OnDestroy()
        {
            stopProduction = true;
            Close();
            EffectCaps.Lifted = false;
            harmony?.UnpatchSelf();
            ExactWoodDrops.Warning = null;
            itemLabels.Clear();
            DisposeTheme();
        }

        private void Update()
        {
            try { UpdateSpawner(); }
            catch (Exception ex) { FailPanel(ex); }
        }

        private void UpdateSpawner()
        {
            EffectCaps.Lifted = unlimited.Value;
            if (!Player.m_localPlayer)
            {
                pendingUiActions.Clear(); stopProduction = true; if (IsOpen) Close();
                lastInventoryOperation = null; showInventoryUndo = false;
                history.Clear(); observedDesign = null; inventoryKey = null; inventorySources.Clear(); inventoryItems.Clear();
                currentPreviewSignature = null; historyCanUndo = historyCanRedo = false;
                return;
            }
            ObserveDesign();
            while (pendingUiActions.Count > 0) Run(pendingUiActions.Dequeue());
            if (Input.GetKeyDown(hotkey.Value))
            {
                if (IsOpen) Close();
                else Run(OpenPanel);
            }
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                if (picker != PickerKind.None) picker = PickerKind.None;
                else if (showInventoryUndo) showInventoryUndo = false;
                else if (showDisenchant) { showDisenchant = false; disenchantPlan = null; }
                else if (showAppearance) showAppearance = false;
                else if (showRecovery) { showRecovery = false; recoveryCandidate = null; }
                else if (showCollection) showCollection = false;
                else if (showPresetManager) { if (pendingDelete != null) pendingDelete = null; else showPresetManager = false; }
                else if (showTransfer) showTransfer = false;
                else if (showPreview) showPreview = false;
                else if (pendingDelete != null) pendingDelete = null;
                else Close();
            }
            if (builder.Unlimited != unlimited.Value) { builder.Unlimited = unlimited.Value; RefreshEffects(); }
            UpdateInventoryLibrary();
            UpdateDisenchant();
            UpdateInventoryUndo();
            UpdateLivePreview();
            UpdateCatalogViews();
            UpdateSpecialCatalog();
            UpdateStonePreviews();
            UpdatePresetViews();
            if (IsOpen) UpdateAppearance();
            UpdateSessionDraft();
        }

        private void LateUpdate()
        {
            if (IsOpen) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        private void RefreshCatalog()
        {
            if (!ObjectDB.instance) throw new InvalidOperationException("Eşya veritabanı hazır değil.");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            itemLabels.Clear(); nameHeights.Clear(); catalogLanguage = Localization.instance.GetSelectedLanguage();
            presetRevision = -1;
            libraryDirty = true; stoneFilterDirty = true;
            specialCatalogSignature = null;
            items.Clear(); stones.Clear();
            RefreshItemTiers();
            foreach (var prefab in ObjectDB.instance.m_items.Where(x => x).Distinct())
            {
                if (!prefab.TryGetComponent(out ItemDrop drop)) continue;
                if (drop.m_itemData?.m_shared == null) continue;
                if (API.CanBeMagicItem(drop.m_itemData)) items.Add(prefab);
                if (Shards.IsShard(drop.m_itemData)) stones.Add(prefab);
            }
            items.Sort((a, b) => { int order = string.Compare(Label(a), Label(b), StringComparison.CurrentCultureIgnoreCase); return order != 0 ? order : string.Compare(a.name, b.name, StringComparison.Ordinal); });
            stones.Sort((a, b) => { int order = string.Compare(Label(a), Label(b), StringComparison.CurrentCultureIgnoreCase); return order != 0 ? order : string.CompareOrdinal(a.name, b.name); });
            RefreshEffects();
            status = $"{items.Count} eşya, {stones.Count} shardstone bulundu. EpicLoot {API.GetPluginVersion()}";
            lastCatalogMs = (float)timer.Elapsed.TotalMilliseconds;
            if (performanceLog.Value) Logger.LogInfo("Katalog: " + lastCatalogMs.ToString("0.0") + " ms");
        }

        private void RefreshEffects()
        {
            try { available = builder.Prefab ? builder.Available() : new List<MagicItemEffectDefinition>(); }
            catch { available = new List<MagicItemEffectDefinition>(); }
            RefreshEffectCards();
            fieldErrors.Clear();
            preview = "";
            observedSignature = null; previewSignature = null; currentPreviewSignature = null;
        }



    }
}





