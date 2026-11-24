using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EpicLoot;
using EpicLoot.MagicItemEffects;
using EpicLoot.ShardStones;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicLootSpawner
{
    internal sealed class InventorySource
    {
        public ItemDrop.ItemData Item;
        public string Fingerprint;
        public GameObject Prefab;
    }
    internal sealed class InventoryEntry
    {
        public ItemDrop.ItemData Item;
        public GameObject Prefab;
        public string Label, Reason;
    }

    public sealed partial class Plugin
    {
        private string inventoryKey, inventorySearch = "";
        private readonly Dictionary<string, InventorySource> inventorySources = new Dictionary<string, InventorySource>();
        private readonly List<InventoryEntry> inventoryItems = new List<InventoryEntry>();
        private Vector2 inventoryScroll;
        private float inventoryDue;
        private string inventoryBlockReason;
        private string observedInventoryState;
        private InventorySource EditingSource => inventoryKey != null && inventorySources.TryGetValue(inventoryKey, out var source) ? source : null;
        private static string ItemFingerprint(ItemDrop.ItemData item) => item.m_quality + "|" + item.m_variant + "|" + API.GetMagicItemJson(item);

        private static string UnsupportedReason(ItemDrop.ItemData item)
        {
            if (item.m_stack != 1) return "Yığın halindeki eşya düzenlenemez.";
            var magic = item.GetMagicItem();
            if (magic == null) return null;
            if (magic.IsUnidentified) return "Önce eşyayı tanımla.";
            try { SpecialItems.Check(item, magic); } catch (InvalidOperationException ex) { return ex.Message; }
            foreach (var socket in magic.Sockets)
            {
                var prefab = ObjectDB.instance.GetItemPrefab(socket.SourcePrefab);
                if (!prefab || !prefab.TryGetComponent(out ItemDrop drop) || !Shards.IsShard(drop.m_itemData))
                    return "Runestone veya kaynağı bulunamayan soketler bu editörde desteklenmiyor.";
            }
            return null;
        }

        private void UpdateInventoryLibrary()
        {
            if (!IsOpen || Time.unscaledTime < inventoryDue) return;
            inventoryDue = Time.unscaledTime + 0.5f;
            try { inventoryBlockReason = inventoryKey == null ? null : EditBlockedReason(); }
            catch (Exception ex) { inventoryBlockReason = ex.Message; }
            var source = EditingSource;
            string state;
            try { state = source == null ? null : ItemFingerprint(source.Item) + "|" + source.Item.m_durability + "|" + Player.m_localPlayer.GetInventory().GetAllItems().Contains(source.Item); }
            catch (Exception ex) { state = ex.Message; inventoryBlockReason = ex.Message; }
            if (state != observedInventoryState)
            {
                observedInventoryState = state;
                if (inventoryKey != null) { observedSignature = null; previewSignature = null; }
            }
            if (libraryTab != 2) return;
            inventoryItems.Clear();
            foreach (var item in Player.m_localPlayer.GetInventory().GetAllItems().ToArray())
            {
                try
                {
                    if (item?.m_shared == null || !API.CanBeMagicItem(item)) continue;
                    var prefab = item.m_dropPrefab ? item.m_dropPrefab : items.FirstOrDefault(x => x.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == item.m_shared.m_name);
                    if (!prefab) continue;
                    inventoryItems.Add(new InventoryEntry
                    {
                        Item = item, Prefab = prefab,
                        Label = Localization.instance.Localize(API.GetItemDisplayName(item)), Reason = UnsupportedReason(item)
                    });
                }
                catch (Exception ex) { Logger.LogWarning("Envanter eşyası okunamadı: " + ex.Message); }
            }
            inventoryItems.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.CurrentCultureIgnoreCase));
        }

        private void LoadInventoryItem(InventoryEntry entry)
        {
            if (!Player.m_localPlayer.GetInventory().GetAllItems().Contains(entry.Item)) throw new InvalidOperationException("Eşya artık envanterde değil.");
            string reason = UnsupportedReason(entry.Item);
            if (reason != null) throw new InvalidOperationException(reason);
            var magic = entry.Item.GetMagicItem();
            var draft = new Recipe
            {
                Prefab = entry.Prefab.name, Rarity = (int)(magic?.Rarity ?? ItemRarity.Magic),
                LegendaryID = magic?.LegendaryID, SetID = magic?.SetID,
                Quality = entry.Item.m_quality.ToString(CultureInfo.InvariantCulture), Count = "1",
                SocketCount = (magic?.SocketCount ?? 0).ToString(CultureInfo.InvariantCulture), Name = magic?.DisplayName ?? ""
            };
            if (magic != null)
            {
                draft.Effects.AddRange(magic.Effects.Select(x => new EffectChoice { Type = x.EffectType, Value = x.EffectValue.ToString("G9", CultureInfo.InvariantCulture) }));
                draft.Stones.AddRange(magic.Sockets.Select(x => x.SourcePrefab));
            }
            builder.RestoreDraft(draft);
            inventoryKey = Guid.NewGuid().ToString("N");
            inventorySources[inventoryKey] = new InventorySource { Item = entry.Item, Fingerprint = ItemFingerprint(entry.Item), Prefab = entry.Prefab };
            inventoryDue = 0;
            RefreshEffects();
            status = "Envanter eşyası tasarıma alındı. Değişiklikler Uygula'ya basınca yazılır.";
        }

        private string EditBlockedReason()
        {
            var source = EditingSource;
            if (source == null || !Player.m_localPlayer.GetInventory().GetAllItems().Contains(source.Item)) return "Eşya artık envanterde değil. Tekrar seç.";
            if (source.Prefab != builder.Prefab) return "Eşya türü değiştirilemez; envanterden tekrar seç.";
            if (ItemFingerprint(source.Item) != source.Fingerprint) return "Asıl eşya dışarıdan değişti. Envanter sekmesinden tekrar seç.";
            if (source.Item.m_equipped) return "Uygulamadan önce kullanılan eşyayı çıkar.";
            if (production != null) return "Önce devam eden üretimi durdur.";
            return UnsupportedReason(source.Item);
        }

        private static ItemDrop.ItemData.SharedData CopyBaselineShared(ItemDrop.ItemData item)
            => ItemCopies.Baseline(item);

        private static ItemDrop.ItemData PrepareInventoryEdit(ItemDrop.ItemData original, ItemDrop.ItemData designed)
        {
            var baseline = CopyBaselineShared(original);
            float ratio = Rules.DurabilityRatio(original.m_durability, original.GetMaxDurability());
            var candidate = original.Clone();
            if (!candidate.m_dropPrefab) candidate.m_dropPrefab = designed.m_dropPrefab;
            // Clone hooks may already have registered a durability baseline for the candidate.
            // Reset that registration before installing its own unmodified SharedData.
            Indestructible.Revert(candidate);
            candidate.m_shared = baseline;
            candidate.m_quality = designed.m_quality;
            var magic = JsonConvert.DeserializeObject<MagicItem>(API.GetMagicItemJson(designed));
            var oldMagic = original.GetMagicItem();
            if ((magic.LegendaryID ?? "") != (oldMagic?.LegendaryID ?? "") || (magic.SetID ?? "") != (oldMagic?.SetID ?? ""))
                throw new InvalidOperationException("Envanter düzenlemesinde özel eşya ve set kimliği değiştirilemez. Yeni eşya üretimine dön.");
            if (oldMagic != null)
            {
                magic.TypeNameOverride = oldMagic.TypeNameOverride;
                string[] oldTypes = oldMagic.Effects.Select(x => x.EffectType).ToArray(), newTypes = magic.Effects.Select(x => x.EffectType).ToArray();
                magic.AugmentedEffectIndex = Rules.RemapEffectIndex(oldMagic.AugmentedEffectIndex, oldTypes, newTypes);
                magic.AugmentedEffectIndices = oldMagic.AugmentedEffectIndices.Select(x => Rules.RemapEffectIndex(x, oldTypes, newTypes)).Where(x => x >= 0).Distinct().ToList();
                magic.TemperedEffectIndices = oldMagic.TemperedEffectIndices.Select(x => Rules.RemapEffectIndex(x, oldTypes, newTypes)).Where(x => x >= 0).Distinct().ToList();
            }
            if (!API.ApplyMagicItemJson(candidate, JsonConvert.SerializeObject(magic))) throw new InvalidOperationException("Düzenlenmiş eşya hazırlanamadı.");
            candidate.m_durability = ratio * candidate.GetMaxDurability();
            candidate.m_customData[ExactWoodDrops.Marker] = "1";
            return candidate;
        }

        private void ApplyInventoryEdit()
        {
            string reason = EditBlockedReason();
            if (reason != null) throw new InvalidOperationException(reason);
            var source = EditingSource;
            var target = source.Item;
            var candidate = PrepareInventoryEdit(target, builder.Build());
            if (candidate.m_dropPrefab != builder.Prefab) throw new InvalidOperationException("Eşya türü değiştirilemez; envanterden tekrar seç.");
            lastInventoryOperation = InventoryOperations.Apply(Player.m_localPlayer.GetInventory(), target,
                candidate, Array.Empty<ItemDrop.ItemData>(), "Envanter düzenleme");
            inventoryUndoDue = 0;
            source.Fingerprint = ItemFingerprint(target);
            RefreshEffects(); inventoryDue = 0;
            status = "Değişiklikler asıl eşyaya uygulandı. Konum, varyant ve dayanıklılık oranı korundu.";
        }

        private void DrawInventoryLibrary()
        {
            GUILayout.Label("Envanterim", section);
            GUILayout.Label("Seçmek eşyayı değiştirmez; değişiklikler Asıl eşyaya uygula ile yazılır.", muted);
            string search = SearchInput(inventorySearch, "Envanterde ara");
            if (search != inventorySearch) QueueAction(() => inventorySearch = search);
            inventoryScroll = GUILayout.BeginScrollView(inventoryScroll, GUILayout.ExpandHeight(true));
            foreach (var entry in inventoryItems)
            {
                if (!Matches(entry.Label + " " + entry.Prefab.name, inventorySearch)) continue;
                GUILayout.BeginHorizontal(); DrawIcon(entry.Prefab, 36);
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && entry.Reason == null;
                string state = entry.Reason ?? (entry.Item.m_equipped ? "Kullanılıyor • uygulamadan önce çıkar" : "Seviye " + entry.Item.m_quality);
                string label = entry.Label + "\n<size=11>" + SafeLabel(state) + "</size>";
                string tip = entry.Label + "  [" + (entry.Item.m_gridPos.x + 1) + "," + (entry.Item.m_gridPos.y + 1) + "]\nPrefab: " + entry.Prefab.name;
                if (NameButton(label, tip, EditingSource?.Item == entry.Item, 0)) QueueDesignAction(() => { LoadInventoryItem(entry); libraryTab = 2; });
                GUI.enabled = enabled; GUILayout.EndHorizontal();
            }
            if (inventoryItems.Count == 0) GUILayout.Label("Envanterinde büyülenebilir eşya yok.", muted);
            GUILayout.EndScrollView();
        }
    }
}
