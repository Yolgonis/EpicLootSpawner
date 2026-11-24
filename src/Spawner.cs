using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EpicLoot;
using EpicLoot.ShardStones;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicLootSpawner
{
    internal sealed class Spawner
    {
        public readonly List<EffectChoice> Effects = new List<EffectChoice>();
        public readonly List<string> Stones = new List<string>();
        public GameObject Prefab;
        public ItemRarity Rarity = ItemRarity.Epic;
        public string Quality = "1", Count = "1", SocketCount = "0", Name = "";
        public string LegendaryID, SetID;
        public bool IsSpecial => SpecialItems.HasIdentity(LegendaryID, SetID);
        // Lifts EpicLoot's roll range, effect count and socket count limits. Compatibility,
        // duplicates and special-identity powers are still checked by EpicLoot's own rules.
        public bool Unlimited;
        public const int UnlimitedEffects = 30, UnlimitedSockets = 12;

        public Recipe Draft()
        {
            var recipe = new Recipe
            {
                Prefab = Prefab ? Prefab.name : null, Rarity = (int)Rarity,
                LegendaryID = LegendaryID, SetID = SetID,
                Quality = Quality, Count = Count, SocketCount = SocketCount, Name = Name,
                Effects = Effects.Select(x => new EffectChoice { Type = x.Type, Value = x.Value }).ToList(),
                Stones = Stones.ToList()
            };
            return recipe;
        }

        public Recipe Export()
        {
            var recipe = Draft();
            recipe.Validate(Enum.GetValues(typeof(ItemRarity)).Length);
            return recipe;
        }

        public void Import(Recipe recipe)
        {
            recipe.Validate(Enum.GetValues(typeof(ItemRarity)).Length);
            var prefab = ObjectDB.instance.GetItemPrefab(recipe.Prefab);
            if (!prefab) throw new InvalidOperationException("Şablondaki eşya bulunamadı: " + recipe.Prefab);
            var candidate = new Spawner
            {
                Prefab = prefab, Rarity = (ItemRarity)recipe.Rarity, Quality = recipe.Quality,
                LegendaryID = recipe.LegendaryID, SetID = recipe.SetID,
                Count = recipe.Count, SocketCount = recipe.SocketCount, Name = recipe.Name
            };
            candidate.Effects.AddRange(recipe.Effects.Select(x => new EffectChoice { Type = x.Type, Value = x.Value }));
            candidate.Stones.AddRange(recipe.Stones);
            _ = candidate.RequestedCount;
            candidate.Build(); // Keep the current recipe if game validation fails.
            RestoreDraft(candidate.Draft());
        }

        public void RestoreDraft(Recipe recipe)
        {
            if (recipe == null) throw new InvalidOperationException("Tasarım geçmişi geçersiz.");
            var prefab = string.IsNullOrEmpty(recipe.Prefab) ? null : ObjectDB.instance.GetItemPrefab(recipe.Prefab);
            if (!string.IsNullOrEmpty(recipe.Prefab) && !prefab) throw new InvalidOperationException("Eşya bulunamadı: " + recipe.Prefab);
            if (recipe.Effects == null || recipe.Stones == null || recipe.Rarity < 0 || recipe.Rarity >= Enum.GetValues(typeof(ItemRarity)).Length
                || recipe.Quality == null || recipe.Count == null || recipe.SocketCount == null || recipe.Name == null
                || recipe.Effects.Any(x => x == null || string.IsNullOrWhiteSpace(x.Type) || x.Value == null)
                || recipe.Stones.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("Tasarım geçmişi geçersiz.");
            var effects = recipe.Effects.Select(x => new EffectChoice { Type = x.Type, Value = x.Value }).ToArray();
            var stones = recipe.Stones.ToArray();
            Prefab = prefab; Rarity = (ItemRarity)recipe.Rarity; Quality = recipe.Quality;
            LegendaryID = recipe.LegendaryID; SetID = recipe.SetID;
            Count = recipe.Count; SocketCount = recipe.SocketCount; Name = recipe.Name;
            Effects.Clear(); Effects.AddRange(effects);
            Stones.Clear(); Stones.AddRange(stones);
        }

        public ItemDrop.ItemData BaseItem()
        {
            if (!Prefab || !Prefab.TryGetComponent(out ItemDrop drop))
                throw new InvalidOperationException("Önce bir eşya seç.");
            var item = ItemCopies.Clone(drop.m_itemData);
            // SharedData must be private to this item: EpicLoot can change durability flags.
            var cloneShared = typeof(ItemDrop.ItemData.SharedData).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            item.m_shared = (ItemDrop.ItemData.SharedData)cloneShared.Invoke(drop.m_itemData.m_shared, null);
            item.m_customData = new Dictionary<string, string>(drop.m_itemData.m_customData);
            item.m_dropPrefab = Prefab;
            item.m_stack = 1;
            // Native loot has no crafter; clear inherited prefab metadata rather than hiding its tooltip.
            item.m_crafterID = 0L;
            item.m_crafterName = "";
            item.m_quality = FieldValidation.At("Quality", () => Rules.Whole(Quality, 1, "Eşya seviyesi"));
            return item;
        }

        public MagicItem MagicShell() => new MagicItem { Rarity = Rarity, LegendaryID = LegendaryID, SetID = SetID,
            DisplayName = string.IsNullOrWhiteSpace(Name) ? (IsSpecial ? SpecialItems.Find(LegendaryID)?.Name : null) : Name.Trim() };

        public MagicItemEffectDefinition.ValueDef EffectRange(MagicItemEffectDefinition definition)
            => SpecialItems.RollRange(definition, Rarity, LegendaryID);
        public bool IsRequired(string type) => IsSpecial && SpecialItems.Required(SpecialItems.Find(LegendaryID)).Any(x => x?.Type == type);
        public bool IsProtected(string type) => IsRequired(type) && MagicItemEffectDefinitions.AllDefinitions.TryGetValue(type, out var definition) && !definition.CanBeAugmented;

        public void SelectSpecial(string id)
        {
            var info = SpecialItems.Resolve(id);
            if (info != null) SpecialItems.Check(BaseItem(), new MagicItem { Rarity = SpecialItems.Rarity(info), LegendaryID = info.ID, SetID = SpecialItems.Set(info) });
            var draft = Draft();
            draft.LegendaryID = info?.ID; draft.SetID = info == null ? null : SpecialItems.Set(info);
            draft.Rarity = info == null ? (int)Rarity : (int)SpecialItems.Rarity(info);
            draft.Name = ""; draft.SocketCount = "0"; draft.Stones.Clear(); draft.Effects.Clear();
            foreach (var effect in SpecialItems.Required(info))
            {
                if (!MagicItemEffectDefinitions.AllDefinitions.TryGetValue(effect.Type, out var definition))
                    throw new InvalidOperationException("Özel eşyanın efekt tanımı eksik: " + effect.Type);
                var range = MagicItemEffectDefinitions.GetRollRange(definition, (ItemRarity)draft.Rarity, info.ID);
                draft.Effects.Add(new EffectChoice { Type = effect.Type, Value = (range?.MinValue ?? 1).ToString("G9", CultureInfo.InvariantCulture) });
            }
            Import(draft); // Build first; a missing/incompatible definition never erases the design.
        }

        public List<MagicItemEffectDefinition> Available()
        {
            if (Effects.Count >= MaxEffects) return new List<MagicItemEffectDefinition>();
            var item = BaseItem();
            var magic = MagicShell();
            SpecialItems.Check(item, magic);
            foreach (var choice in Effects) magic.Effects.Add(new MagicItemEffect(choice.Type));
            return MagicItemEffectDefinitions.GetAvailableEffects(item, magic).OrderBy(x => x.Type).ToList();
        }

        public int EffectLimit
        {
            get
            {
                int normal = LootRoller.GetEffectCountsPerRarity(Rarity, true).Select(x => x.Key).DefaultIfEmpty(0).Max();
                var info = IsSpecial ? SpecialItems.Find(LegendaryID) : null;
                return Math.Max(info?.GuaranteedEffectCount > 0 ? info.GuaranteedEffectCount : normal, SpecialItems.Required(info).Count());
            }
        }
        public int SocketLimit => LootRoller.GetMaxSocketCountForRarity(Rarity);
        public int MaxEffects => Unlimited ? Math.Max(UnlimitedEffects, EffectLimit) : EffectLimit;
        public int MaxSockets => Unlimited ? Math.Max(UnlimitedSockets, SocketLimit) : SocketLimit;

        public Dictionary<string, string> FieldErrors()
        {
            var rules = new Dictionary<string, EffectRule>();
            try { SpecialItems.ValidatePowers(SpecialItems.Find(LegendaryID)); }
            catch (InvalidOperationException ex)
            {
                var invalid = FieldValidation.Check(Draft(), MaxEffects, MaxSockets, rules);
                invalid["Identity"] = ex.Message;
                return invalid;
            }
            foreach (var effect in Effects)
            {
                if (!MagicItemEffectDefinitions.AllDefinitions.TryGetValue(effect.Type, out var definition)) continue;
                var range = EffectRange(definition);
                rules[effect.Type] = new EffectRule { Min = range?.MinValue ?? 1, Max = range?.MaxValue ?? 1, WholeWood = effect.Type == MagicEffectType.IncreaseTreeDrop, Bounded = !Unlimited };
            }
            var errors = FieldValidation.Check(Draft(), MaxEffects, MaxSockets, rules);
            foreach (var required in SpecialItems.Required(SpecialItems.Find(LegendaryID)))
            {
                if (required == null) continue; // Identity validation reports malformed native definitions.
                if ((IsProtected(required.Type) && Effects.Count(x => x.Type == required.Type) != 1) || Effects.Count(x => x.Type == required.Type) > 1)
                    errors["Effects"] = "Özel eşyanın sabit gücü eksik veya yinelenmiş: " + required.Type;
            }
            if (Prefab && !errors.ContainsKey("Quality"))
            {
                var item = BaseItem(); var magic = MagicShell();
                try { SpecialItems.Check(item, magic); } catch (InvalidOperationException ex) { errors["Identity"] = ex.Message; }
                foreach (var choice in Effects.Where(x => IsRequired(x.Type))) magic.Effects.Add(new MagicItemEffect(choice.Type));
                for (int i = 0; i < Effects.Count; i++)
                {
                    var choice = Effects[i];
                    if (IsRequired(choice.Type)) continue;
                    if (!MagicItemEffectDefinitions.GetAvailableEffects(item, magic).Any(x => x.Type == choice.Type))
                        errors["Effect." + i] = "Bu eşya/nadirlikte uyumsuz veya mevcut efektle çakışıyor.";
                    magic.Effects.Add(new MagicItemEffect(choice.Type));
                }
            }
            return errors;
        }

        public ItemDrop.ItemData Build()
        {
            if ((Name ?? "").Length > 80) throw new FieldProblem("Name", "Eşya adı en fazla 80 karakter olmalı.");
            var item = BaseItem();
            if (!API.CanBeMagicItem(item)) throw new InvalidOperationException("Bu eşya büyülenemez.");
            var magic = MagicShell();
            var info = FieldValidation.At("Identity", () => SpecialItems.Check(item, magic));
            foreach (var required in SpecialItems.Required(info))
                if ((IsProtected(required.Type) && Effects.Count(x => x.Type == required.Type) != 1) || Effects.Count(x => x.Type == required.Type) > 1)
                    throw new FieldProblem("Effects", "Özel eşyanın sabit gücü eksik veya yinelenmiş: " + required.Type);
            magic.SocketCount = FieldValidation.At("SocketCount", () => Rules.Bounded(SocketCount, 0, MaxSockets, "Soket sayısı"));
            if (Stones.Count > magic.SocketCount) throw new FieldProblem("SocketCount", "Seçilen taşlar için yeterli soket yok.");
            if (Effects.Count > MaxEffects) throw new FieldProblem("Effects", "Bu nadirlik için efekt sayısı sınırı aşıldı.");
            // Native loot rolls insert guaranteed powers without ordinary roll restrictions.
            // Check optional powers against all guaranteed ones, independent of list order.
            var compatibility = MagicShell();
            foreach (var choice in Effects.Where(x => IsRequired(x.Type))) compatibility.Effects.Add(new MagicItemEffect(choice.Type));
            for (int i = 0; i < Effects.Count; i++)
            {
                var choice = Effects[i];
                MagicItemEffectDefinition definition;
                if (IsRequired(choice.Type)) MagicItemEffectDefinitions.AllDefinitions.TryGetValue(choice.Type, out definition);
                else definition = MagicItemEffectDefinitions.GetAvailableEffects(item, compatibility).FirstOrDefault(x => x.Type == choice.Type);
                if (definition == null) throw new FieldProblem("Effect." + i, "Eşya/nadirlik için uyumsuz veya çakışan efekt: " + choice.Type);
                var range = EffectRange(definition);
                float value = FieldValidation.At("Effect." + i, () =>
                {
                    float parsed = Unlimited ? Rules.EffectValue(choice.Value, null, null)
                        : range == null ? Rules.EffectValue(choice.Value, 1, 1) : Rules.EffectValue(choice.Value, range.MinValue, range.MaxValue);
                    if (choice.Type == MagicEffectType.IncreaseTreeDrop) Rules.ExtraWoodCount(parsed);
                    return parsed;
                });
                magic.Effects.Add(new MagicItemEffect(choice.Type, value));
                if (!IsRequired(choice.Type)) compatibility.Effects.Add(new MagicItemEffect(choice.Type, value));
            }
            if (!API.ApplyMagicItemJson(item, JsonConvert.SerializeObject(magic))) throw new InvalidOperationException("Büyü verisi uygulanamadı.");
            for (int i = 0; i < Stones.Count; i++)
            {
                string prefabName = Stones[i];
                var stonePrefab = ObjectDB.instance.GetItemPrefab(prefabName);
                if (!stonePrefab || !stonePrefab.TryGetComponent(out ItemDrop stoneDrop))
                    throw new FieldProblem("Stone." + i, $"Taş bulunamadı: {prefabName}");
                var stone = stoneDrop.m_itemData.Clone();
                stone.m_dropPrefab = stonePrefab;
                if (!ShardSocketManager.CanSocket(item, stone, out string reason))
                    throw new FieldProblem("Stone." + i, prefabName + ": " + Localization.instance.Localize(reason ?? "Taş takılamıyor."));
                if (!ShardSocketManager.AddShard(item, stone)) throw new FieldProblem("Stone." + i, "Taş takılamadı: " + prefabName);
            }
            item.m_durability = item.GetMaxDurability();
            item.m_customData[ExactWoodDrops.Marker] = "1";
            return item;
        }

        public ItemDrop.ItemData Prepare()
        {
            if (!Player.m_localPlayer || !ZNetScene.instance) throw new InvalidOperationException("Önce dünyaya gir.");
            return Build();
        }

        public static bool Deliver(ItemDrop.ItemData item, bool inventory)
        {
            if (inventory) return Player.m_localPlayer.GetInventory().AddItem(item);
            Vector3 position = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2 + Vector3.up;
            var baseline = ItemCopies.Baseline(item);
            var dropped = ItemDrop.DropItem(item, 1, position + UnityEngine.Random.insideUnitSphere * 0.15f, Quaternion.identity);
            if (!dropped)
                throw new InvalidOperationException("Yere bırakma başarısız.");
            ItemCopies.RestoreBaseline(dropped.m_itemData, baseline);
            return true;
        }

        public void AddEffect(MagicItemEffectDefinition definition)
        {
            if (!Available().Any(x => x.Type == definition.Type)) throw new InvalidOperationException("Bu efekt mevcut eşya seçiminde eklenemez.");
            var range = EffectRange(definition);
            Effects.Add(new EffectChoice { Type = definition.Type, Value = (range?.MinValue ?? 1).ToString(CultureInfo.InvariantCulture) });
        }

        public int RequestedCount => FieldValidation.At("Count", () => Rules.Bounded(Count, 1, 100, "Adet"));
    }
}
