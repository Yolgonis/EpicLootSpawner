using System;
using Newtonsoft.Json;

namespace EpicLootSpawner
{
    internal sealed class SharedRecipe
    {
        [JsonProperty(Required = Required.Always)] public string Format = "EpicLootSpawner.Recipe";
        [JsonProperty(Required = Required.Always)] public int Version = 1;
        [JsonProperty(Required = Required.Always)] public Recipe Recipe;
    }

    internal static class RecipeTransfer
    {
        public const int MaxLength = 65536;
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None, MetadataPropertyHandling = MetadataPropertyHandling.Ignore, MaxDepth = 32
        };
        public static string Encode(Recipe recipe, int rarityCount)
        {
            recipe.Validate(rarityCount);
            string json = JsonConvert.SerializeObject(new SharedRecipe { Recipe = recipe }, Formatting.Indented, Settings);
            if (json.Length > MaxLength) throw new InvalidOperationException("Paylaşılacak tasarım çok büyük (en fazla 65536 karakter).");
            return json;
        }
        public static Recipe Decode(string text, int rarityCount)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("Önce şablon metnini yapıştır.");
            if (text.Length > MaxLength) throw new InvalidOperationException("Şablon metni çok büyük (en fazla 65536 karakter).");
            try
            {
                var shared = JsonConvert.DeserializeObject<SharedRecipe>(text, Settings);
                if (shared == null || shared.Format != "EpicLootSpawner.Recipe" || shared.Version != 1 || shared.Recipe == null)
                    throw new InvalidOperationException("Bu metin desteklenen bir EpicLoot Spawner paylaşım şablonu değil.");
                shared.Recipe.Validate(rarityCount);
                return shared.Recipe;
            }
            catch (JsonException ex) { throw new InvalidOperationException("Şablon metni geçerli JSON değil: " + ex.Message); }
        }
    }
}
