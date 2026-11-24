using System.Linq;
// Headless regression harness: links the actual spawner methods and EpicLoot's verified
// Indestructible implementation. Stubs replace Unity, UI, inventory and API plumbing.
// This reproduces state transitions; it does not simulate a running game.
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using EpicLoot;

namespace UnityEngine
{
    public class GameObject
    {
        public string name = "SwordIron";
        public ItemDrop Drop;
        public bool TryGetComponent<T>(out T value) where T : class { value = Drop as T; return value != null; }
        public T GetComponent<T>() where T : class => Drop as T;
        public static implicit operator bool(GameObject value) => value != null;
    }
    public struct Vector2 { public int x, y; public static Vector2 zero => new Vector2(); }
    public sealed class GUIContent { public string text, tooltip; public GUIContent(string text, string tooltip) { this.text = text; this.tooltip = tooltip; } }
    public struct Vector3
    {
        public static Vector3 up => new Vector3();
        public static Vector3 operator +(Vector3 a, Vector3 b) => a;
        public static Vector3 operator *(Vector3 a, float value) => a;
    }
    public struct Quaternion { public static Quaternion identity => new Quaternion(); }
    public static class Random { public static float value => 0.5f; public static Vector3 insideUnitSphere => new Vector3(); }
    public static class Time { public static float unscaledTime; }
    public static class GUI { public static bool enabled = true; }
    public static class GUILayout
    {
        private sealed class Option { public string Kind; public float Value; public Option(string kind, float value) { Kind = kind; Value = value; } }
        public static float ParentRoom = 400, LastScrollHeight;
        public static string ClickTooltip;
        private static bool inScroll;
        private static int row;
        public static void Label(string text, object style = null, params object[] options) { }
        public static void Space(float height) { }
        public static string TextField(string text) => text;
        public static int SelectionGrid(int current, string[] values, int columns, params object[] options) => current;
        public static bool Toggle(bool current, string label) => current;
        public static Vector2 BeginScrollView(Vector2 value, params object[] options)
        {
            // Managed sizing behavior verified in installed UnityEngine.IMGUIModule:
            // GUILayoutEntry.ApplyOptions(minHeight) also raises maxHeight;
            // GUIScrollGroup.CalcHeight restores that max and sets stretchHeight=0.
            // Native painting, font measurement and mouse transforms are not simulated.
            float minimum = 0, maximum = 0; bool stretch = false;
            foreach (object valueOption in options)
                if (valueOption is Option option)
                {
                    if (option.Kind == "Expand") stretch = option.Value > 0;
                    else if (option.Kind == "Min") { minimum = option.Value; if (maximum < minimum) maximum = minimum; }
                    else if (option.Kind == "Height") { minimum = maximum = option.Value; stretch = false; }
                }
            if (maximum != 0) stretch = false;
            LastScrollHeight = stretch ? ParentRoom : maximum > 0 ? maximum : Math.Max(32, minimum);
            inScroll = true; row = -1;
            return value;
        }
        public static void EndScrollView() { inScroll = false; }
        public static void BeginVertical(object style) { }
        public static void EndVertical() { }
        public static void BeginHorizontal() { if (inScroll) row++; }
        public static void EndHorizontal() { }
        public static bool Button(string label, object style = null, params object[] options) => false;
        public static bool Button(GUIContent label, object style, params object[] options)
        {
            float center = row * 40 + 18;
            if (!GUI.enabled || (label.tooltip != ClickTooltip && !label.tooltip.EndsWith("\nPrefab: " + ClickTooltip, StringComparison.Ordinal)) || !inScroll || center < 0 || center >= LastScrollHeight) return false;
            ClickTooltip = null; return true;
        }
        public static object Width(float value) => new Option("Width", value);
        public static object Height(float value) => new Option("Height", value);
        public static object MinHeight(float value) => new Option("Min", value);
        public static object ExpandHeight(bool value) => new Option("Expand", value ? 1 : 0);
    }
}
public class ItemDrop
{
    public ItemData m_itemData;
    public static int Drops, DropAttempts, FailAt;
    public static implicit operator bool(ItemDrop value) => value != null;
    public static ItemDrop DropItem(ItemData item, int units, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation)
    {
        DropAttempts++;
        if (DropAttempts == FailAt) return null;
        Drops += units; return new ItemDrop { m_itemData = item };
    }
    public class ItemData
    {
        public enum ItemType { OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Helmet, Chest, Legs, Shoulder, Shield, Tool, Material }
        public class SharedData { public bool m_useDurability = true; public string m_name = "$item_swordiron"; public int m_maxStackSize = 50; public ItemType m_itemType; }
        public SharedData m_shared = new SharedData();
        public Dictionary<string, string> m_customData = new Dictionary<string, string>();
        public UnityEngine.GameObject m_dropPrefab;
        public int m_stack = 1, m_quality = 1, m_variant;
        public float m_durability = 50, m_worldLevel;
        public bool m_equipped;
        public UnityEngine.Vector2 m_gridPos;
        public MagicItem Magic;
        public static bool ForceCloneSync, ShareCloneMagic;
        public ItemData Clone()
        {
            var item = (ItemData)MemberwiseClone(); item.m_customData = new Dictionary<string, string>(m_customData);
            item.Magic = ShareCloneMagic ? Magic : Magic == null ? null : JsonConvert.DeserializeObject<MagicItem>(JsonConvert.SerializeObject(Magic));
            if (ForceCloneSync && item.Magic != null) EpicLoot.MagicItemEffects.Indestructible.Sync(item);
            return item;
        }
        public string GetTooltip() => m_shared.m_name;
        public float GetMaxDurability() => 100 + (m_quality - 1) * 50;
    }
}
public class Inventory
{
    public List<ItemDrop.ItemData> Items = new List<ItemDrop.ItemData>();
    public List<ItemDrop.ItemData> GetAllItems() => Items;
    public int Capacity = 32, AddCalls, FailAt, ChangedCalls, FailChangedAt;
    public Action OnAdd, OnRemove;
    public int RemoveCalls, FailRemoveAt;
    public Inventory() { }
    public Inventory(string name, object bkg, int width, int height) { Capacity = width * height; }
    public string GetName() => "Test";
    public object GetBkg() => null;
    public int GetWidth() => Capacity;
    public int GetHeight() => 1;
    public void Changed() { if (++ChangedCalls == FailChangedAt) throw new InvalidOperationException("injected inventory change failure"); }
    public bool AddItem(ItemDrop.ItemData item)
    {
        AddCalls++;
        var existing = Items.FirstOrDefault(x => x.m_shared.m_name == item.m_shared.m_name && x.m_quality == item.m_quality && x.m_worldLevel == item.m_worldLevel && x.m_stack < x.m_shared.m_maxStackSize);
        if (existing != null) existing.m_stack++;
        else if (Items.Count < Capacity) Items.Add(item);
        else return false;
        OnAdd?.Invoke();
        if (AddCalls == FailAt) return false;
        Changed(); return true;
    }
    public bool RemoveItem(ItemDrop.ItemData item, int count)
    {
        if (!Items.Contains(item)) return false;
        RemoveCalls++;
        item.m_stack -= Math.Min(count, item.m_stack);
        if (item.m_stack == 0) Items.Remove(item);
        OnRemove?.Invoke();
        if (RemoveCalls == FailRemoveAt) return false;
        Changed(); return true;
    }
    public void AddItem(ItemDrop.ItemData item, int count, int x, int y, bool flag) { Items.Add(item); }
}
public class Character { }
public class Player : Character
{
    public static Player m_localPlayer = new Player();
    public Inventory Inventory = new Inventory();
    public Inventory GetInventory() => Inventory;
    public ItemDrop.ItemData Weapon;
    public ItemDrop.ItemData GetCurrentWeapon() => Weapon;
}
public class DropTable
{
    public class DropEntry { public UnityEngine.GameObject m_item; public float m_weight = 1; }
    public List<DropEntry> m_drops = new List<DropEntry>();
}
public class ObjectDB
{
    public static ObjectDB instance = new ObjectDB();
    public readonly Dictionary<string, UnityEngine.GameObject> Items = new Dictionary<string, UnityEngine.GameObject>();
    public UnityEngine.GameObject GetItemPrefab(string name) => Items.TryGetValue(name, out var item) ? item : null;
}
namespace EpicLoot
{
    public enum ItemRarity { Magic, Rare, Epic, Legendary, Mythic, Ancient }
    public class MagicItemEffect { public string EffectType; public float EffectValue; }
    public class SocketedEffect { public string SourcePrefab; public MagicItemEffect Effect; }
    public class MagicItemEffectDefinition
    {
        public string Type, Title, Description;
        public bool CanBeAugmented = true;
        public class ValueDef { public float MinValue, MaxValue; }
        public ValueDef Range;
        public ValueDef GetValuesForRarity(ItemRarity rarity) => Range;
    }
    public static class MagicItemEffectDefinitions
    {
        public static Dictionary<string, MagicItemEffectDefinition> AllDefinitions = new Dictionary<string, MagicItemEffectDefinition>();
        public static MagicItemEffectDefinition.ValueDef GetRollRange(MagicItemEffectDefinition definition, ItemRarity rarity, string id = null)
            => LegendarySystem.UniqueLegendaryHelper.GetLegendaryEffectValues(id, definition?.Type) ?? definition?.GetValuesForRarity(rarity);
    }
    public partial class MagicItem
    {
        public ItemRarity Rarity; public int SocketCount, AugmentedEffectIndex = -1;
        public string DisplayName, LegendaryID, SetID, TypeNameOverride;
        public bool IsUnidentified;
        public List<MagicItemEffect> Effects = new List<MagicItemEffect>();
        public List<SocketedEffect> Sockets = new List<SocketedEffect>();
        public List<int> AugmentedEffectIndices = new List<int>(), TemperedEffectIndices = new List<int>();
        public static int FormatCalls;
        public static string GetEffectText(MagicItemEffectDefinition definition, float value) { FormatCalls++; return string.Format(Localization.instance.Localize(definition.Title ?? definition.Type), value); }
        public static string GetEffectDescription(MagicItemEffectDefinition definition, float value) { FormatCalls++; return string.Format(Localization.instance.Localize(definition.Description ?? definition.Type), value); }
    }
    public class MagicItemComponent { }
    public static class MagicEffectType { public const string Indestructible = "Indestructible", IncreaseTreeDrop = "IncreaseTreeDrop"; }
    public static class API
    {
        public static ItemDrop.ItemData FailTarget;
        public static int FailWrites;
        public static string GetMagicItemJson(ItemDrop.ItemData item) => item.Magic == null ? null : JsonConvert.SerializeObject(item.Magic);
        public static bool ApplyMagicItemJson(ItemDrop.ItemData item, string json)
        {
            item.Magic = JsonConvert.DeserializeObject<MagicItem>(json);
            item.m_customData["magic"] = json;
            MagicItemEffects.Indestructible.Sync(item); // same write funnel as installed EpicLoot
            if (item == FailTarget && FailWrites > 0) { FailWrites--; return false; }
            return true;
        }
        public static bool CanBeMagicItem(ItemDrop.ItemData item) => true;
        public static Func<ItemDrop.ItemData, string> DisplayNameOverride;
        public static string GetItemDisplayName(ItemDrop.ItemData item) => DisplayNameOverride?.Invoke(item) ?? "SwordIron";
        public static string GetRarityDisplayNameByIndex(int rarity) => ((ItemRarity)rarity).ToString();
    }
    public static class Extensions
    {
        public static MagicItem GetMagicItem(this ItemDrop.ItemData item) => item.Magic;
        public static bool HasMagicEffect(this ItemDrop.ItemData item, string type) => item.Magic?.GetEffects(type, includeSocketed: true).Count > 0;
        // Mirrors EpicLoot 0.15.6 ItemDataExtensions.RemoveMagicItem -> MagicItemData.Remove:
        // drops the cached component, reverts Indestructible and removes the stored magic key.
        public static bool RemoveMagicItem(this ItemDrop.ItemData item)
        {
            bool had = item.Magic != null;
            item.Magic = null;
            EpicLoot.MagicItemEffects.Indestructible.Revert(item);
            return item.m_customData.Remove("magic") || had;
        }
    }
}
namespace EpicLoot.ShardStones { public static class Shards { public static bool IsShard(ItemDrop.ItemData item) => true; public static ItemRarity GetShardRarity(ItemDrop.ItemData item) => item.Magic?.Rarity ?? ItemRarity.Epic; } }
namespace EpicLoot.MagicItemEffects { public class IncreaseDrop { public string MagicEffect; public void TryDropExtraItems(Character character, DropTable table, UnityEngine.Vector3 position) { } } }
namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    public static class MagicEffectsHelper
    {
        public static float Total;
        public static bool ThrowOnRead;
        public static bool HasActiveMagicEffectOnWeapon(Player player, ItemDrop.ItemData weapon, string type, out float value)
        {
            if (ThrowOnRead) throw new InvalidOperationException("test read failure");
            value = Total; return value != 0;
        }
    }
}
namespace HarmonyLib
{
    public class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string name, params Type[] arguments) { } }
    public static class AccessTools { public static System.Reflection.MethodInfo DeclaredMethod(Type type, string name) => type.GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); }
}
namespace JetBrains.Annotations { public class UsedImplicitlyAttribute : Attribute { } }
namespace BepInEx { public static class Paths { public static string ConfigPath; } }
public class Localization
{
    public static Localization instance = new Localization();
    public string Language = "en";
    public Dictionary<string, string> Text = new Dictionary<string, string>();
    public string Localize(string text) => text != null && Text.TryGetValue(text, out string result) ? result : text;
    public string GetSelectedLanguage() => Language;
}

