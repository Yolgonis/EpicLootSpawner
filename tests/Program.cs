using System;
using System.IO;
using EpicLootSpawner;

int checks = 0;
void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed"); checks++; }
void Reject(Action action) { try { action(); } catch (InvalidOperationException) { checks++; return; } throw new Exception("Invalid input accepted"); }
Assert(Rules.IconIndex(0, 0) == -1);
Assert(Rules.IconIndex(1, 30) == 0);
Assert(Rules.IconIndex(2, -1) == 0);
Assert(Rules.IconIndex(3, 2) == 2);
Assert(Rules.IconIndex(3, 3) == 0);
Assert(Rules.IconIndex(1, int.MaxValue) == 0);
Assert(Rules.SliderValue(-5, 0, 10, 1) == 0);
Assert(Rules.SliderValue(20, 0, 10, 1) == 10);
Assert(Rules.SliderValue(2.6f, 0, 10, 1) == 3);
Assert(Rules.SliderValue(2.5f, 0, 10, 1) == 3);
Assert(Rules.SliderValue(8, 5, 15, 2) == 9);
Assert(Rules.SliderValue(15, 5, 15, 3) == 15);
Assert(Rules.SliderValue(4, 4, 4, 1) == 4);
Assert(Math.Abs(Rules.SliderValue(0.26f, 0, 1, 0.1f) - 0.3f) < 0.00001f);
Assert(Rules.SliderValue(2.6f, 0, 10, 0) == 2.6f);
Assert(Rules.Bounded("4", 1, 4, "Seviye") == 4);
Reject(() => Rules.Bounded("5", 1, 4, "Seviye"));
Assert(Rules.Bounded("100", 1, 100, "Adet") == 100);
Reject(() => Rules.Bounded("101", 1, 100, "Adet"));
Assert(Rules.Bounded("6", 0, 6, "Soket") == 6);
Reject(() => Rules.Bounded("7", 0, 6, "Soket"));
Assert(Rules.EffectValue("5", 5, 15) == 5);
Assert(Rules.EffectValue("15", 5, 15) == 15);
Reject(() => Rules.EffectValue("4", 5, 15));
Reject(() => Rules.EffectValue("16", 5, 15));
Assert(Rules.EffectValue("1", 1, 1) == 1);
Reject(() => Rules.EffectValue("2", 1, 1));
Assert(Rules.Whole("100000", 1, "Seviye") == 100000);
Assert(Rules.Whole("2147483647", 1, "Adet") == int.MaxValue);
Assert(Rules.Whole("100000", 0, "Soket") == 100000);
Assert(Rules.Whole("0", 0, "Soket") == 0);
Reject(() => Rules.Whole("0", 1, "Adet"));
Reject(() => Rules.Whole("-1", 0, "Soket"));
Reject(() => Rules.Whole("1.5", 1, "Seviye"));
Reject(() => Rules.Whole("2147483648", 1, "Seviye"));
Assert(Rules.CustomEffectValue("5000", true) == 5000);
Assert(Rules.CustomEffectValue("5000", false) == 5000);
Assert(Rules.CustomEffectValue("-500", true) == -500);
Assert(Rules.CustomEffectValue("25,5", true) == 25.5f);
Assert(Rules.ExtraWoodCount(30) == 30);
Assert(Rules.ExtraWoodCount(1000) == 1000);
Assert(Rules.ExtraWoodCount(0) == 0);
Assert(Rules.WoodPackSize(30, 50) == 30);
int woodRemaining = 130, woodTotal = 0, woodPacks = 0;
while (woodRemaining > 0) { int pack = Rules.WoodPackSize(woodRemaining, 50); woodRemaining -= pack; woodTotal += pack; woodPacks++; }
Assert(woodTotal == 130 && woodPacks == 3);
Assert(Rules.WoodPackSize(30, 0) == 1);
Reject(() => Rules.ExtraWoodCount(-1));
Reject(() => Rules.ExtraWoodCount(30.5f));
Reject(() => Rules.ExtraWoodCount(float.PositiveInfinity));
Reject(() => Rules.ExtraWoodCount(float.NaN));
Reject(() => Rules.ExtraWoodCount(float.MaxValue));
Assert(Rules.TryRuntimeWoodCount(30, out int runtimeWood) && runtimeWood == 30);
Assert(Rules.TryRuntimeWoodCount(31.5f, out runtimeWood) && runtimeWood == 31);
Assert(Rules.TryRuntimeWoodCount(0.5f, out runtimeWood) && runtimeWood == 0);
Assert(Rules.TryRuntimeWoodCount(0, out runtimeWood) && runtimeWood == 0);
Assert(!Rules.TryRuntimeWoodCount(-1, out runtimeWood) && runtimeWood == 0);
Assert(!Rules.TryRuntimeWoodCount(float.NaN, out runtimeWood) && runtimeWood == 0);
Assert(!Rules.TryRuntimeWoodCount(float.PositiveInfinity, out runtimeWood) && runtimeWood == 0);
Assert(!Rules.TryRuntimeWoodCount(float.MaxValue, out runtimeWood) && runtimeWood == 0);
Reject(() => Rules.CustomEffectValue("NaN", true));
Reject(() => Rules.CustomEffectValue("Infinity", true));
Reject(() => Rules.CustomEffectValue(null, true));
Reject(() => Rules.CustomEffectValue("", true));
var valid = new Recipe { Prefab = "SwordIron", Rarity = 2, Quality = "100000", Count = "100000", SocketCount = "0", Name = new string('x', 300000) };
for (int i = 0; i < 100; i++) valid.Effects.Add(new EffectChoice { Type = "AddDamage", Value = "5000" });
for (int i = 0; i < 10; i++) valid.Stones.Add("Red_Epic_ShardStone");
valid.Validate(6); checks++;
var invalid = new Recipe { Prefab = "SwordIron", Rarity = 6 };
Reject(() => invalid.Validate(6));
invalid.Rarity = 2; invalid.Effects = null;
Reject(() => invalid.Validate(6));
string directory = Path.Combine(Path.GetTempPath(), "EpicLootSpawnerTests-" + Guid.NewGuid().ToString("N"));
var beforeItem = new ItemSnapshot { Quality = 2, Rarity = 1, SocketCount = 2 };
var afterItem = new ItemSnapshot { Quality = 2, Rarity = 1, SocketCount = 2, Name = "" };
Assert(!ItemDifference.Between(beforeItem, afterItem).HasChanges);
beforeItem.Rolled["Wood"] = 10; beforeItem.Totals["Wood"] = 11.5f; beforeItem.Stones.Add("Yellow");
afterItem.Rolled["Wood"] = 15; afterItem.Totals["Wood"] = 17.5f; afterItem.Stones.Add("Red");
afterItem.Quality = 3; afterItem.Rarity = 2; afterItem.Name = "Ad"; afterItem.SocketCount = 3;
var difference = ItemDifference.Between(beforeItem, afterItem);
Assert(difference.HasChanges && difference.Quality && difference.Rarity && difference.Name && difference.Sockets);
Assert(difference.Rolled.Count == 1 && difference.Rolled[0].Before == 10 && difference.Rolled[0].After == 15);
Assert(difference.Rolled[0].Kind == ChangeKind.Changed);
Assert(difference.Totals[0].Before == 11.5f && difference.Totals[0].After == 17.5f);
Assert(difference.Stones.Count == 1 && difference.Stones[0].Slot == 1 && difference.Stones[0].Before == "Yellow" && difference.Stones[0].After == "Red");
Assert(beforeItem.Rolled["Wood"] == 10 && beforeItem.Stones[0] == "Yellow");
afterItem.Rolled.Remove("Wood"); afterItem.Rolled["Binary"] = 0;
difference = ItemDifference.Between(beforeItem, afterItem);
Assert(difference.Rolled.Count == 2 && difference.Rolled[0].Type == "Binary" && difference.Rolled[0].Kind == ChangeKind.Added && difference.Rolled[0].After == 0);
Assert(difference.Rolled[1].Kind == ChangeKind.Removed && !difference.Rolled[1].After.HasValue);
var reordered = new ItemSnapshot { Quality = 2, Rarity = 1, SocketCount = 2 };
reordered.Rolled["Wood"] = 10; reordered.Totals["Wood"] = 11.5f; reordered.Stones.Add("Yellow");
Assert(!ItemDifference.Between(beforeItem, reordered).HasChanges);
reordered.Stones.Add("Blue");
Assert(ItemDifference.Between(beforeItem, reordered).Stones[0].Slot == 2);
Assert(ItemDifference.Between(reordered, beforeItem).Stones[0].After == null);
beforeItem.Stones.Add("Blue"); reordered.Stones.Reverse();
Assert(ItemDifference.Between(beforeItem, reordered).Stones.Count == 2);
reordered.Rolled.Clear(); reordered.Totals["Wood"] = 11.5f;
Assert(ItemDifference.Between(beforeItem, reordered).Rolled.Count == 1 && ItemDifference.Between(beforeItem, reordered).Totals.Count == 0);
Assert(ItemDifference.Between(new ItemSnapshot { Rarity = null }, new ItemSnapshot { Rarity = 0 }).Rarity);

