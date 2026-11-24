using System;
using System.Linq;

namespace EpicLootSpawner
{
    internal sealed class PresetQuery
    {
        private int revision = -1;
        private string search, tags;
        private bool ready;
        private PresetStore owner;
        private string[] result = Array.Empty<string>();
        public string[] Get(PresetStore store, string nameSearch, string tagSearch)
        {
            nameSearch = nameSearch ?? ""; tagSearch = tagSearch ?? "";
            if (ReferenceEquals(owner, store) && revision == store.Revision && search == nameSearch && tags == tagSearch && ready == store.Ready) return result;
            owner = store; revision = store.Revision; search = nameSearch; tags = tagSearch; ready = store.Ready;
            var required = tags.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
            result = !ready ? Array.Empty<string>() : store.Names.Where(name =>
            {
                var assigned = store.GetTags(name);
                return (name + " " + store.Get(name).Prefab + " " + string.Join(" ", assigned)).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                    && required.All(tag => assigned.Contains(tag, StringComparer.OrdinalIgnoreCase));
            }).ToArray();
            return result;
        }
    }
}
