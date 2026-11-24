using System;
using System.Linq;
using EpicLoot;
using EpicLoot.LegendarySystem;
using UnityEngine;

namespace EpicLootSpawner
{
    internal sealed class SpecialItemCard
    {
        public string ID, Title, Description, Rarity, Set, Bonuses, Search;
    }

    public sealed partial class Plugin
    {
        private string specialSearch = "", specialCatalogSignature, specialCatalogError;
        private int specialFilter;
        private Vector2 specialScroll;
        private SpecialItemCard[] specialCards = Array.Empty<SpecialItemCard>();

        private void UpdateSpecialCatalog()
        {
            if (!IsOpen || picker != PickerKind.Special) return;
            string signature = (builder.Prefab ? builder.Prefab.name : "") + "|" + builder.Quality + "|" + Localization.instance.GetSelectedLanguage();
            if (signature == specialCatalogSignature) return;
            specialCatalogSignature = signature;
            specialCards = Array.Empty<SpecialItemCard>(); specialCatalogError = null;
            try
            {
                specialCards = SpecialItems.Available(builder.BaseItem()).Select(info =>
                {
                    string set = SpecialItems.Set(info);
                    string title = Localization.instance.Localize(info.Name ?? info.ID);
                    string description = Localization.instance.Localize(info.Description ?? "");
                    string bonuses = "";
                    if (!string.IsNullOrEmpty(set) && UniqueLegendaryHelper.TryGetLegendarySetInfo(set, out var definition))
                        bonuses = string.Join("\n", definition.SetBonuses.Select(bonus =>
                        {
                            if (bonus.Effect == null) return bonus.Count + " parça";
                            string effect = MagicItemEffectDefinitions.AllDefinitions.TryGetValue(bonus.Effect.Type, out var effectDefinition)
                                ? EffectDisplay.Title(effectDefinition, bonus.Effect.Values?.MinValue ?? 1) : bonus.Effect.Type;
                            return bonus.Count + " parça: " + effect;
                        }));
                    return new SpecialItemCard { ID = info.ID, Title = title, Description = description, Set = set,
                        Rarity = Localization.instance.Localize(API.GetRarityDisplayNameByIndex((int)SpecialItems.Rarity(info))),
                        Bonuses = bonuses, Search = title + " " + description + " " + info.ID + " " + SpecialItems.SetName(set) };
                }).OrderBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.ID, StringComparer.Ordinal).ToArray();
            }
            catch (Exception ex) { specialCatalogError = ex.Message; }
        }

        private const float SpecialCardWidth = 680;

        private void DrawSpecialSelector()
        {
            GUILayout.Label(inventoryKey == null ? "Kimlik seçmek mevcut efekt, taş ve adı temizler; kimliğin başlangıç güçlerini ekler."
                : "Envanter düzenlemesinde kimlik ve set üyeliği korunur. Farklı bir kimlik için Yeni eşya moduna geç.", muted);
            if (fieldErrors.TryGetValue("Identity", out string identityError)) GUILayout.Label(identityError, errorHint);
            string search = SearchInput(specialSearch, "Legendary veya set ara");
            if (search != specialSearch) QueueAction(() => specialSearch = search);
            int filter = GUILayout.SelectionGrid(specialFilter, new[] { "Tümü", "Tekil", "Set parçaları" }, 3, tabs, GUILayout.Height(34));
            if (filter != specialFilter) QueueAction(() => specialFilter = filter);
            specialScroll = GUILayout.BeginScrollView(specialScroll, GUILayout.ExpandHeight(true));
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && inventoryKey == null && builder.Prefab;
            GUILayout.BeginHorizontal();
            if (NameButton("Sıradan büyülü eşya (özel kimlik yok)", "Özel kimliği temizle; sıradan nadirlik ve efekt kurallarını kullan", !builder.IsSpecial, 0, false, SpecialCardWidth))
                QueueDesignAction(() => { builder.SelectSpecial(null); RefreshEffects(); status = "Özel kimlik kaldırıldı."; });
            GUILayout.EndHorizontal();
            GUI.enabled = enabled;
            if (specialCatalogError != null) GUILayout.Label(specialCatalogError, errorHint);
            int visible = 0;
            foreach (var entry in specialCards)
            {
                if (!Matches(entry.Search, specialSearch) || (specialFilter == 1 && !string.IsNullOrEmpty(entry.Set)) || (specialFilter == 2 && string.IsNullOrEmpty(entry.Set))) continue;
                visible++;
                GUILayout.BeginVertical(card);
                GUILayout.BeginHorizontal();
                GUI.enabled = enabled && inventoryKey == null;
                if (NameButton(entry.Title + "   •   " + entry.Rarity, "Kimlik: " + entry.ID + "\n" + entry.Description, entry.ID == builder.LegendaryID, 0, true, SpecialCardWidth))
                {
                    string id = entry.ID;
                    QueueDesignAction(() => { builder.SelectSpecial(id); RefreshEffects(); status = "Özel kimlik seçildi: " + SpecialItems.Name(id); });
                }
                GUI.enabled = enabled;
                GUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(entry.Description)) GUILayout.Label(SafeLabel(entry.Description), body);
                if (!string.IsNullOrEmpty(entry.Set))
                {
                    GUILayout.Label("Set: " + SafeLabel(SpecialItems.SetName(entry.Set)), section);
                    if (!string.IsNullOrEmpty(entry.Bonuses)) GUILayout.Label(entry.Bonuses, muted);
                }
                GUILayout.EndVertical();
            }
            if (visible == 0 && specialCatalogError == null) GUILayout.Label("Bu eşya ve filtreler için uygun özel kimlik yok.", muted);
            GUILayout.EndScrollView();
            GUILayout.Label(visible + " uygun özel kimlik  •  Set bonusları gerekli sayıda parça giyilince etkinleşir.", muted);
        }
    }
}
