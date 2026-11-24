using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace EpicLootSpawner
{
    internal sealed class Recipe
    {
        public string Prefab;
        public int Rarity;
        public string LegendaryID, SetID;
        public string Quality = "1", Count = "1", SocketCount = "0", Name = "";
        public List<EffectChoice> Effects = new List<EffectChoice>();
        public List<string> Stones = new List<string>();

        public void Validate(int rarityCount)
        {
            if (string.IsNullOrWhiteSpace(Prefab)) throw new InvalidOperationException("Şablonda eşya kimliği eksik veya çok uzun.");
            if (Rarity < 0 || Rarity >= rarityCount) throw new InvalidOperationException("Şablonun nadirliği bu EpicLoot sürümünde yok.");
            if ((LegendaryID?.Length ?? 0) > 256 || (SetID?.Length ?? 0) > 256 || (string.IsNullOrEmpty(LegendaryID) && !string.IsNullOrEmpty(SetID)))
                throw new InvalidOperationException("Şablonun özel eşya/set kimliği geçersiz.");
            Rules.Whole(Quality, 1, "Eşya seviyesi");
            Rules.Whole(Count, 1, "Adet");
            Rules.Whole(SocketCount, 0, "Soket sayısı");
            if (Name == null) Name = "";
            if (Effects == null || Stones == null)
                throw new InvalidOperationException("Şablondaki efekt veya taş listesi geçersiz.");
            foreach (var effect in Effects)
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.Type))
                    throw new InvalidOperationException("Şablonda eksik veya geçersiz efekt var.");
                Rules.EffectValue(effect.Value, null, null);
            }
            if (Stones.Any(x => string.IsNullOrWhiteSpace(x))) throw new InvalidOperationException("Şablondaki taş kimliği geçersiz.");
        }
    }

    internal sealed class PresetFile
    {
        [JsonProperty(Required = Required.Always)] public int Version = 2;
        [JsonProperty(Required = Required.Always)] public Dictionary<string, Recipe> Presets = new Dictionary<string, Recipe>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string[]> Tags = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class PresetStore
    {
        private readonly string path;
        private readonly int rarityCount;
        private PresetFile data = new PresetFile();
        private string legacyJson;
        public bool Ready { get; private set; }
        public string Error { get; private set; }
        private string[] names = Array.Empty<string>();
        public string[] Names => names;
        public int Revision { get; private set; }
        internal string FilePath => path;
        public PresetStore(string path, int rarityCount) { this.path = path; this.rarityCount = rarityCount; }

        public void Load()
        {
            Ready = false;
            try
            {
                var loaded = new PresetFile();
                string loadedText = null;
                if (File.Exists(path))
                {
                    loadedText = File.ReadAllText(path);
                    loaded = JsonConvert.DeserializeObject<PresetFile>(loadedText, CollectionTransfer.Settings);
                }
                if (loaded == null || (loaded.Version != 1 && loaded.Version != 2) || loaded.Presets == null)
                    throw new InvalidOperationException("Şablon dosyasının biçimi veya sürümü desteklenmiyor.");
                var normalized = new Dictionary<string, Recipe>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in loaded.Presets)
                {
                    string name = ValidName(pair.Key);
                    if (pair.Value == null || normalized.ContainsKey(name)) throw new InvalidOperationException("Şablon adı yinelenmiş veya içerik eksik.");
                    pair.Value.Validate(rarityCount);
                    normalized.Add(name, pair.Value);
                }
                loaded.Presets = normalized;
                string oldVersion = loaded.Version == 1 ? loadedText : null;
                var tags = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in loaded.Tags ?? new Dictionary<string, string[]>())
                    if (normalized.ContainsKey(pair.Key.Trim())) tags[pair.Key.Trim()] = PresetTags.Normalize(pair.Value);
                loaded.Tags = tags; loaded.Version = 2;
                data = loaded;
                legacyJson = oldVersion;
                RefreshNames();
                Ready = true;
                Error = null;
            }
            catch (Exception ex) { Error = "Şablon dosyası okunamadı; üzerine yazılmayacak. " + ex.Message; throw; }
        }

        public Recipe Get(string name)
        {
            if (!Ready) throw new InvalidOperationException(Error ?? "Şablon deposu hazır değil.");
            return Copy(data.Presets[ValidName(name)]);
        }

        public bool Contains(string name) => Ready && !string.IsNullOrWhiteSpace(name) && data.Presets.ContainsKey(name.Trim());
        public string[] GetTags(string name) => data.Tags.TryGetValue(ValidName(name), out var tags) ? tags.ToArray() : Array.Empty<string>();
        public void SetTags(string name, string text)
        {
            EnsureReady(); name = ValidName(name);
            if (!data.Presets.ContainsKey(name)) throw new InvalidOperationException("Şablon bulunamadı.");
            var updated = Snapshot(); updated.Tags[name] = PresetTags.Parse(text); Commit(updated);
        }
        public void Duplicate(string source, string target)
        {
            EnsureReady(); source = ValidName(source); target = ValidName(target);
            if (Contains(target)) throw new InvalidOperationException("Bu adla bir şablon var. Farklı ad yaz.");
            var updated = Snapshot(); updated.Presets.Add(target, Get(source)); updated.Tags[target] = GetTags(source); Commit(updated);
        }
        public void ImportEntries(IEnumerable<CollectionEntry> entries)
        {
            EnsureReady(); var updated = Snapshot(); int count = 0;
            foreach (var entry in entries)
            {
                string name = ValidName(entry.Name);
                if (updated.Presets.ContainsKey(name)) throw new InvalidOperationException("Ad çakışması: " + name);
                entry.Recipe.Validate(rarityCount); updated.Presets.Add(name, Copy(entry.Recipe));
                updated.Tags[name] = PresetTags.Normalize(entry.Tags); count++;
            }
            if (count == 0) throw new InvalidOperationException("Alınacak kayıt seçilmedi.");
            Commit(updated);
        }

        public void Rename(string oldName, string newName)
        {
            EnsureReady();
            oldName = ValidName(oldName); newName = ValidName(newName);
            if (!data.Presets.TryGetValue(oldName, out var recipe)) throw new InvalidOperationException("Şablon bulunamadı: " + oldName);
            if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase) && data.Presets.ContainsKey(newName))
                throw new InvalidOperationException("Bu adla başka bir şablon var. Farklı bir ad yaz.");
            var updated = Snapshot();
            updated.Presets.Remove(oldName);
            updated.Presets.Add(newName, recipe);
            if (updated.Tags.TryGetValue(oldName, out var tags)) { updated.Tags.Remove(oldName); updated.Tags[newName] = tags; }
            Commit(updated);
        }

        public void Delete(string name)
        {
            EnsureReady();
            name = ValidName(name);
            var updated = Snapshot();
            if (!updated.Presets.Remove(name)) throw new InvalidOperationException("Şablon bulunamadı: " + name);
            updated.Tags.Remove(name);
            Commit(updated);
        }

        private void EnsureReady()
        {
            if (!Ready) throw new InvalidOperationException(Error ?? "Şablon deposu hazır değil.");
        }

        private PresetFile Snapshot() => new PresetFile { Presets = new Dictionary<string, Recipe>(data.Presets, StringComparer.OrdinalIgnoreCase), Tags = data.Tags.ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.OrdinalIgnoreCase) };

        public void Save(string name, Recipe recipe, string tags = null)
        {
            EnsureReady();
            name = ValidName(name);
            recipe.Validate(rarityCount);
            var updated = Snapshot();
            updated.Presets[name] = Copy(recipe);
            if (tags != null) updated.Tags[name] = PresetTags.Parse(tags);
            Commit(updated);
        }

        private void Commit(PresetFile updated)
        {
            string json = JsonConvert.SerializeObject(updated, Formatting.Indented);
            if (legacyJson != null && !File.Exists(path + ".v1.bak")) AtomicFile.Write(path + ".v1.bak", legacyJson);
            AtomicFile.Write(path, json);
            data = updated;
            legacyJson = null;
            RefreshNames();
        }
        private void RefreshNames() { names = data.Presets.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(); Revision++; }

        private static Recipe Copy(Recipe recipe) => JsonConvert.DeserializeObject<Recipe>(JsonConvert.SerializeObject(recipe));
        private static string ValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Şablon adı boş olamaz.");
            return name.Trim();
        }
    }
}

