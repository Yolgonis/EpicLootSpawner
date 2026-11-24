using System;
using System.IO;
using Newtonsoft.Json;

namespace EpicLootSpawner
{
    internal sealed class SavedDraft
    {
        [JsonProperty(Required = Required.Always)] public int Version = 1;
        [JsonProperty(Required = Required.Always)] public Recipe Recipe;
        public bool ToInventory = true;
        public int StoneRarity = 2;

        public void Validate(int rarityCount)
        {
            if (Version != 1 || Recipe == null || Recipe.Rarity < 0 || Recipe.Rarity >= rarityCount || StoneRarity < 0 || StoneRarity >= rarityCount)
                throw new InvalidOperationException("Son taslak dosyasının biçimi veya sürümü desteklenmiyor.");
            // Keep unfinished input intact; game rules will be checked by the live preview.
            if (Recipe.Quality == null || Recipe.Count == null || Recipe.SocketCount == null || Recipe.Name == null || Recipe.Effects == null || Recipe.Stones == null)
                throw new InvalidOperationException("Son taslak dosyasında eksik alan var.");
            foreach (var effect in Recipe.Effects)
                if (effect == null || string.IsNullOrWhiteSpace(effect.Type) || effect.Value == null) throw new InvalidOperationException("Son taslakta geçersiz efekt var.");
            foreach (var stone in Recipe.Stones)
                if (string.IsNullOrWhiteSpace(stone)) throw new InvalidOperationException("Son taslakta geçersiz taş var.");
        }
    }

    internal sealed class DraftStore
    {
        private readonly string path;
        private readonly int rarityCount;
        private string savedJson;
        public bool Ready { get; private set; }
        public DraftStore(string path, int rarityCount) { this.path = path; this.rarityCount = rarityCount; }
        public SavedDraft Load()
        {
            Ready = false;
            if (!File.Exists(path)) { savedJson = null; Ready = true; return null; }
            var draft = JsonConvert.DeserializeObject<SavedDraft>(File.ReadAllText(path), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, MaxDepth = 32 });
            if (draft == null) throw new InvalidOperationException("Son taslak dosyası boş veya geçersiz.");
            draft.Validate(rarityCount);
            savedJson = JsonConvert.SerializeObject(draft, Formatting.Indented);
            Ready = true;
            return draft;
        }
        public void Save(SavedDraft draft)
        {
            if (!Ready) throw new InvalidOperationException("Son taslak dosyası okunamadı; üzerine yazılmayacak.");
            draft.Validate(rarityCount);
            string json = JsonConvert.SerializeObject(draft, Formatting.Indented);
            if (json == savedJson) return;
            AtomicFile.Write(path, json);
            savedJson = json;
        }
    }
}
