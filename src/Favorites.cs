using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace EpicLootSpawner
{
    internal sealed class FavoriteFile
    {
        [JsonProperty(Required = Required.Always)] public int Version = 1;
        [JsonProperty(Required = Required.Always)] public List<string> Prefabs = new List<string>();
    }

    internal sealed class FavoriteStore
    {
        private readonly string path;
        private HashSet<string> prefabs = new HashSet<string>(StringComparer.Ordinal);
        public bool Ready { get; private set; }
        public FavoriteStore(string path) { this.path = path; }
        public bool Contains(string prefab) => prefabs.Contains(prefab ?? "");
        public void Load()
        {
            Ready = false;
            var data = File.Exists(path) ? JsonConvert.DeserializeObject<FavoriteFile>(File.ReadAllText(path), CollectionTransfer.Settings) : new FavoriteFile();
            if (data == null || data.Version != 1 || data.Prefabs == null || data.Prefabs.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("Favori dosyası geçersiz; üzerine yazılmayacak.");
            prefabs = new HashSet<string>(data.Prefabs, StringComparer.Ordinal);
            Ready = true;
        }
        public void Toggle(string prefab)
        {
            if (!Ready) throw new InvalidOperationException("Favori dosyası okunamadı. Yenile ile tekrar dene.");
            if (string.IsNullOrWhiteSpace(prefab)) throw new InvalidOperationException("Eşya kimliği boş olamaz.");
            var updated = new HashSet<string>(prefabs, StringComparer.Ordinal);
            if (!updated.Remove(prefab)) updated.Add(prefab);
            string json = JsonConvert.SerializeObject(new FavoriteFile { Prefabs = updated.OrderBy(x => x, StringComparer.Ordinal).ToList() }, Formatting.Indented);
            AtomicFile.Write(path, json);
            prefabs = updated;
        }
    }

    internal static class AtomicFile
    {
        public static void Write(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, content);
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
