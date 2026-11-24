using System;
using System.Collections.Generic;

namespace EpicLootSpawner
{
    internal sealed class FieldProblem : InvalidOperationException
    {
        public readonly string Key;
        public FieldProblem(string key, string message) : base(message) { Key = key; }
    }
    internal sealed class EffectRule
    {
        public float Min = 1, Max = 1;
        public bool WholeWood;
        // Unlimited mode accepts any finite value outside EpicLoot's roll range.
        public bool Bounded = true;
    }
    internal static class FieldValidation
    {
        public static T At<T>(string key, Func<T> action)
        {
            try { return action(); }
            catch (FieldProblem) { throw; }
            catch (InvalidOperationException ex) { throw new FieldProblem(key, ex.Message); }
        }
        public static Dictionary<string, string> Check(Recipe recipe, int maxEffects, int maxSockets, IDictionary<string, EffectRule> effectRules)
        {
            var errors = new Dictionary<string, string>();
            CheckValue(errors, "Quality", () => Rules.Whole(recipe.Quality, 1, "Seviye"));
            CheckValue(errors, "Count", () => Rules.Bounded(recipe.Count, 1, 100, "Adet"));
            CheckValue(errors, "SocketCount", () => Rules.Bounded(recipe.SocketCount, 0, maxSockets, "Soket"));
            if ((recipe.Name ?? "").Length > 80) errors["Name"] = "Özel ad en fazla 80 karakter olmalı.";
            if (recipe.Effects.Count > maxEffects) errors["Effects"] = "Bu nadirlikte en fazla " + maxEffects + " efekt olabilir. Daha fazlası için Limitsiz modu aç.";
            if (int.TryParse(recipe.SocketCount, out int sockets) && recipe.Stones.Count > sockets)
                errors["SocketCount"] = "Seçilen " + recipe.Stones.Count + " taş için yeterli soket yok.";
            for (int i = 0; i < recipe.Effects.Count; i++)
            {
                var effect = recipe.Effects[i];
                if (!effectRules.TryGetValue(effect.Type, out var rule)) { errors["Effect." + i] = "Efekt tanımı bulunamadı: " + effect.Type; continue; }
                CheckValue(errors, "Effect." + i, () =>
                {
                    float value = rule.Bounded ? Rules.EffectValue(effect.Value, rule.Min, rule.Max) : Rules.EffectValue(effect.Value, null, null);
                    if (rule.WholeWood) Rules.ExtraWoodCount(value);
                    return value;
                });
            }
            return errors;
        }
        private static void CheckValue<T>(Dictionary<string, string> errors, string key, Func<T> action)
        {
            try { action(); } catch (InvalidOperationException ex) { errors[key] = ex.Message; }
        }
    }
}