namespace EpicLootSpawner
{
    internal class Spawner
    {
        public UnityEngine.GameObject Prefab;
        public ItemDrop.ItemData Designed;
        public Recipe State = new Recipe();
        public List<EffectChoice> Effects => State.Effects;
        public List<string> Stones => State.Stones;
        public string Quality { get => State.Quality; set => State.Quality = value; }
        public string SocketCount { get => State.SocketCount; set => State.SocketCount = value; }
        public string LegendaryID { get => State.LegendaryID; set => State.LegendaryID = value; }
        public string SetID { get => State.SetID; set => State.SetID = value; }
        public string Name { get => State.Name; set => State.Name = value; }
        public bool IsSpecial => SpecialItems.HasIdentity(LegendaryID, SetID);
        public ItemDrop.ItemData BaseItem() => Prefab.GetComponent<ItemDrop>().m_itemData;
        public MagicItemEffectDefinition.ValueDef EffectRange(MagicItemEffectDefinition definition) => SpecialItems.RollRange(definition, Rarity, LegendaryID);
        public void SelectSpecial(string id) { LegendaryID = id; SetID = id == null ? null : SpecialItems.Set(SpecialItems.Resolve(id)); }
        public ItemRarity Rarity = ItemRarity.Epic;
        public ItemDrop.ItemData Build() => Designed;
        public static int DraftCalls; public Recipe Draft() { DraftCalls++; if (Prefab) State.Prefab = Prefab.name; return State; }
        public void RestoreDraft(Recipe recipe) { if (recipe.Prefab == "MissingPrefab") throw new InvalidOperationException("missing"); State = recipe; }
        public void Import(Recipe recipe) { recipe.Validate(6); RestoreDraft(recipe); }
    }
    public partial class Plugin
    {
        public const string Id = "local.epicloot.spawner";
        private Spawner builder = new Spawner();
        private bool IsOpen = true, toInventory = true;
        private ItemRarity stoneRarity = ItemRarity.Epic;
        private int RarityCount => 6;
        private object production; private int libraryTab; private PickerKind picker;
        private string status, observedSignature, previewSignature;
        private System.Collections.Generic.Dictionary<string, string> fieldErrors = new System.Collections.Generic.Dictionary<string, string>();
        private object card, section, muted, selected, tabs, heading, primary, errorHint, chip, caption, body;
        private class ButtonStyle { public float CalcHeight(UnityEngine.GUIContent content, float width) => 44; }
        private class Skin { public ButtonStyle button = new ButtonStyle(); }
        private Skin theme = new Skin();
        private class Log { public void LogWarning(object value) { } }
        private Log Logger = new Log();
        private List<UnityEngine.GameObject> items = new List<UnityEngine.GameObject>();
        private List<UnityEngine.GameObject> stones = new List<UnityEngine.GameObject>();
        private List<MagicItemEffectDefinition> available = new List<MagicItemEffectDefinition>();
        private string itemSearch = "", effectSearch = "", stoneSearch = "";
        private UnityEngine.Vector2 itemScroll;
        private UiLayout layout = UiLayout.ForScreen(1920, 1080, 1);
        private void RefreshEffects() { }
        private void Run(Action action) { try { action(); } catch (Exception ex) { status = ex.Message; } }
        private readonly Queue<Action> pendingActions = new Queue<Action>();
        private void QueueAction(Action action) => pendingActions.Enqueue(action);
        private void DrawIcon(UnityEngine.GameObject prefab, int size) { }
        private string SearchInput(string value, string hint) => value;
        // Name wrapping/font measurement is native Unity; this harness exercises clicks only.
        private bool NameButton(string title, string tooltip, bool current, float trailingWidth, bool insetCard = false, float? widthOverride = null)
            => UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(title, tooltip), current ? selected : theme.button);
        private void RefreshCatalog() { itemLabels.Clear(); catalogLanguage = Localization.instance.GetSelectedLanguage(); libraryDirty = stoneFilterDirty = true; RefreshItemTiers(); RefreshEffectCards(); }