var fieldRecipe = new Recipe { Prefab = "Sword", Quality = "4", Count = "100", SocketCount = "1" };
fieldRecipe.Effects.Add(new EffectChoice { Type = "Wood", Value = "30" });
fieldRecipe.Stones.Add("Yellow");
var fieldRules = new System.Collections.Generic.Dictionary<string, EffectRule> { ["Wood"] = new EffectRule { Min = 10, Max = 30, WholeWood = true }, ["Toggle"] = new EffectRule() };
Assert(FieldValidation.Check(fieldRecipe, 2, 2, fieldRules).Count == 0);
fieldRecipe.Quality = "5"; fieldRecipe.Count = ""; fieldRecipe.SocketCount = "0"; fieldRecipe.Name = new string('x', 81); fieldRecipe.Effects[0].Value = "15,5";
var fieldIssues = FieldValidation.Check(fieldRecipe, 2, 2, fieldRules);
Assert(fieldIssues.Count == 4 && !fieldIssues.ContainsKey("Quality") && fieldIssues.ContainsKey("Count") && fieldIssues.ContainsKey("SocketCount") && fieldIssues.ContainsKey("Name") && fieldIssues.ContainsKey("Effect.0"));
Assert(fieldRecipe.Quality == "5" && fieldRecipe.Count == "" && fieldRecipe.Effects[0].Value == "15,5");
fieldRecipe.Quality = "30"; fieldRecipe.Count = "1"; fieldRecipe.SocketCount = "1"; fieldRecipe.Name = new string('x', 80); fieldRecipe.Effects[0].Value = "10";
Assert(FieldValidation.Check(fieldRecipe, 2, 2, fieldRules).Count == 0);
foreach (string invalidQuality in new[] { "0", "-1", "1.5", "2147483648", "", "NaN" })
{
    fieldRecipe.Quality = invalidQuality;
    Assert(FieldValidation.Check(fieldRecipe, 2, 2, fieldRules).ContainsKey("Quality"));
}
fieldRecipe.Quality = "2147483647";
Assert(FieldValidation.Check(fieldRecipe, 2, 2, fieldRules).Count == 0);
fieldRecipe.Quality = "30";
fieldRecipe.Effects[0].Value = "NaN"; Assert(FieldValidation.Check(fieldRecipe, 2, 2, fieldRules).ContainsKey("Effect.0"));
fieldRecipe.Effects[0].Value = "31"; Assert(FieldValidation.Check(fieldRecipe, 2, 2, fieldRules).ContainsKey("Effect.0"));
fieldRecipe.Effects[0].Value = "10"; fieldRecipe.Effects.Add(new EffectChoice { Type = "Toggle", Value = "2" });
Assert(FieldValidation.Check(fieldRecipe, 1, 2, fieldRules).ContainsKey("Effects"));
Assert(FieldValidation.Check(fieldRecipe, 2, 2, fieldRules).ContainsKey("Effect.1"));
fieldRecipe.Effects[1].Value = "1"; Assert(FieldValidation.Check(fieldRecipe, 2, 2, fieldRules).Count == 0);
fieldRecipe.Effects[1].Type = "Unknown"; Assert(FieldValidation.Check(fieldRecipe, 2, 2, fieldRules).ContainsKey("Effect.1"));
try { FieldValidation.At("Quality", () => Rules.Bounded("5", 1, 4, "Seviye")); throw new Exception("Field error not reported"); }
catch (FieldProblem problem) { Assert(problem.Key == "Quality" && problem.Message.Contains("1–4")); }
Assert(FieldValidation.At("Quality", () => Rules.Bounded("4", 1, 4, "Seviye")) == 4);

