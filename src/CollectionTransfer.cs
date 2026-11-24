using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EpicLootSpawner
{
    internal static class PresetTags
    {
        public static string[] Parse(string text) => Normalize((text ?? "").Split(','));
        public static string[] Normalize(IEnumerable<string> tags)
        {
            var result = (tags ?? Array.Empty<string>()).Select(x => (x ?? "").Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (result.Length > 8 || result.Any(x => x.Length > 24 || x.Contains(',') || x.Any(char.IsControl)))
                throw new InvalidOperationException("En fazla 8 etiket; her biri 24 karakter olmalı ve virgül içermemeli.");
            return result;
        }
    }
    internal sealed class CollectionEntry
    {
        public string Name;
        public Recipe Recipe;
        public string[] Tags = Array.Empty<string>();
    }
    internal sealed class ImportRow
    {
        public CollectionEntry Entry;
        public string Error;
        public bool Selected, Conflict;
    }
    internal sealed class CollectionFile
    {
        public string Format = "EpicLootSpawner.Collection";
        public int Version = 1;
        public List<CollectionEntry> Entries = new List<CollectionEntry>();
    }
    internal static class CollectionTransfer
    {
        public const int MaxLength = 1048576, MaxEntries = 500;
        internal static readonly JsonSerializerSettings Settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, MetadataPropertyHandling = MetadataPropertyHandling.Ignore, MaxDepth = 32 };
        public static string Encode(IEnumerable<CollectionEntry> entries, int rarityCount)
        {
            var file = new CollectionFile { Entries = entries.ToList() };
            if (file.Entries.Count == 0 || file.Entries.Count > MaxEntries) throw new InvalidOperationException("1–500 kayıt seç.");
            foreach (var entry in file.Entries) { Validate(entry, rarityCount); }
            if (file.Entries.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != file.Entries.Count) throw new InvalidOperationException("Yinelenen kayıt adı.");
            string json = JsonConvert.SerializeObject(file, Formatting.Indented, Settings);
            if (json.Length > MaxLength) throw new InvalidOperationException("Koleksiyon dosyası 1 MB sınırını aşıyor.");
            return json;
        }
        public static List<ImportRow> Decode(string text, int rarityCount)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > MaxLength) throw new InvalidOperationException("Koleksiyon metni boş veya 1 MB sınırını aşıyor.");
            JObject root;
            try { root = JsonConvert.DeserializeObject<JObject>(text, Settings); }
            catch (JsonException ex) { throw new InvalidOperationException("Geçersiz koleksiyon JSON: " + ex.Message); }
            if (root == null || (string)root["Format"] != "EpicLootSpawner.Collection" || root["Version"]?.Type != JTokenType.Integer || (int)root["Version"] != 1 || !(root["Entries"] is JArray array))
                throw new InvalidOperationException("Desteklenmeyen koleksiyon biçimi/sürümü.");
            if (array.Count == 0 || array.Count > MaxEntries) throw new InvalidOperationException("Koleksiyon 1–500 kayıt içermeli.");
            var result = new List<ImportRow>();
            foreach (var token in array)
            {
                var row = new ImportRow();
                try
                {
                    row.Entry = token.ToObject<CollectionEntry>(JsonSerializer.Create(Settings)); Validate(row.Entry, rarityCount, false);
                    row.Selected = true;
                }
                catch (Exception ex) { row.Error = ex.Message; }
                result.Add(row);
            }
            return result;
        }
        public static void RefreshRows(IList<ImportRow> rows, PresetStore store, IDictionary<ImportRow, string> baseErrors)
        {
            var duplicates = new HashSet<string>(rows.Where(x => x.Entry != null && !string.IsNullOrWhiteSpace(x.Entry.Name))
                .GroupBy(x => x.Entry.Name.Trim(), StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1).Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                row.Error = baseErrors.TryGetValue(row, out string error) ? error : row.Error; row.Conflict = false;
                if (row.Error == null)
                {
                    string name = row.Entry?.Name?.Trim();
                    if (string.IsNullOrWhiteSpace(name)) row.Error = "Kayıt adı boş olamaz.";
                    else if (duplicates.Contains(name)) { row.Error = "Dosyada yinelenen ad; farklı ad yaz."; row.Conflict = true; }
                    else if (store.Contains(name)) { row.Error = "Bu ad depoda var; farklı ad yaz."; row.Conflict = true; }
                }
                if (row.Error != null) row.Selected = false;
            }
        }
        private static void Validate(CollectionEntry entry, int rarityCount, bool requireName = true)
        {
            if (entry == null || entry.Recipe == null || (requireName && string.IsNullOrWhiteSpace(entry.Name))) throw new InvalidOperationException("Kayıt adı veya tasarım eksik.");
            entry.Name = entry.Name?.Trim(); entry.Recipe.Validate(rarityCount); entry.Tags = PresetTags.Normalize(entry.Tags);
        }
    }
}