        private static ItemDrop.ItemData DesignedItem(UnityEngine.GameObject prefab, bool indestructible, int quality = 1)
        {
            var item = new ItemDrop.ItemData { m_dropPrefab = prefab, m_quality = quality };
            API.ApplyMagicItemJson(item, indestructible ? "{\"Effects\":[{\"EffectType\":\"Indestructible\",\"EffectValue\":1}]}" : "{\"Effects\":[]}");
            return item;
        }
        private static Plugin EditingPlugin(ItemDrop.ItemData original, ItemDrop.ItemData designed)
        {
            Player.m_localPlayer.Inventory.Items.Add(original);
            var plugin = new Plugin(); plugin.builder.Prefab = original.m_dropPrefab; plugin.builder.Designed = designed;
            plugin.inventoryKey = "source";
            plugin.inventorySources["source"] = new InventorySource { Item = original, Prefab = original.m_dropPrefab, Fingerprint = ItemFingerprint(original) };
            return plugin;
        }
        public static void CheckIndestructible()
        {
            foreach (bool cloneSync in new[] { false, true })
            {
                ItemDrop.ItemData.ForceCloneSync = cloneSync;
                var prefab = new UnityEngine.GameObject();
                var original = new ItemDrop.ItemData { m_dropPrefab = prefab, m_variant = 7, m_gridPos = new UnityEngine.Vector2 { x = 3, y = 2 } };
                original.m_customData["other.mod"] = "kept";
                var plugin = EditingPlugin(original, DesignedItem(prefab, true, 3));
                var preview = PrepareInventoryEdit(original, plugin.builder.Build());
                Program.Assert(original.m_shared.m_useDurability && original.Magic == null, "preview must not edit original");
                Program.Assert(!preview.m_shared.m_useDurability && preview.m_durability == 100, "preview wear ratio");
                plugin.ApplyInventoryEdit();
                Program.Assert(!original.m_shared.m_useDurability && EpicLoot.MagicItemEffects.Indestructible.OriginallyUsesDurability(original), "adding Indestructible keeps true baseline");
                Program.Assert(original.m_quality == 3 && original.m_durability == 100, "apply preserves wear ratio");
                Program.Assert(original.m_variant == 7 && original.m_gridPos.x == 3 && original.m_gridPos.y == 2 && original.m_customData["other.mod"] == "kept", "apply preserves identity and foreign data");
                plugin.builder.Designed = DesignedItem(prefab, false, 3);
                preview = PrepareInventoryEdit(original, plugin.builder.Build());
                Program.Assert(preview.m_shared.m_useDurability && !original.m_shared.m_useDurability, "removal preview is isolated and correct");
                plugin.ApplyInventoryEdit();
                Program.Assert(!original.HasMagicEffect(MagicEffectType.Indestructible) && original.m_shared.m_useDurability, "removing effect restores durability");
                plugin.builder.Designed = DesignedItem(prefab, true, 3); plugin.ApplyInventoryEdit();
                plugin.builder.Designed = DesignedItem(prefab, false, 3); plugin.ApplyInventoryEdit();
                Program.Assert(original.m_shared.m_useDurability, "second add/remove cycle restores durability");
                plugin.builder.Designed = DesignedItem(prefab, false, 30);
                var highPreview = PrepareInventoryEdit(original, plugin.builder.Build());
                Program.Assert(highPreview.m_quality == 30 && original.m_quality == 3, "high quality inventory preview is isolated");
                plugin.ApplyInventoryEdit();
                Program.Assert(original.m_quality == 30 && Math.Abs(original.m_durability / original.GetMaxDurability() - 0.5f) < 0.0001f, "apply keeps high quality and wear ratio");

                foreach (bool oldIndestructible in new[] { false, true })
                {
                    var target = DesignedItem(prefab, oldIndestructible);
                    target.m_durability = 35; target.m_customData["other.mod"] = "saved";
                    string before = API.GetMagicItemJson(target);
                    var failed = EditingPlugin(target, DesignedItem(prefab, !oldIndestructible, 4));
                    API.FailTarget = target; API.FailWrites = 1;
                    bool rejected = false;
                    try { failed.ApplyInventoryEdit(); } catch (InvalidOperationException) { rejected = true; }
                    API.FailTarget = null;
                    Program.Assert(rejected && target.m_quality == 1 && target.m_durability == 35 && target.m_customData["other.mod"] == "saved", "failed apply restores vanilla state");
                    Program.Assert(API.GetMagicItemJson(target) == before && target.m_shared.m_useDurability == !oldIndestructible, "failed apply restores magic and flags");
                    Program.Assert(EpicLoot.MagicItemEffects.Indestructible.OriginallyUsesDurability(target), "rollback keeps true durability baseline");
                    failed.builder.Designed = DesignedItem(prefab, false); failed.ApplyInventoryEdit();
                    Program.Assert(target.m_shared.m_useDurability, "removal works after rollback");
                }
                var noDurability = new ItemDrop.ItemData { m_dropPrefab = prefab };
                noDurability.m_shared.m_useDurability = false;
                var nondurable = EditingPlugin(noDurability, DesignedItem(prefab, true)); nondurable.ApplyInventoryEdit();
                nondurable.builder.Designed = DesignedItem(prefab, false); nondurable.ApplyInventoryEdit();
                Program.Assert(!noDurability.m_shared.m_useDurability, "naturally nondurable item stays nondurable");
            }
        }
        public static void CheckDraftProtection(string folder)
        {
            BepInEx.Paths.ConfigPath = folder;
            string file = Path.Combine(folder, "EpicLootSpawner.draft.json");
            File.WriteAllText(file, JsonConvert.SerializeObject(new SavedDraft { Recipe = new Recipe { Prefab = "MissingPrefab" } }));
            var plugin = new Plugin(); plugin.InitializeDraftStore(); plugin.Run(plugin.RestoreSessionDraft);
            UnityEngine.Time.unscaledTime = 0; plugin.UpdateSessionDraft();
            UnityEngine.Time.unscaledTime = 3; plugin.UpdateSessionDraft();
            Program.Assert(JsonConvert.DeserializeObject<SavedDraft>(File.ReadAllText(file)).Recipe.Prefab == "MissingPrefab", "missing prefab draft protected initially");
            plugin.toInventory = false;
            UnityEngine.Time.unscaledTime = 4; plugin.UpdateSessionDraft();
            UnityEngine.Time.unscaledTime = 7; plugin.UpdateSessionDraft();
            Program.Assert(JsonConvert.DeserializeObject<SavedDraft>(File.ReadAllText(file)).Recipe.Prefab == "MissingPrefab", "delivery preference must not unlock draft");
            plugin.stoneRarity = ItemRarity.Ancient;
            UnityEngine.Time.unscaledTime = 8; plugin.UpdateSessionDraft();
            UnityEngine.Time.unscaledTime = 11; plugin.UpdateSessionDraft();
            Program.Assert(JsonConvert.DeserializeObject<SavedDraft>(File.ReadAllText(file)).Recipe.Prefab == "MissingPrefab", "stone picker rarity must not unlock draft");
            plugin.builder.State.Quality = "unfinished";
            UnityEngine.Time.unscaledTime = 12; plugin.UpdateSessionDraft();
            UnityEngine.Time.unscaledTime = 15; plugin.UpdateSessionDraft();
            Program.Assert(JsonConvert.DeserializeObject<SavedDraft>(File.ReadAllText(file)).Recipe.Prefab == "MissingPrefab", "empty recipe input must not unlock draft");
            plugin.builder.State.Prefab = "SwordIron";
            plugin.SaveSessionDraft();
            Program.Assert(JsonConvert.DeserializeObject<SavedDraft>(File.ReadAllText(file)).Recipe.Prefab == "SwordIron", "new item recipe unlocks even before next Update");
            Program.Assert(JsonConvert.DeserializeObject<SavedDraft>(File.ReadAllText(file + ".bak")).Recipe.Prefab == "MissingPrefab", "replaced draft retained as backup");
        }

