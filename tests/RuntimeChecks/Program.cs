using System;
using System.Collections;
using System.Linq;
using EpicLoot;
using EpicLoot.MagicItemEffects;
using EpicLootSpawner;

public static partial class Program
{
    private static int checks;
    private static void Check(bool condition, string label) { if (!condition) throw new Exception(label); checks++; }
    private static void Reject(Action action, string label) { bool threw = false; try { action(); } catch { threw = true; } Check(threw, label); }
    private static Recipe Recipe(bool indestructible = false, int count = 1, bool socket = false)
    {
        var recipe = new Recipe { Prefab = "Sword", Count = count.ToString(), SocketCount = socket ? "1" : "0" };
        if (indestructible && !socket) recipe.Effects.Add(new EffectChoice { Type = MagicEffectType.Indestructible, Value = "1" });
        if (socket) recipe.Stones.Add("Stone");
        return recipe;
    }
    private static void Drain(IEnumerator routine) { int steps = 0; while (routine.MoveNext()) if (++steps > 1000) throw new Exception("coroutine did not finish"); }
    private static void Reset()
    {
        Player.m_localPlayer = new Player(); ZNetScene.instance = new ZNetScene();
        ItemDrop.Drops.Clear(); ItemDrop.Attempts = ItemDrop.FailAt = 0; API.FailWrites = 0;
    }
    public static void Main()
    {
        var sword = new UnityEngine.GameObject { name = "Sword", Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
        sword.Drop.m_itemData.m_dropPrefab = sword;
        sword.Drop.m_itemData.m_crafterID = 123456;
        sword.Drop.m_itemData.m_crafterName = "Inherited prefab crafter";
        ObjectDB.instance.Items["Sword"] = sword;
        var stone = new UnityEngine.GameObject { name = "Stone", Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData { IsStone = true, StoneEffect = new MagicItemEffect(MagicEffectType.Indestructible) } } };
        ObjectDB.instance.Items["Stone"] = stone;
        ObjectDB.instance.Items["NotStone"] = new UnityEngine.GameObject { name = "NotStone", Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
        MagicItemEffectDefinitions.AllDefinitions[MagicEffectType.Indestructible] = new MagicItemEffectDefinition { Type = MagicEffectType.Indestructible };
        MagicItemEffectDefinitions.AllDefinitions["Damage"] = new MagicItemEffectDefinition { Type = "Damage", Values = new MagicItemEffectDefinition.ValueDef() };
        MagicItemEffectDefinitions.AllDefinitions["Incompatible"] = new MagicItemEffectDefinition { Type = "Incompatible", RequiredPrefab = "Other" };
        MagicItemEffectDefinitions.AllDefinitions[MagicEffectType.IncreaseTreeDrop] = new MagicItemEffectDefinition { Type = MagicEffectType.IncreaseTreeDrop, Values = new MagicItemEffectDefinition.ValueDef() };

        var builder = new Spawner(); builder.Import(Recipe());
        Check(builder.Build().m_customData.ContainsKey(ExactWoodDrops.Marker), "build adds marker");
        Check(sword.Drop.m_itemData.Magic == null && sword.Drop.m_itemData.m_shared.m_useDurability, "build preserves prefab");
        Check(builder.BaseItem().m_crafterID == 0 && builder.BaseItem().m_crafterName == "", "base preview has native loot crafter metadata");
        Check(builder.Build().m_crafterID == 0 && builder.Build().m_crafterName == "", "completed magic item does not inherit a crafter");
        Check(sword.Drop.m_itemData.m_crafterID == 123456 && sword.Drop.m_itemData.m_crafterName == "Inherited prefab crafter", "clearing loot crafter leaves prefab template untouched");
        Reject(() => builder.Import(new Recipe { Prefab = "Missing" }), "missing prefab import rejected");
        Check(builder.Prefab == sword && builder.Count == "1", "failed import keeps design");
        var highQuality = Recipe(); highQuality.Quality = "30";
        builder.Import(highQuality);
        Check(builder.BaseItem().m_quality == 30 && builder.Build().m_quality == 30, "quality above prefab maximum is retained in real builder");
        Check(!builder.FieldErrors().ContainsKey("Quality") && builder.Export().Quality == "30", "quality above maximum can be previewed and exported");
        Check(sword.Drop.m_itemData.m_shared.m_maxQuality == 4 && sword.Drop.m_itemData.m_quality == 1, "custom quality preserves normal prefab bounds and template");
        foreach (string invalidQuality in new[] { "0", "-1", "1.5", "2147483648", "", "NaN" })
        {
            var invalid = Recipe(); invalid.Quality = invalidQuality;
            Reject(() => builder.Import(invalid), "invalid quality rejected: " + invalidQuality);
            Check(builder.Quality == "30", "invalid quality import preserves current design");
            builder.Quality = invalidQuality;
            Check(builder.FieldErrors().ContainsKey("Quality"), "invalid quality gets field error");
            Reject(() => builder.Build(), "direct invalid quality build rejected");
            builder.Quality = "30";
        }
        foreach (bool inventory in new[] { true, false })
        {
            Reset();
            var custom = Recipe(false, 1, true); custom.Quality = "30";
            custom.Effects.Add(new EffectChoice { Type = "Damage", Value = "15" });
            var customPlugin = new Plugin(); customPlugin.Configure(custom, inventory); customPlugin.Start(); Drain(customPlugin.Routine);
            var delivered = inventory ? Player.m_localPlayer.Inventory.Items.Single() : ItemDrop.Drops.Single().m_itemData;
            Check(delivered.m_quality == 30 && delivered.Magic.SocketCount == 1 && delivered.Magic.Sockets.Count == 1, "delivery keeps high quality and sockets");
            Check(delivered.Magic.Effects.Single().EffectValue == 15 && sword.Drop.m_itemData.m_shared.m_maxQuality == 4, "high quality keeps effects without changing normal maximum");
            Check(delivered.m_crafterID == 0 && delivered.m_crafterName == "", "inventory and ground production preserve native loot crafter metadata with quality effects and stones");
        }
        builder.Import(Recipe());
        var bad = Recipe();
        bad = Recipe(); bad.Count = "101"; Reject(() => builder.Import(bad), "count limit");
        bad = Recipe(); bad.SocketCount = "3"; Reject(() => builder.Import(bad), "socket limit");
        bad = Recipe(); bad.Name = new string('a', 81); Reject(() => builder.Import(bad), "name limit");
        bad = Recipe(); bad.Effects.Add(new EffectChoice { Type = "Incompatible" }); Reject(() => builder.Import(bad), "item effect compatibility");
        bad = Recipe(); bad.Effects.Add(new EffectChoice { Type = "Damage", Value = "31" }); Reject(() => builder.Import(bad), "effect upper limit");
        bad.Effects[0].Value = "0"; Reject(() => builder.Import(bad), "effect lower limit");
        bad.Effects[0].Value = "NaN"; Reject(() => builder.Import(bad), "nonfinite effect rejected");
        bad = Recipe(true); bad.Effects[0].Value = "2"; Reject(() => builder.Import(bad), "binary value rejected");
        bad = Recipe(); bad.Effects.Add(new EffectChoice { Type = "Damage" }); bad.Effects.Add(new EffectChoice { Type = "Damage" }); Reject(() => builder.Import(bad), "duplicate effect rejected");
        bad.Effects.Add(new EffectChoice { Type = "Damage" }); bad.Effects.Add(new EffectChoice { Type = "Damage" }); Reject(() => builder.Import(bad), "effect count bound");
        bad = Recipe(); bad.Effects.Add(new EffectChoice { Type = MagicEffectType.IncreaseTreeDrop, Value = "1.5" }); Reject(() => builder.Import(bad), "lumberjack integral input");
        bad = Recipe(false, 1, true); bad.SocketCount = "0"; Reject(() => builder.Import(bad), "stone capacity");
        bad.SocketCount = "1"; bad.Stones[0] = "Missing"; Reject(() => builder.Import(bad), "missing stone");
        bad.Stones[0] = "NotStone"; Reject(() => builder.Import(bad), "CanSocket rejection");
        API.FailWrites = 1; Reject(() => builder.Import(Recipe()), "magic API failure rejected");
        Check(builder.Prefab == sword, "API failure leaves current builder intact");
        sword.Drop.m_itemData.m_shared.MagicAllowed = false; Reject(() => builder.Build(), "CanBeMagicItem final validation");
        sword.Drop.m_itemData.m_shared.MagicAllowed = true;

        foreach (bool cloneSync in new[] { false, true })
        foreach (bool inventory in new[] { false, true })
        foreach (bool socketed in new[] { false, true })
        {
            Reset(); ItemDrop.ItemData.CloneSync = cloneSync;
            var plugin = new Plugin(); plugin.Configure(Recipe(true, 1, socketed), inventory); plugin.Start();
            var routine = plugin.Routine;
            Check(routine != null && Player.m_localPlayer.Inventory.Items.Count == 0 && ItemDrop.Drops.Count == 0, "delivery deferred until handle installed");
            Drain(routine);
            var result = inventory ? Player.m_localPlayer.Inventory.Items.Single() : ItemDrop.Drops.Single().m_itemData;
            Check(plugin.Produced == 1 && plugin.Routine == null, "one item completed and handle cleared");
            Check(!result.m_shared.m_useDurability && Indestructible.OriginallyUsesDurability(result), "production keeps true baseline for rolled/socketed effects");
            Check(result.m_customData.ContainsKey(ExactWoodDrops.Marker), "delivery retains marker");
            API.ApplyMagicItemJson(result, "{\"Effects\":[],\"Sockets\":[]}");
            Check(result.m_shared.m_useDurability, "removal restores durability in same session");
        }
        Reset(); sword.Drop.m_itemData.m_shared.m_useDurability = false;
        var naturallyNondurable = new Plugin(); naturallyNondurable.Configure(Recipe(true), true); naturallyNondurable.Start(); Drain(naturallyNondurable.Routine);
        var noDurability = Player.m_localPlayer.Inventory.Items.Single();
        API.ApplyMagicItemJson(noDurability, "{\"Effects\":[]}");
        Check(!noDurability.m_shared.m_useDurability && !Indestructible.OriginallyUsesDurability(noDurability), "naturally nondurable remains nondurable");
        sword.Drop.m_itemData.m_shared.m_useDurability = true;

        Reset(); var bulk = new Plugin(); bulk.Configure(Recipe(false, 100), true); bulk.Start(); Drain(bulk.Routine);
        Check(bulk.Produced == 100 && Player.m_localPlayer.Inventory.Items.Count == 100 && bulk.LastOperation == bulk.Status, "100 item production and completion diagnostics");
        Check(Player.m_localPlayer.Inventory.Items.Distinct().Count() == 100, "independent ItemData objects");
        Check(Player.m_localPlayer.Inventory.Items.All(x => x.m_crafterID == 0 && x.m_crafterName == ""), "every bulk-produced item has no crafter");
        Reset(); Player.m_localPlayer.Inventory.Capacity = 3;
        bulk = new Plugin(); bulk.Configure(Recipe(false, 100), true); bulk.Start(); Drain(bulk.Routine);
        Check(bulk.Produced == 3 && bulk.Status.Contains("yer kalmadı") && ItemDrop.Drops.Count == 0, "full inventory stops without fallback ground delivery");
        Reset(); bulk = new Plugin(); bulk.Configure(Recipe(false, 100), true); bulk.Start();
        var iterator = bulk.Routine; iterator.MoveNext(); iterator.MoveNext();
        Check(bulk.Produced == 8, "bounded batch per frame");
        bulk.Stop(); Drain(iterator);
        Check(bulk.Produced == 8 && Player.m_localPlayer.Inventory.Items.Count == 8 && bulk.Routine == null, "stop halts remaining work");
        Reset(); Player.m_localPlayer.Inventory.FailAt = 3;
        bulk = new Plugin(); bulk.Configure(Recipe(false, 100), true); bulk.Start(); Drain(bulk.Routine);
        Check(bulk.Produced == 2 && Player.m_localPlayer.Inventory.Items.Count == 2 && bulk.Status.Contains("test delivery") && bulk.LastError.Contains("test delivery") && bulk.LastOperation == bulk.Status, "partial delivery failure no retry and error diagnostics");
        Reset(); ItemDrop.FailAt = 3;
        bulk = new Plugin(); bulk.Configure(Recipe(false, 100), false); bulk.Start(); Drain(bulk.Routine);
        Check(bulk.Produced == 2 && ItemDrop.Drops.Count == 2 && bulk.Routine == null, "ground failure ends batch");
        Reset(); bulk = new Plugin(); bulk.Configure(Recipe(false, 100), true); bulk.Start(); iterator = bulk.Routine; iterator.MoveNext();
        Reject(bulk.Start, "parallel production rejected");
        Player.m_localPlayer = null; Drain(iterator);
        Check(bulk.Produced == 0 && bulk.Routine == null, "world departure ends coroutine");
        Reset();
        CheckSpecialItems();
        Reset();
        CheckUnlimited();
        Console.WriteLine("PASS: " + checks + " linked Spawner/Production checks (Unity/API delivery stubs)");
    }
}
