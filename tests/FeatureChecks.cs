using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using EpicLootSpawner;

internal static class FeatureChecks
{
    public static void Run(Action<bool, string> check)
    {
        void Reject(Action action, string label) { bool failed = false; try { action(); } catch { failed = true; } check(failed, label); }
        string root = Path.Combine(Path.GetTempPath(), "SpawnerFeatures-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string malformedPath = Path.Combine(root, "malformed-presets.json");
            var malformedStore = new PresetStore(malformedPath, 6); malformedStore.Load();
            malformedStore.Save("Keep", new Recipe { Prefab = "Axe" });
            foreach (string badJson in new[] { "{}", "{\"Version\":2}", "{\"Presets\":{}}", "{\"Version\":2,\"Presets\":null}" })
            {
                File.WriteAllText(malformedPath, badJson);
                Reject(malformedStore.Load, "missing preset schema blocks load");
                check(!malformedStore.Ready && malformedStore.Names.Contains("Keep"), "invalid load preserves prior preset memory but blocks writes");
                Reject(() => malformedStore.Save("Overwrite", new Recipe { Prefab = "Axe" }), "invalid preset file cannot be overwritten");
                check(File.ReadAllText(malformedPath) == badJson, "malformed preset bytes preserved");
            }
            File.WriteAllText(malformedPath, "{\"Version\":1,\"Presets\":{}}"); malformedStore.Load();
            check(malformedStore.Ready, "legacy version one still accepts omitted tags");
            string malformedFavoritePath = Path.Combine(root, "malformed-favorites.json");
            var malformedFavorites = new FavoriteStore(malformedFavoritePath); malformedFavorites.Load(); malformedFavorites.Toggle("Axe");
            foreach (string badJson in new[] { "{}", "{\"Version\":1}", "{\"Prefabs\":[]}", "{\"Version\":1,\"Prefabs\":null}" })
            {
                File.WriteAllText(malformedFavoritePath, badJson);
                Reject(malformedFavorites.Load, "missing favorite schema blocks load");
                check(!malformedFavorites.Ready && malformedFavorites.Contains("Axe"), "invalid favorite load preserves prior memory");
                Reject(() => malformedFavorites.Toggle("Sword"), "invalid favorites cannot be overwritten");
                check(File.ReadAllText(malformedFavoritePath) == badJson, "malformed favorite bytes preserved");
            }
            File.WriteAllText(malformedFavoritePath, "{\"Version\":1,\"Prefabs\":[]}"); malformedFavorites.Load();
            check(malformedFavorites.Ready && !malformedFavorites.Contains("Axe"), "explicitly empty favorites remain valid");
            foreach (var range in new[] { (float.NaN, 20f), (1f, float.NaN), (float.NegativeInfinity, 20f), (1f, float.PositiveInfinity), (20f, 1f) })
            {
                check(!Rules.ValidRange(range.Item1, range.Item2), "malformed range detected for UI");
                Reject(() => Rules.EffectValue("10", range.Item1, range.Item2), "malformed configured range cannot accept a finite value");
            }
            check(Rules.ValidRange(1, 1) && Rules.EffectValue("1", 1, 1) == 1, "fixed valid ranges retained");
            check(Rules.SliderValue(10, 1, 20, float.PositiveInfinity) == 10 && Rules.SliderValue(10, 1, 20, float.NaN) == 10, "invalid step cannot turn slider input into NaN");
            check(PresetTags.Parse(" savaş, odun, SAVAŞ, ").SequenceEqual(new[] { "savaş", "odun" }), "tag normalization");
            Reject(() => PresetTags.Normalize(new[] { new string('x', 25) }), "tag length");
            Reject(() => PresetTags.Normalize(Enumerable.Range(0, 9).Select(x => "tag" + x)), "tag count");
            Reject(() => PresetTags.Normalize(new[] { "bad,tag" }), "embedded comma");
            Reject(() => PresetTags.Normalize(new[] { "bad\ntag" }), "control character");
            string path = Path.Combine(root, "presets.json");
            File.WriteAllText(path, "{\"Version\":1,\"Presets\":{\"Odun\":{\"Prefab\":\"Axe\",\"Rarity\":2}}}");
            var store = new PresetStore(path, 6); store.Load();
            check(store.GetTags("Odun").Length == 0 && store.Get("Odun").Prefab == "Axe", "legacy v1 read");
            store.SetTags("Odun", "odun, alet");
            check(File.ReadAllText(path + ".bak").Contains("\"Version\":1"), "migration retains original v1 backup");
            check(File.ReadAllText(path).Contains("\"Version\": 2"), "v2 written on deliberate change");
            check(File.ReadAllText(path + ".v1.bak").Contains("\"Version\":1"), "permanent original migration backup");
            string before = File.ReadAllText(path); int revision = store.Revision;
            Reject(() => store.Save("Invalid", new Recipe { Prefab = "Axe" }, new string('t', 25)), "invalid metadata save rejected");
            check(File.ReadAllText(path) == before && store.Revision == revision && !store.Contains("Invalid"), "invalid metadata write atomic");
            store.Duplicate("Odun", "Kopya");
            check(File.ReadAllText(path + ".v1.bak").Contains("\"Version\":1"), "later writes retain original migration backup");
            check(store.Get("Kopya").Prefab == "Axe" && store.GetTags("Kopya").SequenceEqual(new[] { "odun", "alet" }), "copy includes metadata");
            var copy = store.Get("Kopya"); copy.Prefab = "Changed";
            check(store.Get("Odun").Prefab == "Axe" && store.Get("Kopya").Prefab == "Axe", "copy detached from source");
            var copiedTags = store.GetTags("Kopya"); copiedTags[0] = "Changed";
            check(store.GetTags("Kopya")[0] == "odun", "tags detached");
            Reject(() => store.Duplicate("Odun", "ODUN"), "copy collision ignores case");
            store.Rename("Kopya", "Yeni");
            check(!store.Contains("Kopya") && store.GetTags("Yeni")[0] == "odun", "rename moves tags");
            store.Delete("Yeni");
            check(store.GetTags("Yeni").Length == 0, "delete removes metadata");
            store.Save("Savaş", new Recipe { Prefab = "SwordIron" }, "savaş, alet");
            var query = new PresetQuery();
            check(query.Get(store, "Axe", "").SequenceEqual(new[] { "Odun" }), "prefab search");
            check(query.Get(store, "", "alet, odun").SequenceEqual(new[] { "Odun" }), "tags combine with AND");
            check(query.Get(store, "Savaş", "odun").Length == 0, "search combines with tags");
            string[] cached = query.Get(store, "", "alet");
            check(Enumerable.Range(0, 1000).All(i => ReferenceEquals(cached, query.Get(store, "", "alet"))), "unchanged query reuses results");
            store.SetTags("Savaş", "savaş");
            check(query.Get(store, "", "alet").SequenceEqual(new[] { "Odun" }), "revision refreshes cached filter");
            string secondPath = Path.Combine(root, "second.json"); var secondStore = new PresetStore(secondPath, 6); secondStore.Load();
            check(query.Get(secondStore, "", "alet").Length == 0, "cache keyed by store identity");

            var entries = new[] { new CollectionEntry { Name = "Odun", Recipe = store.Get("Odun"), Tags = store.GetTags("Odun") }, new CollectionEntry { Name = "Savaş", Recipe = store.Get("Savaş"), Tags = store.GetTags("Savaş") } };
            string json = CollectionTransfer.Encode(entries, 6);
            var rows = CollectionTransfer.Decode(json, 6);
            check(rows.Count == 2 && rows.All(x => x.Error == null) && rows[0].Entry.Tags[0] == "odun", "collection roundtrip");
            var baseErrors = rows.ToDictionary(x => x, x => x.Error);
            CollectionTransfer.RefreshRows(rows, store, baseErrors);
            check(rows.All(x => x.Conflict && !x.Selected), "existing names not automatically imported");
            rows[0].Entry.Name = "OdunYeni";
            CollectionTransfer.RefreshRows(rows, store, baseErrors);
            check(rows[0].Error == null && !rows[0].Selected && rows[1].Conflict, "rename resolves conflict and requires explicit selection");
            rows[0].Selected = true;
            store.ImportEntries(rows.Where(x => x.Selected && x.Error == null).Select(x => x.Entry));
            check(store.Contains("OdunYeni") && store.GetTags("OdunYeni")[0] == "odun", "selective import stores tags");
            before = File.ReadAllText(path);
            Reject(() => store.ImportEntries(new[] { new CollectionEntry { Name = "Candidate", Recipe = new Recipe { Prefab = "Axe" } }, entries[0] }), "batch collision rejected");
            check(!store.Contains("Candidate") && File.ReadAllText(path) == before, "batch rejection leaves data unchanged");
            Reject(() => CollectionTransfer.Encode(Array.Empty<CollectionEntry>(), 6), "empty export");
            Reject(() => CollectionTransfer.Encode(Enumerable.Repeat(entries[0], 501), 6), "oversized entry count");
            Reject(() => CollectionTransfer.Encode(new[] { entries[0], entries[0] }, 6), "duplicate export names");
            Reject(() => CollectionTransfer.Decode(json.Replace("\"Version\": 1", "\"Version\": 99"), 6), "unknown collection version");
            Reject(() => CollectionTransfer.Decode("{\"Format\":\"wrong\",\"Version\":1,\"Entries\":[]}", 6), "unknown format");
            Reject(() => CollectionTransfer.Decode(new string('x', CollectionTransfer.MaxLength + 1), 6), "text size bound");
            Reject(() => CollectionTransfer.Decode("{", 6), "broken collection JSON");
            rows = CollectionTransfer.Decode("{\"Format\":\"EpicLootSpawner.Collection\",\"Version\":1,\"Entries\":[{\"Name\":\"Good\",\"Recipe\":{\"Prefab\":\"Axe\"}},{\"Name\":\"Bad\",\"Recipe\":{\"Prefab\":\"Axe\",\"Count\":\"abc\"}},null]}", 6);
            check(rows[0].Error == null && rows[1].Error != null && rows[2].Error != null, "bad records isolated");
            rows = CollectionTransfer.Decode("{\"Format\":\"EpicLootSpawner.Collection\",\"Version\":1,\"Entries\":[{\"Name\":\"Dup\",\"Recipe\":{\"Prefab\":\"Axe\"}},{\"Name\":\"dup\",\"Recipe\":{\"Prefab\":\"Axe\"}}]}", 6);
            baseErrors = rows.ToDictionary(x => x, x => x.Error);
            CollectionTransfer.RefreshRows(rows, store, baseErrors);
            check(rows.All(x => x.Conflict && !x.Selected), "all duplicate rows disabled");
            rows[1].Entry.Name = "Distinct";
            CollectionTransfer.RefreshRows(rows, store, baseErrors);
            check(rows.All(x => x.Error == null), "duplicate rename recoverable");

            string recoverPath = Path.Combine(root, "recover.json");
            File.WriteAllText(recoverPath, "{broken");
            File.WriteAllText(recoverPath + ".bak", json.Replace("EpicLootSpawner.Collection", "unsupported"));
            Reject(() => RecoveryService.Inspect(recoverPath, recoverPath + ".bak", "Şablonlar", 6, false), "backup schema validated");
            File.WriteAllText(recoverPath + ".bak", "{\"Version\":1,\"Presets\":{\"Backup\":{\"Prefab\":\"Axe\"}}}");
            var candidate = RecoveryService.Inspect(recoverPath, recoverPath + ".bak", "Şablonlar", 6, false);
            check(candidate.Kept == 1 && File.ReadAllText(recoverPath) == "{broken", "recovery preview does not write");
            string preserved = RecoveryService.Restore(candidate);
            check(File.ReadAllText(preserved) == "{broken", "original corrupt file preserved");
            var recoveredStore = new PresetStore(recoverPath, 6); recoveredStore.Load();
            check(recoveredStore.Contains("Backup"), "backup successfully restored");
            Reject(() => RecoveryService.Inspect(recoverPath, recoverPath + ".bak", "Şablonlar", 6, false), "replaced backup contains former broken file");
            candidate = RecoveryService.Inspect(recoverPath, recoverPath, "Şablonlar", 6, false);
            int preservedCount = Directory.GetFiles(root).Length;
            File.WriteAllText(recoverPath, "external change");
            Reject(() => RecoveryService.Restore(candidate), "stale recovery preview rejected");
            check(File.ReadAllText(recoverPath) == "external change" && Directory.GetFiles(root).Length == preservedCount, "external changes protected without extra backup");
            File.WriteAllText(recoverPath, "{\"Version\":2,\"Presets\":{\"Good\":{\"Prefab\":\"Axe\"},\"Bad\":{\"Prefab\":\"Axe\",\"Count\":\"abc\"}},\"Tags\":{\"Good\":[\"odun\"]}}");
            candidate = RecoveryService.Inspect(recoverPath, recoverPath, "Şablonlar", 6, true);
            check(candidate.Kept == 1 && candidate.Skipped == 1, "valid record salvage isolates bad entry");
            preserved = RecoveryService.Restore(candidate);
            recoveredStore.Load();
            check(recoveredStore.Contains("Good") && !recoveredStore.Contains("Bad") && recoveredStore.GetTags("Good")[0] == "odun", "salvage preserves tags");
            check(File.ReadAllText(preserved).Contains("abc"), "salvage preserves rejected record in original copy");
            File.WriteAllText(recoverPath, "{\"Version\":99,\"Presets\":{}}");
            Reject(() => RecoveryService.Inspect(recoverPath, recoverPath, "Şablonlar", 6, true), "unknown schema not salvaged");
            File.WriteAllText(recoverPath, "{\"Version\":1,\"Presets\":{\"Bad\":null}}");
            Reject(() => RecoveryService.Inspect(recoverPath, recoverPath, "Şablonlar", 6, true), "no valid records not restored as empty");
            string favoritePath = Path.Combine(root, "favorites.json");
            File.WriteAllText(favoritePath, "{\"Version\":1,\"Prefabs\":[\"Axe\",null,\"Axe\",\"Sword\"]}");
            candidate = RecoveryService.Inspect(favoritePath, favoritePath, "Favoriler", 6, true);
            check(candidate.Kept == 2 && candidate.Skipped == 1, "favorite salvage removes invalid/duplicate entries");
            RecoveryService.Restore(candidate);
            var favorites = new FavoriteStore(favoritePath); favorites.Load();
            check(favorites.Ready && favorites.Contains("Axe") && favorites.Contains("Sword"), "favorite recovery loadable");
            string draftPath = Path.Combine(root, "draft.json");
            File.WriteAllText(draftPath, "broken");
            File.WriteAllText(draftPath + ".bak", JsonConvert.SerializeObject(new SavedDraft { Recipe = new Recipe { Prefab = "Axe", Quality = "" } }));
            candidate = RecoveryService.Inspect(draftPath, draftPath + ".bak", "Taslak", 6, false);
            RecoveryService.Restore(candidate);
            check(new DraftStore(draftPath, 6).Load().Recipe.Quality == "", "unfinished draft preserved during recovery");
            string huge = Path.Combine(root, "huge.json"); File.WriteAllText(huge, new string(' ', 4194305));
            Reject(() => LocalJson.Read(huge), "file read byte limit");
            File.WriteAllText(path, "broken"); Reject(store.Load, "store load becomes blocked");
            check(query.Get(store, "", "alet").Length == 0, "blocked store invalidates cache");

            var specialRecipe = new Recipe { Prefab = "Staff", Rarity = 3, LegendaryID = "MossStaff", SetID = "MossSet" };
            string specialPath = Path.Combine(root, "special.json"); var specialStore = new PresetStore(specialPath, 6); specialStore.Load();
            specialStore.Save("Special", specialRecipe); specialStore.Load();
            check(specialStore.Get("Special").LegendaryID == "MossStaff" && specialStore.Get("Special").SetID == "MossSet", "preset disk roundtrip retains special identity");
            specialStore.Duplicate("Special", "Special copy");
            check(specialStore.Get("Special copy").SetID == "MossSet", "preset duplication retains set identity");
            var detached = specialStore.Get("Special"); detached.LegendaryID = "Other";
            check(specialStore.Get("Special").LegendaryID == "MossStaff", "returned identity fields are detached from store");
            var sharedSpecial = RecipeTransfer.Decode(RecipeTransfer.Encode(specialRecipe, 6), 6);
            check(sharedSpecial.LegendaryID == "MossStaff" && sharedSpecial.SetID == "MossSet", "clipboard roundtrip retains unique identity");
            var collectionSpecial = CollectionTransfer.Decode(CollectionTransfer.Encode(new[] { new CollectionEntry { Name = "Special", Recipe = specialRecipe } }, 6), 6).Single();
            check(collectionSpecial.Error == null && collectionSpecial.Entry.Recipe.SetID == "MossSet", "collection roundtrip retains set membership");
            string specialDraftPath = Path.Combine(root, "special-draft.json"); var specialDraftStore = new DraftStore(specialDraftPath, 6); specialDraftStore.Load();
            specialDraftStore.Save(new SavedDraft { Recipe = specialRecipe });
            check(new DraftStore(specialDraftPath, 6).Load().Recipe.LegendaryID == "MossStaff", "session draft retains identity");
            var oldRecipe = JsonConvert.DeserializeObject<Recipe>("{\"Prefab\":\"Sword\",\"Rarity\":3}"); oldRecipe.Validate(6);
            check(oldRecipe.LegendaryID == null && oldRecipe.SetID == null, "legacy recipe stays ordinary legendary rarity");
            Reject(() => new Recipe { Prefab = "Sword", SetID = "WithoutPiece" }.Validate(6), "orphan set ID rejected at format boundary");
            Reject(() => new Recipe { Prefab = "Sword", LegendaryID = new string('x', 257) }.Validate(6), "oversized identity rejected");
            var identityDiff = ItemDifference.Between(new ItemSnapshot(), new ItemSnapshot { LegendaryID = "MossStaff", SetID = "MossSet" });
            check(identityDiff.Identity && identityDiff.Set && identityDiff.HasChanges, "comparison detects identity-only change");
            identityDiff = ItemDifference.Between(new ItemSnapshot(), new ItemSnapshot { LegendaryID = "", SetID = "" });
            check(!identityDiff.HasChanges, "empty and absent identities are equivalent");

            string benchPath = Path.Combine(root, "bench.json"); var bench = new PresetStore(benchPath, 6); bench.Load();
            bench.ImportEntries(Enumerable.Range(0, 300).Select(i => new CollectionEntry { Name = "Recipe-" + i, Recipe = new Recipe { Prefab = "Axe" } }));
            var cachedQuery = new PresetQuery();
            Func<string[]> legacy = () => bench.Names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Where(x => x.IndexOf("Recipe-2", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            for (int i = 0; i < 20; i++) { legacy(); cachedQuery.Get(bench, "Recipe-2", ""); }
            var timer = Stopwatch.StartNew(); int oldCount = 0;
            for (int i = 0; i < 2000; i++) oldCount += legacy().Length;
            double oldMs = timer.Elapsed.TotalMilliseconds;
            timer.Restart(); int newCount = 0;
            for (int i = 0; i < 2000; i++) newCount += cachedQuery.Get(bench, "Recipe-2", "").Length;
            double newMs = timer.Elapsed.TotalMilliseconds;
            check(oldCount == newCount && oldCount > 0, "benchmark compares same filter results");
            Console.WriteLine("BENCHMARK " + JsonConvert.SerializeObject(new { records = 300, iterations = 2000, legacyMs = oldMs, cachedMs = newMs, results = oldCount }));
        }
        finally
        {
            foreach (string file in Directory.GetFiles(root)) File.Delete(file);
            Directory.Delete(root);
        }
    }
}