        public static void CheckComparison()
        {
            var prefab = new UnityEngine.GameObject();
            var original = DesignedItem(prefab, false);
            original.Magic.Effects.Add(new MagicItemEffect { EffectType = "Wood", EffectValue = 10 });
            original.Magic.SocketCount = 2;
            original.Magic.Sockets.Add(new SocketedEffect { SourcePrefab = "Yellow", Effect = new MagicItemEffect { EffectType = "Wood", EffectValue = 1.5f } });
            original.Magic.Sockets.Add(new SocketedEffect { SourcePrefab = "Inert" });
            var candidate = original.Clone(); candidate.Magic.Effects[0].EffectValue = 15;
            candidate.Magic.Sockets[0].Effect.EffectValue = 2.5f;
            var before = Snapshot(original); var after = Snapshot(candidate);
            Program.Assert(before.Rolled["Wood"] == 10 && before.Totals["Wood"] == 11.5f, "snapshot separates rolled and socket contributions using EpicLoot methods");
            Program.Assert(after.Rolled["Wood"] == 15 && after.Totals["Wood"] == 17.5f, "snapshot uses recalculated socket values");
            Program.Assert(before.Stones.Count == 2 && before.Stones[1] == "Inert", "inert stone retained without fake effect");
            var plugin = new Plugin();
            string description = plugin.DescribeComparison(original, candidate);
            Program.Assert(description.Contains("Eşyanın efektleri") && description.Contains("Toplam etkiler"), "summary includes base and final effect changes");
            Program.Assert(original.Magic.Effects[0].EffectValue == 10 && original.Magic.Sockets[0].Effect.EffectValue == 1.5f, "comparison leaves original untouched");
            Program.Assert(plugin.DescribeComparison(original, original.Clone()).Contains("tasarımı aynı"), "identical actual item has no changes");
            candidate.Magic.Effects[0].EffectValue = 10;
            var onlySocket = ItemDifference.Between(Snapshot(original), Snapshot(candidate));
            Program.Assert(onlySocket.Rolled.Count == 0 && onlySocket.Totals.Count == 1, "socket-only value change appears in totals");
            candidate.Magic.DisplayName = "<b>Ad</b>";
            Program.Assert(plugin.DescribeComparison(original, candidate).Contains("‹b›Ad‹/b›"), "user name shown as text in summary");
            var plain = new ItemDrop.ItemData();
            Program.Assert(Snapshot(plain).Rarity == null && Snapshot(plain).Totals.Count == 0, "plain item snapshot supports nonmagic source");
            Program.Assert(plugin.DescribeComparison(plain, original).Contains("Büyüsüz → Magic"), "plain to magic rarity shown");
            candidate.Magic.Effects.Clear(); candidate.Magic.Sockets.Clear();
            Program.Assert(ItemDifference.Between(Snapshot(original), Snapshot(candidate)).Rolled[0].Kind == ChangeKind.Removed, "removal uses effect identity");
        }