Assert(UiLayout.NormalizeScale(float.NaN) == 1 && UiLayout.NormalizeScale(float.PositiveInfinity) == 1);
Assert(UiLayout.NormalizeScale(-1) == 0.75f && UiLayout.NormalizeScale(5) == 1.5f);
foreach (var screen in new[] { (640, 480), (1280, 720), (1920, 1080), (2560, 1440), (3840, 2160) })
foreach (float desired in new[] { 0.75f, 1f, 1.5f })
{
    var ui = UiLayout.ForScreen(screen.Item1, screen.Item2, desired);
    Assert(ui.Scale > 0 && ui.Scale <= desired && ui.Width * ui.Scale <= screen.Item1 - 23 && ui.Height * ui.Scale <= screen.Item2 - 23);
    Assert(ui.LibraryWidth > 0 && ui.EditorWidth > ui.LibraryWidth && ui.PreviewWidth > ui.LibraryWidth && Math.Abs(ui.LibraryWidth + ui.EditorWidth + ui.PreviewWidth - (ui.Width - 64)) < 0.001f);
    float x = UiLayout.Position(99999, screen.Item1 / ui.Scale, ui.Width), y = UiLayout.Position(99999, screen.Item2 / ui.Scale, ui.Height);
    Assert(x >= 0 && y >= 0 && (x + ui.Width) * ui.Scale <= screen.Item1 + 0.01f && (y + ui.Height) * ui.Scale <= screen.Item2 + 0.01f);
}
Assert(UiLayout.Position(float.NaN, 100, 50) == 0 && UiLayout.Position(float.PositiveInfinity, 100, 50) == 0);
Assert(UiLayout.Position(-10, 100, 50) == 0 && UiLayout.Position(75, 100, 50) == 50 && UiLayout.Position(10, 40, 50) == 0);
var history = new DesignHistory(3);
Assert(!history.CanUndo && !history.CanRedo && history.Current == null);
history.Record("initial"); history.Record("initial");
Assert(!history.CanUndo && history.Current == "initial");
history.Record("effect-added"); history.Record("invalid-value");
Assert(history.CanUndo && !history.CanRedo && history.PeekUndo() == "effect-added");
history.Undo(); Assert(history.Current == "effect-added" && history.CanRedo);
history.Redo(); Assert(history.Current == "invalid-value");
history.Undo(); history.Record("stone-added");
Assert(!history.CanRedo && history.Current == "stone-added");
history.Record("rarity-changed"); history.Undo(); history.Undo();
Assert(history.Current == "effect-added" && !history.CanUndo && history.CanRedo);
history.Clear(); Assert(history.Current == null && !history.CanRedo && !history.CanUndo);
string serializedDraft = Newtonsoft.Json.JsonConvert.SerializeObject(new Recipe { Prefab = "SwordIron", Quality = "unfinished" });
history.Record(serializedDraft); history.Record("other"); history.Undo();
Assert(Newtonsoft.Json.JsonConvert.DeserializeObject<Recipe>(history.Current).Quality == "unfinished");
Assert(Rules.DurabilityRatio(30, 100) == 0.3f);
Assert(Rules.DurabilityRatio(150, 100) == 1);
Assert(Rules.DurabilityRatio(-5, 100) == 0);
Assert(Rules.DurabilityRatio(10, 0) == 0);
Assert(Rules.DurabilityRatio(float.NaN, 100) == 0);
Assert(Rules.DurabilityRatio(float.PositiveInfinity, 100) == 0);
Assert(Rules.DurabilityRatio(100, float.NaN) == 0);
Assert(Rules.DurabilityRatio(100, float.PositiveInfinity) == 0);
string[] oldTypes = { "A", "B", "C" }, newTypes = { "C", "A", "D" };
Assert(Rules.RemapEffectIndex(0, oldTypes, newTypes) == 1);
Assert(Rules.RemapEffectIndex(1, oldTypes, newTypes) == -1);
Assert(Rules.RemapEffectIndex(2, oldTypes, newTypes) == 0);
Assert(Rules.RemapEffectIndex(-1, oldTypes, newTypes) == -1);
Assert(Rules.RemapEffectIndex(99, oldTypes, newTypes) == -1);
var sharedRecipe = new Recipe { Prefab = "SwordIron", Rarity = 2, Quality = "3", Count = "2", SocketCount = "1", Name = "Paylaşılan kılıç" };
sharedRecipe.Effects.Add(new EffectChoice { Type = "AddDamage", Value = "25,5" });
sharedRecipe.Stones.Add("Red_Epic_ShardStone");
string sharedText = RecipeTransfer.Encode(sharedRecipe, 6);
var decoded = RecipeTransfer.Decode(sharedText, 6);
Assert(decoded.Prefab == sharedRecipe.Prefab && decoded.Name == sharedRecipe.Name && decoded.Quality == "3");
Assert(decoded.Effects[0].Value == "25,5" && decoded.Stones[0] == "Red_Epic_ShardStone");
decoded.Effects[0].Value = "10"; Assert(sharedRecipe.Effects[0].Value == "25,5");
Reject(() => RecipeTransfer.Decode("", 6));
Reject(() => RecipeTransfer.Decode("{ broken json", 6));
Reject(() => RecipeTransfer.Decode("null", 6));
Reject(() => RecipeTransfer.Decode("{}", 6));
Reject(() => RecipeTransfer.Decode(sharedText.Replace("\"Version\": 1", "\"Version\": 99"), 6));
Reject(() => RecipeTransfer.Decode(sharedText.Replace("EpicLootSpawner.Recipe", "Other.Format"), 6));
Reject(() => RecipeTransfer.Decode(new string('x', RecipeTransfer.MaxLength + 1), 6));
Reject(() => RecipeTransfer.Decode(sharedText.Replace("\"Rarity\": 2", "\"Rarity\": 99"), 6));
Reject(() => RecipeTransfer.Decode(sharedText.Replace("\"Value\": \"25,5\"", "\"Value\": \"NaN\""), 6));
string typeText = sharedText.Replace("\"Format\":", "\"$type\":\"Invalid.Type, NotInstalled\",\"Format\":");
Assert(RecipeTransfer.Decode(typeText, 6).Prefab == "SwordIron");
Directory.CreateDirectory(directory);
try
{
    string path = Path.Combine(directory, "presets.json");
    var store = new PresetStore(path, 6);
    store.Load(); Assert(store.Ready && store.Names.Length == 0);
    string longName = new string('a', 200);
    store.Save(longName, valid);
    Assert(new FileInfo(path).Length > 262144);
    valid.Effects[0].Value = "99";
    Assert(store.Get(longName).Effects[0].Value == "5000");
    var loaded = new PresetStore(path, 6); loaded.Load();
    var recipe = loaded.Get(longName);
    Assert(recipe.Quality == "100000" && recipe.Count == "100000");
    Assert(recipe.Stones.Count == 10 && recipe.Effects.Count == 100);
    Assert(recipe.Name.Length == 300000);
    recipe.Stones.Clear(); Assert(loaded.Get(longName).Stones.Count == 10);
    loaded.Save(longName.ToUpperInvariant(), loaded.Get(longName));
    Assert(loaded.Names.Length == 1 && File.Exists(path + ".bak"));
    for (int i = 0; i < 101; i++) loaded.Save("Preset-" + i, new Recipe { Prefab = "SwordIron", Rarity = 2 });
    Assert(loaded.Names.Length == 102);
    loaded.Load(); Assert(loaded.Names.Length == 102);
    Assert(loaded.Contains("  PRESET-0  "));
    Assert(!loaded.Contains(null) && !loaded.Contains("missing"));
    loaded.Rename("Preset-0", " Renamed ");
    Assert(!loaded.Contains("Preset-0") && loaded.Contains("Renamed") && loaded.Names.Length == 102);
    Assert(loaded.Get("Renamed").Prefab == "SwordIron");
    var beforeRename = new PresetStore(path + ".bak", 6); beforeRename.Load();
    Assert(beforeRename.Contains("Preset-0") && !beforeRename.Contains("Renamed"));
    loaded.Load(); Assert(loaded.Contains("Renamed") && !loaded.Contains("Preset-0"));
    loaded.Rename("Renamed", "RENAMED");
    Assert(Array.IndexOf(loaded.Names, "RENAMED") >= 0 && loaded.Names.Length == 102);
    string unchanged = File.ReadAllText(path);
    Reject(() => loaded.Rename("RENAMED", "Preset-1"));
    Reject(() => loaded.Rename("missing", "Other"));
    Reject(() => loaded.Rename("RENAMED", " "));
    Reject(() => loaded.Delete("missing"));
    Assert(File.ReadAllText(path) == unchanged && loaded.Contains("RENAMED") && loaded.Contains("Preset-1"));
    using (var lockedFile = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    {
        try { loaded.Delete("RENAMED"); throw new Exception("Locked file mutation accepted"); }
        catch (IOException) { checks++; }
        Assert(loaded.Contains("RENAMED") && loaded.Names.Length == 102);
    }
    Assert(Directory.GetFiles(directory, "*.tmp").Length == 0);
    loaded.Delete("renamed");
    Assert(!loaded.Contains("RENAMED") && loaded.Contains("Preset-1") && loaded.Names.Length == 101);
    var beforeDelete = new PresetStore(path + ".bak", 6); beforeDelete.Load();
    Assert(beforeDelete.Contains("RENAMED"));
    loaded.Load(); Assert(!loaded.Contains("RENAMED") && loaded.Names.Length == 101);
    Reject(() => loaded.Save("", valid));
    File.WriteAllText(path, "{ broken json");
    try { loaded.Load(); throw new Exception("Corrupt file accepted"); }
    catch (Newtonsoft.Json.JsonException) { checks++; }
    Assert(!loaded.Ready);
    Reject(() => loaded.Save("Another", valid));
    Reject(() => loaded.Rename("Preset-1", "Other"));
    Reject(() => loaded.Delete("Preset-1"));
    Assert(File.ReadAllText(path) == "{ broken json");
    File.WriteAllText(path, "{\"Version\":99,\"Presets\":{}}"); Reject(loaded.Load);
    File.WriteAllText(path, "{\"Version\":1,\"Presets\":{}}"); loaded.Load(); Assert(loaded.Ready);
    string favoritePath = Path.Combine(directory, "favorites.json");
    var favorites = new FavoriteStore(favoritePath); favorites.Load();
    Assert(favorites.Ready && !favorites.Contains("SwordIron"));
    favorites.Toggle("SwordIron"); Assert(favorites.Contains("SwordIron"));
    var savedFavorites = new FavoriteStore(favoritePath); savedFavorites.Load();
    Assert(savedFavorites.Contains("SwordIron"));
    favorites.Toggle("SwordIron"); Assert(!favorites.Contains("SwordIron"));
    var favoriteBackup = new FavoriteStore(favoritePath + ".bak"); favoriteBackup.Load();
    Assert(favoriteBackup.Contains("SwordIron"));
    Reject(() => favorites.Toggle(" "));
    using (var lockedFavorites = new FileStream(favoritePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    {
        try { favorites.Toggle("HelmetIron"); throw new Exception("Locked favorite file mutation accepted"); }
        catch (IOException) { checks++; }
        Assert(!favorites.Contains("HelmetIron"));
    }
    File.WriteAllText(favoritePath, "{ broken json");
    try { favorites.Load(); throw new Exception("Corrupt favorite file accepted"); }
    catch (Newtonsoft.Json.JsonException) { checks++; }
    Assert(!favorites.Ready);
    Reject(() => favorites.Toggle("SwordIron"));
    Assert(File.ReadAllText(favoritePath) == "{ broken json");
    File.WriteAllText(favoritePath, "{\"Version\":99,\"Prefabs\":[]}"); Reject(favorites.Load);
    File.WriteAllText(favoritePath, "{\"Version\":1,\"Prefabs\":[null]}"); Reject(favorites.Load);
    File.WriteAllText(favoritePath, "{\"Version\":1,\"Prefabs\":[\"SwordIron\",\"SwordIron\"]}"); favorites.Load();
    favorites.Toggle("SwordIron"); Assert(!favorites.Contains("SwordIron"));
    Assert(Directory.GetFiles(directory, "*.tmp").Length == 0);
    string draftPath = Path.Combine(directory, "draft.json");
    var drafts = new DraftStore(draftPath, 6); Assert(drafts.Load() == null && drafts.Ready);
    var unfinished = new SavedDraft { Recipe = new Recipe { Prefab = "SwordIron", Quality = "unfinished", Count = "", SocketCount = "broken" }, StoneRarity = 4, ToInventory = false };
    unfinished.Recipe.Effects.Add(new EffectChoice { Type = "AddDamage", Value = "unfinished" });
    drafts.Save(unfinished);
    string unchangedDraft = File.ReadAllText(draftPath);
    drafts.Save(unfinished); Assert(!File.Exists(draftPath + ".bak"));
    var resumed = new DraftStore(draftPath, 6); var resumedDraft = resumed.Load();
    Assert(resumedDraft.Recipe.Quality == "unfinished" && resumedDraft.Recipe.Effects[0].Value == "unfinished");
    Assert(resumedDraft.StoneRarity == 4 && !resumedDraft.ToInventory);
    Assert(!unchangedDraft.Contains("InventoryKey"));
    resumedDraft.Recipe.Name = "İkinci taslak"; resumed.Save(resumedDraft);
    var previousDraft = new DraftStore(draftPath + ".bak", 6).Load(); Assert(previousDraft.Recipe.Name == "");
    using (var lockedDraft = new FileStream(draftPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    {
        try { resumed.Save(new SavedDraft { Recipe = new Recipe { Prefab = "HelmetIron" } }); throw new Exception("Locked draft mutation accepted"); }
        catch (IOException) { checks++; }
    }
    Assert(resumed.Load().Recipe.Name == "İkinci taslak");
    resumed.Save(new SavedDraft { Recipe = new Recipe() }); Assert(resumed.Load().Recipe.Prefab == null);
    File.WriteAllText(draftPath, "{ broken json");
    try { resumed.Load(); throw new Exception("Corrupt draft accepted"); }
    catch (Newtonsoft.Json.JsonException) { checks++; }
    Assert(!resumed.Ready);
    Reject(() => resumed.Save(unfinished)); Assert(File.ReadAllText(draftPath) == "{ broken json");
    Reject(() => new SavedDraft { Recipe = new Recipe(), StoneRarity = 99 }.Validate(6));
    Reject(() => new SavedDraft { Recipe = new Recipe { Effects = null } }.Validate(6));
    Assert(Directory.GetFiles(directory, "*.tmp").Length == 0);
}
finally
{
    foreach (string file in Directory.GetFiles(directory)) File.Delete(file);
    for (int retry = 0; Directory.Exists(directory) && retry < 3; retry++)
    {
        try { Directory.Delete(directory); }
        catch (IOException) when (retry < 2) { System.Threading.Thread.Sleep(50); }
    }
}
FeatureChecks.Run((condition, label) => { if (!condition) throw new Exception(label); checks++; });
ResizeChecks.Run((condition, label) => { if (!condition) throw new Exception(label); checks++; });
EffectCapChecks.Run((condition, label) => { if (!condition) throw new Exception(label); checks++; });
Console.WriteLine($"PASS: {checks} boundary, slider, preset and persistence checks");
