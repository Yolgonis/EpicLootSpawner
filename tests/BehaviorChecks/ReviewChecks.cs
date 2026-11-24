using System;
using System.IO;
using EpicLoot;
using Newtonsoft.Json;

namespace EpicLootSpawner
{
    public partial class Plugin
    {
        private PresetStore presets;
        private string selectedPreset = "", presetName = "", presetTags = "";

        public static void CheckReview(string folder)
        {
            var plugin = new Plugin { presets = new PresetStore(Path.Combine(folder, "review-presets.json"), 6) };
            plugin.presets.Load();
            plugin.presets.Save("Original", new Recipe { Prefab = "SwordIron" }, "keep");
            plugin.presets.Save("Unavailable", new Recipe { Prefab = "MissingPrefab" }, "bad");
            plugin.LoadPreset("Original");
            Program.Assert(plugin.selectedPreset == "Original" && plugin.presetName == "Original" && plugin.presetTags == "keep", "successful preset load commits manager selection");
            plugin.inventoryKey = "KeepInventorySelection";
            string before = JsonConvert.SerializeObject(plugin.builder.Draft());
            try { plugin.LoadPreset("Unavailable"); throw new Exception("Expected rejected unavailable preset"); }
            catch (InvalidOperationException) { }
            Program.Assert(JsonConvert.SerializeObject(plugin.builder.Draft()) == before, "failed preset load keeps design");
            Program.Assert(plugin.selectedPreset == "Original" && plugin.presetName == "Original" && plugin.presetTags == "keep" && plugin.inventoryKey == "KeepInventorySelection", "failed preset load keeps record and inventory selection together");
            plugin.LoadPreset("Original");
            Program.Assert(plugin.inventoryKey == null, "successful preset load switches to item production");

            var malformed = new MagicItemEffectDefinition { Type = "MalformedText", Title = "Broken {3}", Description = "Broken {" };
            Program.Assert(EffectDisplay.Title(malformed, 10) == malformed.Type, "bad effect title falls back to identity");
            Program.Assert(EffectDisplay.Description(malformed, 10).Contains("metin biçimi geçersiz"), "bad effect description stays readable");
            plugin.available.Add(malformed);
            plugin.available.Add(new MagicItemEffectDefinition { Type = "GoodText", Title = "Good {0}", Description = "Value {0}" });
            plugin.RefreshEffectCards(); plugin.UpdateCatalogViews();
            Program.Assert(plugin.filteredEffects.Length == 2 && plugin.filteredEffects[0].Title == "Good 1", "malformed localization cannot hide the remaining effect catalog");
            malformed.Range = new MagicItemEffectDefinition.ValueDef { MinValue = float.NaN, MaxValue = 30 };
            Program.Assert(EffectDisplay.Range(malformed, ItemRarity.Epic).Contains("aralığı geçersiz"), "malformed range presented as configuration error");
            var invalidIdentity = new EpicLoot.LegendarySystem.LegendaryInfo { ID = "ReviewBrokenPowers" };
            invalidIdentity.GuaranteedMagicEffects.Add(null);
            EpicLoot.LegendarySystem.UniqueLegendaryHelper.LegendaryInfo[invalidIdentity.ID] = invalidIdentity;
            try
            {
                plugin.builder.LegendaryID = invalidIdentity.ID;
                plugin.RefreshEffectCards(); plugin.UpdateCatalogViews();
                Program.Assert(plugin.filteredEffects.Length == 2, "changed invalid identity configuration cannot crash cached effect catalog");
                Program.Assert(plugin.filteredEffects[0].Range.Contains("başlangıç güçleri"), "special configuration error replaces unsafe native range lookup in cards");
            }
            finally { EpicLoot.LegendarySystem.UniqueLegendaryHelper.LegendaryInfo.Remove(invalidIdentity.ID); }
        }
    }
}
