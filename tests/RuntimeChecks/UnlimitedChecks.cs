using System;
using System.Linq;
using EpicLoot;
using EpicLootSpawner;

public static partial class Program
{
    // Unlimited mode lifts the roll range, effect count and socket count limits while
    // EpicLoot's compatibility, duplicate and non-finite value rules still apply.
    private static void CheckUnlimited()
    {
        for (int i = 0; i < 4; i++)
            MagicItemEffectDefinitions.AllDefinitions["Extra" + i] = new MagicItemEffectDefinition { Type = "Extra" + i, Values = new MagicItemEffectDefinition.ValueDef() };
        try
        {
            var free = new Spawner(); free.Import(Recipe());
            free.Effects.Add(new EffectChoice { Type = "Damage", Value = "500" });
            Reject(() => free.Build(), "bounded mode rejects a value above the roll range");
            Check(free.FieldErrors()["Effect.0"].Contains("Limitsiz"), "range error points to unlimited mode");
            free.Unlimited = true;
            Check(!free.FieldErrors().ContainsKey("Effect.0"), "unlimited mode reports no range error");
            Check(Math.Abs(free.Build().Magic.Effects[0].EffectValue - 500) < 0.001f, "unlimited mode writes a value above the roll range");
            free.Effects[0].Value = "-5";
            Check(Math.Abs(free.Build().Magic.Effects[0].EffectValue + 5) < 0.001f, "unlimited mode writes a value below the roll range");
            foreach (string bad in new[] { "NaN", "Infinity", "", "abc" })
            {
                free.Effects[0].Value = bad;
                Reject(() => free.Build(), "unlimited mode still rejects non-finite value: " + bad);
                Check(free.FieldErrors().ContainsKey("Effect.0"), "unlimited mode reports non-finite value: " + bad);
            }
            free.Effects[0].Value = "10";

            for (int i = 0; i < 4; i++) free.Effects.Add(new EffectChoice { Type = "Extra" + i, Value = "5" });
            Check(free.Effects.Count == 5 && free.EffectLimit == 3 && free.MaxEffects == Spawner.UnlimitedEffects, "effect limits expose normal and unlimited values");
            Check(!free.FieldErrors().ContainsKey("Effects") && free.Build().Magic.Effects.Count == 5, "unlimited mode builds more effects than the rarity allows");
            Check(free.Available().Any(), "unlimited mode keeps offering compatible effects past the normal count");
            free.Unlimited = false;
            Check(free.FieldErrors().ContainsKey("Effects") && free.FieldErrors()["Effects"].Contains("Limitsiz"), "bounded mode flags extra effects and points to unlimited mode");
            Reject(() => free.Build(), "bounded mode rejects extra effects");
            Check(!free.Available().Any(), "bounded mode offers nothing once the normal count is full");

            free.Unlimited = true;
            free.Effects.Add(new EffectChoice { Type = "Incompatible", Value = "1" });
            Reject(() => free.Build(), "unlimited mode keeps item type compatibility");
            Check(free.FieldErrors().ContainsKey("Effect.5"), "incompatible effect is reported in unlimited mode");
            free.Effects.RemoveAt(5);
            free.Effects.Add(new EffectChoice { Type = "Damage", Value = "3" });
            Reject(() => free.Build(), "unlimited mode keeps the duplicate effect rule");
            free.Effects.RemoveAt(5);

            free.Effects.Clear(); free.SocketCount = "5";
            free.Unlimited = false;
            Check(free.FieldErrors().ContainsKey("SocketCount"), "bounded mode rejects sockets above the rarity maximum");
            free.Unlimited = true;
            Check(!free.FieldErrors().ContainsKey("SocketCount") && free.Build().Magic.SocketCount == 5, "unlimited mode builds extra sockets");
            free.Stones.Add("Stone");
            Check(free.Build().Magic.Sockets.Count == 1, "stones still socket into extra sockets");
            free.SocketCount = (Spawner.UnlimitedSockets + 1).ToString();
            Check(free.FieldErrors().ContainsKey("SocketCount"), "unlimited sockets keep a hard upper bound");
        }
        finally
        {
            for (int i = 0; i < 4; i++) MagicItemEffectDefinitions.AllDefinitions.Remove("Extra" + i);
        }
    }
}