        public static void CheckCatalog(string folder)
        {
            UnityEngine.GameObject Prefab(string id, string label, ItemDrop.ItemData.ItemType type, ItemRarity rarity = ItemRarity.Epic)
            {
                var drop = new ItemDrop { m_itemData = new ItemDrop.ItemData { Magic = new MagicItem { Rarity = rarity } } };
                drop.m_itemData.m_shared.m_name = label; drop.m_itemData.m_shared.m_itemType = type;
                return new UnityEngine.GameObject { name = id, Drop = drop };
            }
            var plugin = new Plugin();
            plugin.favorites = new FavoriteStore(Path.Combine(folder, "catalog-favorites.json")); plugin.favorites.Load();
            var sword = Prefab("SwordIron", "$sword", ItemDrop.ItemData.ItemType.OneHandedWeapon);
            var helmet = Prefab("HelmetIron", "$helmet", ItemDrop.ItemData.ItemType.Helmet);
            Localization.instance.Text["$sword"] = "Iron Sword"; Localization.instance.Text["$helmet"] = "Iron Helmet";
            plugin.items.Add(sword); plugin.items.Add(helmet);
            plugin.stones.Add(Prefab("YellowEpic", "$yellow", ItemDrop.ItemData.ItemType.Material));
            plugin.stones.Add(Prefab("RedMagic", "$red", ItemDrop.ItemData.ItemType.Material, ItemRarity.Magic));
            plugin.available.Add(new MagicItemEffectDefinition { Type = "Z", Title = "Zırh", Description = "savunma" });
            plugin.available.Add(new MagicItemEffectDefinition { Type = "A", Title = "Ahşap", Description = "odun" });
            plugin.RefreshCatalog(); plugin.UpdateCatalogViews();
            Program.Assert(plugin.filteredItems.Length == 2 && plugin.filteredStones.Length == 1, "initial catalog and stone rarity");
            Program.Assert(plugin.filteredEffects[0].Definition.Type == "A", "effect cards sorted by display name");
            var sameItems = plugin.filteredItems; var sameEffects = plugin.filteredEffects; int calls = MagicItem.FormatCalls;
            plugin.UpdateCatalogViews(); plugin.UpdateCatalogViews();
            Program.Assert(ReferenceEquals(sameItems, plugin.filteredItems) && ReferenceEquals(sameEffects, plugin.filteredEffects) && calls == MagicItem.FormatCalls, "unchanged catalog does not rebuild filters or reformat effect cards");
            plugin.itemSearch = "sWoRd"; plugin.libraryDirty = true; plugin.UpdateCatalogViews();
            Program.Assert(plugin.filteredItems.Length == 1 && plugin.filteredItems[0] == sword, "localized name search ignores case");
            plugin.category = 2; plugin.libraryDirty = true; plugin.UpdateCatalogViews();
            Program.Assert(plugin.filteredItems.Length == 0, "category combines with search");
            plugin.itemSearch = ""; plugin.category = 1; plugin.favoritesOnly = true; plugin.libraryDirty = true; plugin.UpdateCatalogViews();
            Program.Assert(plugin.filteredItems.Length == 0, "favorites filter excludes unstarred item");
            plugin.favorites.Toggle("SwordIron"); plugin.libraryDirty = true; plugin.UpdateCatalogViews();
            Program.Assert(plugin.filteredItems.Length == 1 && plugin.filteredItems[0] == sword, "favorite mutation reflected after invalidation");
            plugin.effectSearch = "ODUN"; plugin.effectFilterDirty = true; plugin.UpdateCatalogViews();
            Program.Assert(plugin.filteredEffects.Length == 1 && plugin.filteredEffects[0].Definition.Type == "A", "effect description search uses cached card");
            plugin.stoneRarity = ItemRarity.Magic; plugin.stoneFilterDirty = true; plugin.UpdateCatalogViews();
            Program.Assert(plugin.filteredStones.Length == 1 && plugin.filteredStones[0].name == "RedMagic", "stone rarity refresh replaces filter");
            plugin.stoneSearch = "missing"; plugin.stoneFilterDirty = true; plugin.UpdateCatalogViews();
            Program.Assert(plugin.filteredStones.Length == 0, "stone search combines with rarity");
            Program.Assert(Label(sword) == "Iron Sword", "item label localized");
            Localization.instance.Text["$sword"] = "Demir Kılıç"; Localization.instance.Language = "tr";
            plugin.UpdateCatalogViews();
            Program.Assert(Label(sword) == "Demir Kılıç", "language change refreshes label cache");
            plugin.available.Clear(); plugin.RefreshEffectCards(); plugin.UpdateCatalogViews();
            Program.Assert(plugin.filteredEffects.Length == 0, "unavailable effects removed from cached view");
        }

