namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private void LoadPreset(string name)
        {
            var recipe = presets.Get(name);
            string tags = string.Join(", ", presets.GetTags(name));
            builder.Import(recipe); // The design and selected record commit only after game validation succeeds.
            selectedPreset = presetName = name;
            presetTags = tags;
            inventoryKey = null;
            RefreshEffects();
            status = "Şablon yüklendi: " + name;
        }
    }
}
