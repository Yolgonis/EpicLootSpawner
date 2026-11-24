using System;
using System.Linq;
using EpicLoot;
using EpicLoot.LegendarySystem;
using EpicLootSpawner;
using Newtonsoft.Json;

public static partial class Program
{
    private static void CheckSpecialItems()
    {
        UniqueLegendaryHelper.Clear();
        MagicItemEffectDefinitions.AllDefinitions["UniquePower"] = new MagicItemEffectDefinition { Type = "UniquePower", NoRoll = true, CanBeAugmented = false };
        MagicItemEffectDefinitions.AllDefinitions["Conflict"] = new MagicItemEffectDefinition { Type = "Conflict", ConflictsWith = "UniquePower" };
        for (int i = 0; i < 5; i++) MagicItemEffectDefinitions.AllDefinitions["Optional" + i] = new MagicItemEffectDefinition { Type = "Optional" + i };
        var special = new LegendaryInfo { ID = "WoodStaff", Name = "$special_name", IsSetItem = true, GuaranteedEffectCount = 5, Requirements = new MagicItemEffectRequirements { RequiredPrefab = "Sword" } };
        special.GuaranteedMagicEffects.Add(new GuaranteedMagicEffect { Type = "Damage", Values = new MagicItemEffectDefinition.ValueDef { MinValue = 50, MaxValue = 60 } });
        special.GuaranteedMagicEffects.Add(new GuaranteedMagicEffect { Type = "UniquePower" });
        var set = new LegendarySetInfo { ID = "WoodSet", Name = "$set_name" }; set.LegendaryIDs.Add(special.ID);
        UniqueLegendaryHelper.LegendaryInfo[special.ID] = special;
        UniqueLegendaryHelper.LegendarySets[set.ID] = set;
        UniqueLegendaryHelper.LegendaryItemsToSetMap[special.ID] = set;
        var mythic = new LegendaryInfo { ID = "MythicSword", Name = "Mythic sword" };
        UniqueLegendaryHelper.MythicInfo[mythic.ID] = mythic;
        var builder = new Spawner(); builder.Import(Recipe(false, 2, true));
        builder.Name = "Old name"; builder.Quality = "30";
        builder.SelectSpecial(special.ID);
        Check(builder.LegendaryID == special.ID && builder.SetID == set.ID && builder.Rarity == ItemRarity.Legendary, "identity selection stamps native rarity and set mapping");
        Check(builder.Name == "" && builder.Quality == "30" && builder.Count == "2" && builder.SocketCount == "0" && builder.Stones.Count == 0, "identity selection retains quality quantity and clears previous adornments");
        Check(builder.Effects.Count == 2 && builder.Effects[0].Value == "50" && builder.Effects[1].Value == "1", "guaranteed powers seed their own lower bounds including non-rollable unique power");
        Check(builder.Build().Magic.DisplayName == "$special_name", "blank special name uses native localized token");
        Check(builder.EffectLimit == 5 && builder.IsRequired("UniquePower") && !builder.IsRequired("Optional0"), "special count and guaranteed flags follow definition");
        Check(builder.FieldErrors().Count == 0 && builder.Available().All(x => x.Type != "UniquePower" && x.Type != "Conflict"), "guaranteed powers validate without entering ordinary optional catalog");
        builder.Effects[0].Value = "60";
        Check(builder.Build().Magic.Effects[0].EffectValue == 60, "special range may exceed regular rarity range");
        foreach (string invalid in new[] { "49", "61", "NaN" })
        {
            builder.Effects[0].Value = invalid;
            Check(builder.FieldErrors().ContainsKey("Effect.0"), "special range reports inline error");
            Reject(() => builder.Build(), "special range enforces own bound");
        }
        builder.Effects[0].Value = "60";
        var recipe = builder.Export();
        var copy = JsonConvert.DeserializeObject<Recipe>(JsonConvert.SerializeObject(recipe));
        var restored = new Spawner(); restored.Import(copy);
        Check(restored.Build().Magic.LegendaryID == special.ID && restored.Build().Magic.SetID == set.ID, "serialized recipe imports full special identity");
        restored.RestoreDraft(copy);
        Check(restored.LegendaryID == special.ID && restored.SetID == set.ID, "history restores identity fields");
        var bad = JsonConvert.DeserializeObject<Recipe>(JsonConvert.SerializeObject(recipe)); bad.LegendaryID = "Missing";
        Reject(() => restored.Import(bad), "unknown identity rejects import atomically");
        Check(restored.LegendaryID == special.ID, "failed special import retains previous identity");
        bad = JsonConvert.DeserializeObject<Recipe>(JsonConvert.SerializeObject(recipe)); bad.SetID = "Wrong";
        Reject(() => restored.Import(bad), "forged set ID rejected");
        bad.SetID = null; Reject(() => restored.Import(bad), "missing membership rejected");
        bad = JsonConvert.DeserializeObject<Recipe>(JsonConvert.SerializeObject(recipe)); bad.Rarity = (int)ItemRarity.Epic;
        Reject(() => restored.Import(bad), "ordinary rarity cannot retain unique identity");
        bad = JsonConvert.DeserializeObject<Recipe>(JsonConvert.SerializeObject(recipe)); bad.Effects.RemoveAt(1);
        Reject(() => restored.Import(bad), "required unique power cannot be omitted");
        bad = JsonConvert.DeserializeObject<Recipe>(JsonConvert.SerializeObject(recipe)); bad.Effects.RemoveAt(0); bad.Effects.Add(new EffectChoice { Type = "Optional0", Value = "1" });
        restored.Import(bad);
        Check(restored.Build().Magic.LegendaryID == special.ID && restored.IsProtected("UniquePower") && !restored.IsProtected("Damage"), "legitimately replaced augmentable starting power remains editable with identity intact");
        restored.Import(recipe);
        bad = JsonConvert.DeserializeObject<Recipe>(JsonConvert.SerializeObject(recipe)); bad.Effects.Add(new EffectChoice { Type = "UniquePower", Value = "1" });
        Reject(() => restored.Import(bad), "required power cannot be duplicated");
        bad = JsonConvert.DeserializeObject<Recipe>(JsonConvert.SerializeObject(recipe)); bad.Effects.Insert(0, new EffectChoice { Type = "Conflict", Value = "1" });
        Reject(() => restored.Import(bad), "optional power conflicts checked against later guaranteed powers");
        for (int i = 0; i < 3; i++) restored.AddEffect(MagicItemEffectDefinitions.AllDefinitions["Optional" + i]);
        Check(restored.Build().Magic.Effects.Count == 5 && restored.Available().Count == 0, "special count allows up to configured limit");
        restored.Effects.Add(new EffectChoice { Type = "Optional4", Value = "1" });
        Reject(() => restored.Build(), "special limit cannot be exceeded");
        restored.Import(recipe);
        restored.SocketCount = "2"; restored.Stones.Add("Stone");
        var socketRecipe = restored.Export();
        foreach (bool inventory in new[] { true, false })
        {
            Reset();
            var plugin = new Plugin(); plugin.Configure(socketRecipe, inventory); plugin.Start(); Drain(plugin.Routine);
            var delivered = inventory ? Player.m_localPlayer.Inventory.Items.First() : ItemDrop.Drops.First().m_itemData;
            Check(delivered.Magic.LegendaryID == special.ID && delivered.Magic.SetID == set.ID && delivered.Magic.Rarity == ItemRarity.Legendary, "inventory and ground delivery preserve unique identity");
            Check(delivered.Magic.Sockets.Count == 1 && delivered.Magic.Effects[0].EffectValue == 60 && delivered.m_quality == 30, "special delivery keeps powers quality and socket");
            Check(delivered.m_crafterID == 0 && delivered.m_crafterName == "", "special set production has native loot crafter metadata for inventory and ground delivery");
        }
        Reset();
        var other = new UnityEngine.GameObject { name = "Other", Drop = new ItemDrop { m_itemData = new ItemDrop.ItemData() } };
        ObjectDB.instance.Items[other.name] = other;
        var wrong = new Spawner { Prefab = other };
        Reject(() => wrong.SelectSpecial(special.ID), "unique identity cannot be assigned to incompatible base item");
        Check(wrong.LegendaryID == null, "failed identity selection does not alter design");
        Check(!SpecialItems.Available(wrong.BaseItem()).Any(x => x.ID == special.ID), "catalog excludes incompatible special definition");
        Check(SpecialItems.Available(restored.BaseItem()).Any(x => x.ID == mythic.ID), "catalog includes native mythic definitions");
        restored.SelectSpecial(mythic.ID);
        Check(restored.Rarity == ItemRarity.Mythic && restored.Build().Magic.SetID == null, "standalone mythic uses mythic rarity without set");
        restored.SelectSpecial(null);
        Check(!restored.IsSpecial && restored.Build().Magic.LegendaryID == null && restored.Effects.Count == 0, "ordinary identity clears special powers and metadata");
        restored.RestoreDraft(new Recipe { Prefab = "Sword", LegendaryID = "RemovedConfig", Rarity = (int)ItemRarity.Legendary });
        Check(restored.EffectLimit == 3 && !restored.IsRequired("Damage"), "missing config remains displayable for repair");
        Reject(() => restored.Build(), "missing config never silently produces ordinary item");
        restored.Import(recipe);
        string intact = JsonConvert.SerializeObject(restored.Draft());
        var malformedDraft = restored.Draft(); malformedDraft.Name = "Wrong"; malformedDraft.Effects.Add(null);
        Reject(() => restored.RestoreDraft(malformedDraft), "null effect draft rejected before mutations");
        Check(JsonConvert.SerializeObject(restored.Draft()) == intact, "null effect draft preserves complete prior design");
        malformedDraft = restored.Draft(); malformedDraft.Stones.Add(null);
        Reject(() => restored.RestoreDraft(malformedDraft), "null stone draft rejected before mutations");
        Check(JsonConvert.SerializeObject(restored.Draft()) == intact, "null stone draft keeps design intact");
        Reject(() => restored.RestoreDraft(null), "null draft produces validation error");
        malformedDraft = restored.Draft(); malformedDraft.Quality = "";
        restored.RestoreDraft(malformedDraft);
        Check(restored.Quality == "", "draft restoration continues preserving unfinished numeric input");
        restored.Import(recipe);
        var values = special.GuaranteedMagicEffects[0].Values;
        foreach (var range in new[] { (float.NaN, 60f), (50f, float.PositiveInfinity), (60f, 50f) })
        {
            values.MinValue = range.Item1; values.MaxValue = range.Item2;
            Check(restored.FieldErrors().ContainsKey("Effect.0"), "invalid special range reports inline error");
            Reject(() => restored.Build(), "invalid special range blocks production");
            Reject(() => restored.SelectSpecial(special.ID), "invalid range selection does not replace active design");
            Check(JsonConvert.SerializeObject(restored.Draft()) == intact, "invalid special range leaves design intact");
        }
        values.MinValue = 50; values.MaxValue = 60;
        Check(restored.Build().Magic.Effects[0].EffectValue == 60, "repaired native range restores production");
        var uniqueDefinition = MagicItemEffectDefinitions.AllDefinitions["UniquePower"];
        MagicItemEffectDefinitions.AllDefinitions.Remove("UniquePower");
        var noPowers = restored.Draft(); noPowers.Effects.Clear();
        Reject(() => restored.Import(noPowers), "missing fixed power definition cannot bypass protection with empty effects");
        Reject(() => restored.Build(), "missing required native definition blocks existing design");
        Check(!SpecialItems.Available(restored.BaseItem()).Any(x => x.ID == special.ID), "invalid special definition excluded from catalog");
        MagicItemEffectDefinitions.AllDefinitions["UniquePower"] = uniqueDefinition;
        special.GuaranteedMagicEffects.Add(null);
        Reject(() => SpecialItems.Check(restored.BaseItem(), restored.MagicShell()), "null guaranteed power rejected clearly");
        Check(restored.FieldErrors().ContainsKey("Identity"), "malformed required power reports field error before native range lookup");
        Check(!SpecialItems.Available(restored.BaseItem()).Any(x => x.ID == special.ID), "null guaranteed power cannot crash special catalog");
        special.GuaranteedMagicEffects.RemoveAt(special.GuaranteedMagicEffects.Count - 1);
        special.GuaranteedMagicEffects.Add(new GuaranteedMagicEffect { Type = "UniquePower" });
        Reject(() => SpecialItems.Check(restored.BaseItem(), restored.MagicShell()), "duplicate guaranteed power rejected");
        special.GuaranteedMagicEffects.RemoveAt(special.GuaranteedMagicEffects.Count - 1);
        UniqueLegendaryHelper.LegendaryInfo["NullRecord"] = null;
        UniqueLegendaryHelper.LegendaryInfo["WrongKey"] = new LegendaryInfo { ID = "DifferentId" };
        Reject(() => SpecialItems.Resolve("NullRecord"), "null native identity record rejected");
        Reject(() => SpecialItems.Resolve("WrongKey"), "registry key and identity mismatch rejected");
        Check(SpecialItems.Available(restored.BaseItem()).Any(x => x.ID == special.ID), "bad unrelated identity records do not hide valid special items");
        var savedPowers = special.GuaranteedMagicEffects; special.GuaranteedMagicEffects = null;
        Check(restored.FieldErrors().ContainsKey("Identity"), "null guaranteed power list produces repairable identity error");
        Reject(() => restored.Build(), "null guaranteed power list blocks native production safely");
        special.GuaranteedMagicEffects = savedPowers;
        Check(restored.Build().Magic.SetID == set.ID, "repaired special configuration recovers without design loss");
        UniqueLegendaryHelper.Clear();
    }
}