        public static void CheckItemSelection(string folder)
        {
            var plugin = new Plugin();
            plugin.favorites = new FavoriteStore(Path.Combine(folder, "selection-favorites.json")); plugin.favorites.Load();
            var first = new UnityEngine.GameObject { name = "Staff", Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
            var second = new UnityEngine.GameObject { name = "SwordIron", Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
            var third = new UnityEngine.GameObject { name = "AxeIron", Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
            plugin.filteredItems = new[] { first, second, third }; plugin.items.AddRange(plugin.filteredItems);
            plugin.builder.Quality = "4"; plugin.builder.SocketCount = "2";
            plugin.builder.Effects.Add(new EffectChoice { Type = "Wood", Value = "10" }); plugin.builder.Stones.Add("Yellow");
            plugin.inventoryKey = "previous-inventory-item";
            UnityEngine.GUI.enabled = true; UnityEngine.GUILayout.ParentRoom = 400;
            // Negative control reproduces the 0.7.0 option combination.
            UnityEngine.GUILayout.BeginScrollView(new UnityEngine.Vector2(), UnityEngine.GUILayout.ExpandHeight(true), UnityEngine.GUILayout.MinHeight(48));
            Program.Assert(UnityEngine.GUILayout.LastScrollHeight == 48, "0.7.0 minimum option caps the scroll viewport");
            UnityEngine.GUILayout.EndScrollView();
            UnityEngine.GUILayout.ClickTooltip = "SwordIron";
            plugin.DrawItemLibrary();
            Program.Assert(UnityEngine.GUILayout.LastScrollHeight == 400, "item viewport must use remaining column height instead of collapsing to 48 pixels");
            Program.Assert(plugin.builder.Prefab == null && plugin.pendingActions.Count == 1, "second-row click queues selection without editing during GUI drawing");
            while (plugin.pendingActions.Count > 0) plugin.Run(plugin.pendingActions.Dequeue());
            Program.Assert(plugin.builder.Prefab == second && plugin.inventoryKey == null, "queued second-row selection enters production mode for the clicked prefab");
            Program.Assert(plugin.builder.Quality == "1" && plugin.builder.SocketCount == "0" && plugin.builder.Effects.Count == 0 && plugin.builder.Stones.Count == 0, "item selection resets incompatible design choices");
            Program.Assert(plugin.history.CanUndo, "selection is recorded by real design history");
            UnityEngine.GUI.enabled = false; UnityEngine.GUILayout.ClickTooltip = "AxeIron";
            plugin.DrawItemLibrary();
            Program.Assert(plugin.pendingActions.Count == 0 && plugin.builder.Prefab == second && !UnityEngine.GUI.enabled, "disabled panel does not accept item clicks or leak enabled state");
            UnityEngine.GUI.enabled = true;
            plugin.DrawItemLibrary();
            while (plugin.pendingActions.Count > 0) plugin.Run(plugin.pendingActions.Dequeue());
            Program.Assert(plugin.builder.Prefab == third, "later visible row remains selectable after the first selection");
            UnityEngine.GUILayout.ClickTooltip = null;
        }
    }
}
public static class Program
{
    private static int checks;
    public static void Assert(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        checks++;
    }
    private static bool WoodPrefix(EpicLoot.MagicItemEffects.IncreaseDrop effect, Character player, DropTable table)
        => (bool)typeof(EpicLootSpawner.ExactWoodDrops).GetMethod("Prefix", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Invoke(null, new object[] { effect, player, table, new UnityEngine.Vector3() });

    private static void CheckWoodBonus()
    {
        var player = Player.m_localPlayer;
        player.Weapon = new ItemDrop.ItemData();
        player.Weapon.m_customData[EpicLootSpawner.ExactWoodDrops.Marker] = "1";
        var wood = new UnityEngine.GameObject { name = "Wood", Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
        var table = new DropTable(); table.m_drops.Add(new DropTable.DropEntry { m_item = wood });
        var effect = new EpicLoot.MagicItemEffects.IncreaseDrop { MagicEffect = MagicEffectType.IncreaseTreeDrop };
        int warnings = 0;
        EpicLootSpawner.ExactWoodDrops.Warning = ex => warnings++;
        foreach (var sample in new[] { (30f, 30), (31.5f, 31), (1.5f, 1), (0.5f, 0), (130f, 130) })
        {
            ItemDrop.Drops = ItemDrop.DropAttempts = ItemDrop.FailAt = 0;
            EpicLoot.src.Magic.MagicItemEffects.Helpers.MagicEffectsHelper.Total = sample.Item1;
            Assert(!WoodPrefix(effect, player, table), "exact bonus replaces original path");
            Assert(ItemDrop.Drops == sample.Item2, "whole and fractional equipment totals preserve expected count");
        }
        Assert(warnings == 0, "fractions do not produce warnings or exceptions");
        ItemDrop.Drops = ItemDrop.DropAttempts = 0; ItemDrop.FailAt = 2;
        EpicLoot.src.Magic.MagicItemEffects.Helpers.MagicEffectsHelper.Total = 130;
        Assert(!WoodPrefix(effect, player, table) && ItemDrop.Drops == 50, "partial failure does not restart original bonus");
        Assert(warnings == 1, "partial failure is reported");
        ItemDrop.Drops = ItemDrop.DropAttempts = 0; ItemDrop.FailAt = 1;
        Assert(!WoodPrefix(effect, player, table) && ItemDrop.Drops == 0, "first delivery failure does not escape tree flow");
        ItemDrop.FailAt = 0;
        EpicLoot.src.Magic.MagicItemEffects.Helpers.MagicEffectsHelper.ThrowOnRead = true;
        Assert(WoodPrefix(effect, player, table), "pre-delivery read failure falls back to original bonus");
        EpicLoot.src.Magic.MagicItemEffects.Helpers.MagicEffectsHelper.ThrowOnRead = false;
        player.Weapon.m_customData.Clear();
        Assert(WoodPrefix(effect, player, table), "ordinary weapon preserves EpicLoot bonus path");
        player.Weapon.m_customData[EpicLootSpawner.ExactWoodDrops.Marker] = "1";
        Assert(WoodPrefix(effect, new Character(), table), "nonlocal attacker preserves original path");
        EpicLoot.src.Magic.MagicItemEffects.Helpers.MagicEffectsHelper.Total = float.NaN;
        Assert(WoodPrefix(effect, player, table), "nonfinite total falls back without throwing");
        EpicLootSpawner.ExactWoodDrops.Warning = null;
    }
    public static void CleanupFolder(string folder)
    {
        string fullPath = Path.GetFullPath(folder);
        string tempPath = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
        if (!string.Equals(Path.GetDirectoryName(fullPath), tempPath, StringComparison.OrdinalIgnoreCase)
            || !Path.GetFileName(fullPath).StartsWith("Spawner", StringComparison.Ordinal))
            throw new ArgumentException("Cleanup is restricted to owned Spawner test folders in the temporary directory.");
        // Windows can keep delete-pending files visible to RemoveDirectory briefly.
        // Only retry housekeeping; permanent failures and test exceptions propagate.
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                foreach (string file in Directory.GetFiles(fullPath)) File.Delete(file);
                Directory.Delete(fullPath); return;
            }
            catch (IOException) when (attempt < 6) { System.Threading.Thread.Sleep(100 * (attempt + 1)); }
        }
    }

    public static void Main()
    {
        EpicLootSpawner.Plugin.CheckDisenchant();
        EpicLootSpawner.Plugin.CheckInventoryOperations();
        EpicLootSpawner.Plugin.CheckInventoryPresentation();
        EpicLootSpawner.Plugin.CheckSpecialInventory();
        EpicLootSpawner.Plugin.CheckIndestructible();
        EpicLootSpawner.Plugin.CheckComparison();
        EpicLootSpawner.Plugin.CheckRetentionAndHistory(); EpicLootSpawner.Plugin.CheckSortModes();
        EpicLootSpawner.Plugin.CheckTierSort(); EpicLootSpawner.Plugin.CheckGameplayLanguage();
        string folder = Path.Combine(Path.GetTempPath(), "SpawnerReview-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try { EpicLootSpawner.Plugin.CheckDraftProtection(folder); EpicLootSpawner.Plugin.CheckCatalog(folder); EpicLootSpawner.Plugin.CheckItemSelection(folder); EpicLootSpawner.Plugin.CheckReview(folder); }
        finally { CleanupFolder(folder); }
        CheckWoodBonus();
        Console.WriteLine($"PASS: {checks} linked-source behavior checks (Unity/API stubs, verified EpicLoot Indestructible and totals fixtures)");
    }
}
