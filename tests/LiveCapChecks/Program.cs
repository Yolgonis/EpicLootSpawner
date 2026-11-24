using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;

// Loads the built EpicLootSpawner.dll next to the installed EpicLoot, Harmony and game
// assemblies, applies the Limitsiz cap patches for real and calls EpicLoot's own patched
// methods with the mode off and on. Nothing is installed or modified.
internal static class Program
{
    private static int checks, failures;

    private static void Check(bool condition, string label)
    {
        checks++;
        if (!condition) { failures++; Console.WriteLine("FAIL: " + label); }
    }

    private static int Main(string[] args)
    {
        string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../.."));
        string game = Environment.GetEnvironmentVariable("GamePath") ?? @"C:\Program Files (x86)\Steam\steamapps\common\Valheim";
        string profile = Environment.GetEnvironmentVariable("ProfilePath")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"com.kesomannen.gale\valheim\profiles\Sini");
        // dotnet run forwards unknown switches such as --nologo; only a .dll argument overrides.
        string spawnerDll = args.FirstOrDefault(a => a.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            ?? Path.Combine(root, @"bin\Release\net481\EpicLootSpawner.dll");
        var folders = new[] { Path.GetDirectoryName(spawnerDll), Path.Combine(game, @"valheim_Data\Managed"), Path.Combine(profile, @"BepInEx\core") }
            .Concat(Directory.GetDirectories(Path.Combine(profile, @"BepInEx\plugins"), "*", SearchOption.AllDirectories)).ToArray();
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
        {
            string file = new AssemblyName(e.Name).Name + ".dll";
            foreach (string folder in folders)
                if (File.Exists(Path.Combine(folder, file))) return Assembly.LoadFrom(Path.Combine(folder, file));
            return null;
        };

        var spawner = Assembly.LoadFrom(spawnerDll);
        var caps = spawner.GetType("EpicLootSpawner.EffectCaps", true);
        var epic = AppDomain.CurrentDomain.Load("EpicLoot");
        var harmonyType = AppDomain.CurrentDomain.Load("0Harmony").GetType("HarmonyLib.Harmony", true);
        var harmony = Activator.CreateInstance(harmonyType, "live.effect.caps");
        var lifted = caps.GetField("Lifted", BindingFlags.Static | BindingFlags.NonPublic);
        try
        {
            caps.GetMethod("Apply", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new object[] { harmony, new Action<string>(m => Console.WriteLine("WARN: " + m)) });
            var active = (IList)caps.GetField("Active", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var missing = (IList)caps.GetField("Missing", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            int total = (int)caps.GetProperty("Total", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            // .NET Framework cannot JIT methods that touch Valheim types (Unity-only IL); the game's
            // Mono runtime can. Those targets are checked statically below instead. Since EpicLoot
            // 0.15 the mead cooldown clamp lives in ModifyRestoringMead(Player, SE_Stats), so five
            // targets (resistance, Bulk Up, Off-Set, night, mead) are static-only here.
            Check(active.Count + missing.Count == total && active.Count >= 9, $"cap patches bind outside the game ({active.Count}/{total})");
            CheckTargetsStatically(caps, epic);

            object Call(string type, string method, params object[] values)
                => epic.GetType(type, true).GetMethod(method, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, values);
            void Pair(string label, string type, string method, object off, object on, params object[] values)
            {
                lifted.SetValue(null, false);
                object a = Call(type, method, values);
                lifted.SetValue(null, true);
                object b = Call(type, method, values);
                lifted.SetValue(null, false);
                Check(Same(a, off), label + " off: expected " + off + ", got " + a);
                Check(Same(b, on), label + " on: expected " + on + ", got " + b);
            }

            const string effects = "EpicLoot.MagicItemEffects.", shards = "EpicLoot.MagicItemEffects.Shards.";
            Pair("slow floor", effects + "Slow", "ClampMultiplier", 0.1f, 0.02f, 0.02f);
            Pair("parry window", effects + "ModifyParryWindow", "GetBlockTimer", 0.1f, 0.2f, 0.1f, -100f);
            Pair("mercenary soft cap", shards + "Mercenary", "ApplySoftCap", 30f + 70f * (1f - (float)Math.Exp(-1)), 100f, 100f, 30f, 70f);
            Pair("lucky loot multiplier", shards + "LuckyLoot", "Multiplier", 10, 51, 100f);
            Pair("lucky loot units per entry", shards + "LuckyLoot", "GetMaxUnitsPerEntry", 100, 10000);
            Pair("lucky loot units per death", shards + "LuckyLoot", "GetMaxAddedUnitsPerDeath", 300, 10000);
            Pair("shocking charge minimum", shards + "EikthyrShockingCharge", "GetChargesNeeded", 6, 3, 50f);
            Pair("adrenaline efficiency", shards + "UseAdrenalineAsStamina", "ClampEfficiency", 1f, 2.5f, 2.5f);
            Pair("blood cost reduction cap", shards + "KillsReduceNextBloodCost", "GetMaxReduction", 1f, 1f);
        }
        finally
        {
            try { harmonyType.GetMethod("UnpatchSelf").Invoke(harmony, null); }
            catch (TargetInvocationException) { } // Same .NET Framework JIT limit as above.
        }
        Console.WriteLine(failures == 0 ? $"PASS: {checks} live EpicLoot cap checks" : $"FAILED: {failures}/{checks}");
        return failures == 0 ? 0 : 1;
    }

    // Every target exists, prefix/postfix parameters match EpicLoot's by name and type, and
    // every Clamp01 swap target really calls Mathf.Clamp01. Read with Cecil: no type loads.
    private static void CheckTargetsStatically(Type caps, Assembly epic)
    {
        const BindingFlags any = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var epicModule = Mono.Cecil.ModuleDefinition.ReadModule(epic.Location);
        var capsType = Mono.Cecil.ModuleDefinition.ReadModule(caps.Assembly.Location).GetType(caps.FullName);
        foreach (var target in (Array)caps.GetField("Targets", any).GetValue(null))
        {
            string Field(string name) => (string)target.GetType().GetField(name).GetValue(target);
            bool Flag(string name) => (bool)target.GetType().GetField(name).GetValue(target);
            string label = Field("Name");
            var methods = epicModule.GetType(Field("Type"))?.Methods.Where(m => m.Name == Field("Method")).ToArray();
            Check(methods != null && methods.Length == 1, "target exists once: " + label);
            if (methods == null || methods.Length != 1) continue;
            var method = methods[0];
            var patch = capsType.Methods.Single(m => m.Name == Field("Patch"));
            if (Flag("Swap"))
            {
                int calls = method.Body.Instructions.Count(i => i.OpCode.Code == Mono.Cecil.Cil.Code.Call
                    && i.Operand is Mono.Cecil.MethodReference r && r.FullName == "System.Single UnityEngine.Mathf::Clamp01(System.Single)");
                Check(calls > 0, "swap target calls Mathf.Clamp01: " + label);
                Check(patch.ReturnType.FullName == "System.Single" && patch.Parameters.Count == 1 && patch.Parameters[0].ParameterType.FullName == "System.Single", "swap helper matches Clamp01: " + label);
                continue;
            }
            foreach (var p in patch.Parameters)
            {
                if (p.Name == "__result")
                {
                    Check(p.ParameterType.GetElementType().FullName == method.ReturnType.FullName, "result type matches: " + label);
                    continue;
                }
                var match = method.Parameters.SingleOrDefault(o => o.Name == p.Name);
                Check(match != null && match.ParameterType.FullName == p.ParameterType.FullName, "parameter " + p.Name + " matches: " + label);
            }
            Check(Flag("Postfix") || patch.ReturnType.FullName == "System.Boolean", "prefix can skip EpicLoot's clamp: " + label);
        }
    }

    private static bool Same(object a, object b)
        => a is float x && b is float y ? Math.Abs(x - y) < 0.0001f : Equals(a, b);
}
