using System.Globalization;
using EpicLoot;

namespace EpicLootSpawner
{
    internal sealed class SocketCard
    {
        public string Prefab, Name, Title, Description, Note;

        public static SocketCard From(SocketedEffect socket, int index)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(socket?.SourcePrefab ?? "");
            string name = prefab && prefab.TryGetComponent(out ItemDrop drop)
                ? Localization.instance.Localize(API.GetItemDisplayName(drop.m_itemData)) : socket?.SourcePrefab ?? "Taş kaynağı bulunamadı";
            var card = new SocketCard { Prefab = socket?.SourcePrefab, Name = "Soket " + (index + 1) + " • " + name };
            var effect = socket?.Effect;
            if (effect == null)
            {
                card.Title = "Bu eşyada etkisiz";
                card.Description = "Taş takılıdır; mevcut eşya türüne efekt katkısı sağlamaz. Çıkardığında yine envantere iade edilir.";
            }
            else if (!string.IsNullOrEmpty(effect.EffectType) && MagicItemEffectDefinitions.AllDefinitions.TryGetValue(effect.EffectType, out var definition))
            {
                card.Title = EffectDisplay.Title(definition, effect.EffectValue);
                card.Description = EffectDisplay.Description(definition, effect.EffectValue);
                card.Note = EffectDisplay.SpawnerNote(definition, effect.EffectValue);
            }
            else
            {
                card.Title = (effect.EffectType ?? "Bilinmeyen efekt").Replace('<', '‹').Replace('>', '›') + ": " + effect.EffectValue.ToString("G9", CultureInfo.CurrentCulture);
                card.Description = "EpicLoot bu efekt için açıklama sağlamıyor.";
            }
            return card;
        }
    }
}
