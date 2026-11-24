using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EpicLootSpawner
{
    internal static class LocalJson
    {
        public static string Read(string path)
        {
            if (!File.Exists(path)) throw new InvalidOperationException("Dosya bulunamadı: " + path);
            if (new FileInfo(path).Length > 4194304) throw new InvalidOperationException("Dosya 4 MB okuma sınırını aşıyor.");
            return File.ReadAllText(path);
        }
        public static string Stamp(string path)
        {
            if (!File.Exists(path)) return "missing";
            using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(stream));
        }
    }
    internal sealed class RecoveryCandidate
    {
        public string Target, Source, TargetStamp, Json, Kind;
        public int Kept, Skipped;
    }
    internal static class RecoveryService
    {
        public static RecoveryCandidate Inspect(string target, string source, string kind, int rarityCount, bool salvage)
        {
            string stamp = LocalJson.Stamp(target);
            string text = LocalJson.Read(source);
            var candidate = new RecoveryCandidate { Target = target, Source = source, TargetStamp = stamp, Kind = kind };
            if (kind == "Taslak")
            {
                var draft = JsonConvert.DeserializeObject<SavedDraft>(text, CollectionTransfer.Settings);
                if (draft == null) throw new InvalidOperationException("Taslak boş.");
                draft.Validate(rarityCount); candidate.Json = JsonConvert.SerializeObject(draft, Formatting.Indented); candidate.Kept = 1;
            }
            else
            {
                var root = JsonConvert.DeserializeObject<JObject>(text, CollectionTransfer.Settings);
                if (root == null || root["Version"]?.Type != JTokenType.Integer) throw new InvalidOperationException("Dosya sürümü eksik.");
                int version = (int)root["Version"];
                if (kind == "Şablonlar")
                {
                    if ((version != 1 && version != 2) || !(root["Presets"] is JObject records)) throw new InvalidOperationException("Desteklenmeyen şablon biçimi.");
                    var file = new PresetFile();
                    foreach (var pair in records.Properties())
                    {
                        try
                        {
                            string name = pair.Name.Trim();
                            var recipe = pair.Value.ToObject<Recipe>(JsonSerializer.Create(CollectionTransfer.Settings));
                            if (name.Length == 0 || recipe == null || file.Presets.ContainsKey(name)) throw new InvalidOperationException("Ad veya içerik geçersiz.");
                            recipe.Validate(rarityCount);
                            var tagToken = root["Tags"]?[pair.Name];
                            var tags = PresetTags.Normalize(tagToken?.ToObject<string[]>());
                            file.Presets.Add(name, recipe); file.Tags[name] = tags; candidate.Kept++;
                        }
                        catch { if (!salvage) throw; candidate.Skipped++; }
                    }
                    candidate.Json = JsonConvert.SerializeObject(file, Formatting.Indented);
                }
                else if (kind == "Favoriler")
                {
                    if (version != 1 || !(root["Prefabs"] is JArray values)) throw new InvalidOperationException("Desteklenmeyen favori biçimi.");
                    var names = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var token in values)
                    {
                        if (token.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)token)) names.Add((string)token);
                        else { if (!salvage) throw new InvalidOperationException("Geçersiz favori kaydı."); candidate.Skipped++; }
                    }
                    candidate.Kept = names.Count;
                    candidate.Json = JsonConvert.SerializeObject(new FavoriteFile { Prefabs = names.OrderBy(x => x, StringComparer.Ordinal).ToList() }, Formatting.Indented);
                }
                else throw new InvalidOperationException("Bilinmeyen kayıt türü.");
            }
            if (candidate.Kept == 0 && candidate.Skipped > 0) throw new InvalidOperationException("Kurtarılabilir kayıt bulunamadı.");
            if (LocalJson.Stamp(target) != stamp) throw new InvalidOperationException("Dosya inceleme sırasında değişti. Yeniden incele.");
            return candidate;
        }
        public static string Restore(RecoveryCandidate candidate)
        {
            if (candidate == null || candidate.Json == null) throw new InvalidOperationException("Önce kurtarma önizlemesi hazırla.");
            if (LocalJson.Stamp(candidate.Target) != candidate.TargetStamp) throw new InvalidOperationException("Dosya dışarıdan değişti. Yeniden incele.");
            string preserved = null;
            if (File.Exists(candidate.Target))
            {
                preserved = candidate.Target + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                File.Copy(candidate.Target, preserved);
            }
            AtomicFile.Write(candidate.Target, candidate.Json);
            return preserved;
        }
    }
}
