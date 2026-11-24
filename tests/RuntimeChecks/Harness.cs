using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using EpicLoot;

namespace UnityEngine
{
    public class GameObject
    {
        public string name;
        public ItemDrop Drop;
        public T GetComponent<T>() where T : class => Drop as T;
        public bool TryGetComponent<T>(out T result) where T : class { result = Drop as T; return result != null; }
        public static implicit operator bool(GameObject obj) => obj != null;
    }
    public struct Vector3
    {
        public static Vector3 up => new Vector3();
        public static Vector3 operator +(Vector3 a, Vector3 b) => a;
        public static Vector3 operator *(Vector3 a, float b) => a;
    }
    public struct Quaternion { public static Quaternion identity => new Quaternion(); }
    public static class Random { public static Vector3 insideUnitSphere => new Vector3(); }
    public sealed class Coroutine { public IEnumerator Routine; }
    public sealed class Transform { public Vector3 position, forward; }
}
public class ItemDrop
{
    public ItemData m_itemData;
    public static List<ItemDrop> Drops = new List<ItemDrop>();
    public static int FailAt, Attempts;
    public static implicit operator bool(ItemDrop obj) => obj != null;
    public static ItemDrop DropItem(ItemData data, int amount, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation)
    {
        if (++Attempts == FailAt) return null;
        var drop = new ItemDrop { m_itemData = data.Clone() };
        EpicLoot.MagicItemEffects.Indestructible.Sync(drop.m_itemData);
        Drops.Add(drop); return drop;
    }
    public class ItemData
    {
        public class SharedData { public int m_maxQuality = 4, m_maxStackSize = 1; public bool m_useDurability = true, MagicAllowed = true; public string m_name = "Sword"; }
        public SharedData m_shared = new SharedData();
        public Dictionary<string, string> m_customData = new Dictionary<string, string>();
        public UnityEngine.GameObject m_dropPrefab;
        public int m_quality = 1, m_stack = 1, m_variant;
        public long m_crafterID;
        public string m_crafterName = "";
        public float m_durability = 40;
        public MagicItem Magic;
        public bool IsStone;
        public MagicItemEffect StoneEffect;
        public static bool CloneSync;
        public ItemData Clone()
        {
            var copy = (ItemData)MemberwiseClone(); copy.m_customData = new Dictionary<string, string>(m_customData);
            copy.Magic = Magic == null ? null : JsonConvert.DeserializeObject<MagicItem>(JsonConvert.SerializeObject(Magic));
            if (CloneSync && copy.Magic != null) EpicLoot.MagicItemEffects.Indestructible.Sync(copy);
            return copy;
        }
        public float GetMaxDurability() => m_quality * 100;
    }
}
public class Inventory
{
    public List<ItemDrop.ItemData> Items = new List<ItemDrop.ItemData>();
    public int Capacity = 100, FailAt, Attempts;
    public bool AddItem(ItemDrop.ItemData item)
    {
        if (++Attempts == FailAt) throw new InvalidOperationException("test delivery failure");
        if (Items.Count >= Capacity) return false;
        EpicLoot.MagicItemEffects.Indestructible.Sync(item); Items.Add(item); return true;
    }
    public bool AddItem(ItemDrop.ItemData item, int count, int x, int y, bool check) => AddItem(item);
}
public class Player
{
    public static Player m_localPlayer = new Player();
    public Inventory Inventory = new Inventory();
    public UnityEngine.Transform transform = new UnityEngine.Transform();
    public Inventory GetInventory() => Inventory;
    public static implicit operator bool(Player player) => player != null;
}
public sealed class ZNetScene
{
    public static ZNetScene instance = new ZNetScene();
    public static implicit operator bool(ZNetScene scene) => scene != null;
}
public sealed class ObjectDB
{
    public static ObjectDB instance = new ObjectDB();
    public Dictionary<string, UnityEngine.GameObject> Items = new Dictionary<string, UnityEngine.GameObject>();
    public UnityEngine.GameObject GetItemPrefab(string name) => name != null && Items.TryGetValue(name, out var item) ? item : null;
}
public sealed class Localization
{
    public static Localization instance = new Localization();
    public string Localize(string text) => text;
}
namespace EpicLoot
{
    public enum ItemRarity { Magic, Rare, Epic, Legendary, Mythic, Ancient }
    public class MagicItemEffect
    {
        public string EffectType; public float EffectValue;
        public MagicItemEffect() { }
        public MagicItemEffect(string type, float value = 1) { EffectType = type; EffectValue = value; }
    }
    public class SocketedEffect { public string SourcePrefab; public MagicItemEffect Effect; }
    public partial class MagicItem
    {
        public ItemRarity Rarity;
        public string DisplayName;
        public string LegendaryID, SetID;
        public int SocketCount;
        public List<MagicItemEffect> Effects = new List<MagicItemEffect>();
        public List<SocketedEffect> Sockets = new List<SocketedEffect>();
    }
    public class MagicItemEffectDefinition
    {
        public string Type, RequiredPrefab, ConflictsWith;
        public bool NoRoll;
        public bool CanBeAugmented = true;
        public class ValueDef { public float MinValue = 1, MaxValue = 30; }
        public ValueDef Values;
        public ValueDef GetValuesForRarity(ItemRarity rarity) => Values;
    }
    public static class MagicItemEffectDefinitions
    {
        public static Dictionary<string, MagicItemEffectDefinition> AllDefinitions = new Dictionary<string, MagicItemEffectDefinition>();
        public static List<MagicItemEffectDefinition> GetAvailableEffects(ItemDrop.ItemData item, MagicItem magic)
            => AllDefinitions.Values.Where(x => !x.NoRoll && (x.RequiredPrefab == null || item.m_dropPrefab.name == x.RequiredPrefab) && magic.Effects.All(e => e.EffectType != x.Type && e.EffectType != x.ConflictsWith)).ToList();
        public static MagicItemEffectDefinition.ValueDef GetRollRange(MagicItemEffectDefinition definition, ItemRarity rarity, string id = null)
            => LegendarySystem.UniqueLegendaryHelper.GetLegendaryEffectValues(id, definition?.Type) ?? definition?.GetValuesForRarity(rarity);
    }
    public static class MagicEffectType { public const string Indestructible = "Indestructible", IncreaseTreeDrop = "IncreaseTreeDrop"; }
    public static class LootRoller
    {
        public static IEnumerable<KeyValuePair<int,float>> GetEffectCountsPerRarity(ItemRarity rarity, bool enchanting) => new[] { new KeyValuePair<int,float>(3,1) };
        public static int GetMaxSocketCountForRarity(ItemRarity rarity) => 2;
    }
    public static class API
    {
        public static int FailWrites;
        public static bool CanBeMagicItem(ItemDrop.ItemData data) => data.m_shared.MagicAllowed;
        public static bool ApplyMagicItemJson(ItemDrop.ItemData item, string json)
        {
            if (FailWrites > 0) { FailWrites--; return false; }
            item.Magic = JsonConvert.DeserializeObject<MagicItem>(json);
            item.m_customData["magic"] = json;
            MagicItemEffects.Indestructible.Sync(item); return true;
        }
        public static string GetMagicItemJson(ItemDrop.ItemData item) => item.Magic == null ? null : JsonConvert.SerializeObject(item.Magic);
    }
    public static class Extensions
    {
        public static MagicItem GetMagicItem(this ItemDrop.ItemData item) => item.Magic;
        public static bool HasMagicEffect(this ItemDrop.ItemData item, string type) => item.Magic?.GetEffects(type, true).Count > 0;
    }
}
namespace EpicLoot.ShardStones
{
    public static class ShardSocketManager
    {
        public static bool CanSocket(ItemDrop.ItemData item, ItemDrop.ItemData stone, out string reason)
        {
            reason = "test stone rejected";
            return stone.IsStone && item.Magic.Sockets.Count < item.Magic.SocketCount && item.Magic.Sockets.All(x => x.SourcePrefab != stone.m_dropPrefab.name);
        }
        public static bool AddShard(ItemDrop.ItemData item, ItemDrop.ItemData stone)
        {
            if (!CanSocket(item, stone, out _)) return false;
            item.Magic.Sockets.Add(new SocketedEffect { SourcePrefab = stone.m_dropPrefab.name, Effect = stone.StoneEffect });
            EpicLoot.MagicItemEffects.Indestructible.Sync(item); return true;
        }
    }
}
namespace HarmonyLib
{
    public sealed class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string name, params Type[] arguments) { } }
    public static class AccessTools { public static MethodInfo DeclaredMethod(Type type, string name) => type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic); }
}
namespace JetBrains.Annotations { public sealed class UsedImplicitlyAttribute : Attribute { } }
namespace EpicLootSpawner
{
    internal static class ExactWoodDrops { public const string Marker = "local.epicloot.spawner.exact-effects"; }
    public sealed partial class Plugin
    {
        public const string Id = "local.epicloot.spawner";
        private readonly Spawner builder = new Spawner();
        private bool toInventory = true;
        private string status;
        private string lastError, lastOperation;
        private void RecordError(Exception error) => lastError = error.Message;
        private sealed class Log { public void LogWarning(object value) { } }
        private readonly Log Logger = new Log();
        private UnityEngine.Coroutine StartCoroutine(IEnumerator routine) => new UnityEngine.Coroutine { Routine = routine };
        internal void Configure(Recipe recipe, bool inventory) { builder.Import(recipe); toInventory = inventory; }
        internal void Start() => BeginSpawn();
        internal IEnumerator Routine => production?.Routine;
        internal void Stop() => stopProduction = true;
        internal int Produced => produced;
        internal string Status => status;
        internal string LastError => lastError;
        internal string LastOperation => lastOperation;
    }
}
