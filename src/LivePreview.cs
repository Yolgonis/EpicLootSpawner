using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot;
using EpicLoot.ShardStones;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicLootSpawner
{
    internal sealed class StonePreview
    {
        public bool Allowed;
        public string Title, Description, Note, Reason;
    }

    public sealed partial class Plugin
    {
        private ItemDrop.ItemData previewItem;
        private string previewError = "Önce bir eşya seç.";
        private string previewSignature, observedSignature;
        private string currentPreviewSignature;
        private Dictionary<string, string> fieldErrors = new Dictionary<string, string>();
        private float previewDue;
        private string originalPreview;
        private int comparisonTab;
        private string VisiblePreview => inventoryKey == null || comparisonTab == 1 ? preview : comparisonTab == 2 ? originalPreview : comparisonPreview;
        private readonly Dictionary<string, StonePreview> stonePreviews = new Dictionary<string, StonePreview>();

        private string DraftSignature() => JsonConvert.SerializeObject(builder.Draft()) + "|" + stoneRarity + "|" + inventoryKey + "|" + builder.Unlimited;

        private bool PreviewValid => currentPreviewSignature != null && previewItem != null && previewError == null && previewSignature == currentPreviewSignature;

        private void UpdateLivePreview()
        {
            if (!IsOpen) return;
            string signature = DraftSignature();
            currentPreviewSignature = signature;
            if (signature != observedSignature)
            {
                observedSignature = signature;
                previewDue = Time.unscaledTime + 0.15f;
            }
            if (signature == previewSignature || Time.unscaledTime < previewDue) return;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            previewItem = null; preview = ""; originalPreview = ""; comparisonPreview = ""; stonePreviews.Clear();
            previewSignature = signature;
            fieldErrors.Clear();
            try
            {
                fieldErrors = builder.FieldErrors();
                _ = builder.RequestedCount; // Validate quantity as part of the visible recipe.
                previewItem = builder.Build();
                if (inventoryKey != null)
                {
                    var source = EditingSource;
                    if (source == null || !Player.m_localPlayer.GetInventory().GetAllItems().Contains(source.Item)) throw new InvalidOperationException("Eşya artık envanterde değil. Tekrar seç.");
                    if (source.Fingerprint != ItemFingerprint(source.Item)) throw new InvalidOperationException("Asıl eşya dışarıdan değişti. Envanter sekmesinden tekrar seç.");
                    originalPreview = Localization.instance.Localize(API.GetItemDisplayName(source.Item)) + "\n\n" + source.Item.GetTooltip();
                    previewItem = PrepareInventoryEdit(source.Item, previewItem);
                    comparisonPreview = DescribeComparison(source.Item, previewItem);
                }
                preview = Localization.instance.Localize(API.GetItemDisplayName(previewItem)) + "\n\n" + previewItem.GetTooltip();
                previewError = null;
                available = builder.Available();
                RefreshEffectCards();
            }
            catch (Exception ex)
            {
                previewItem = null; previewError = ex.Message;
                if (ex is FieldProblem problem) fieldErrors[problem.Key] = problem.Message;
                // An invalid value must not prevent fixing the selected effect in the editor.
                try { available = builder.Prefab ? builder.Available() : new List<MagicItemEffectDefinition>(); }
                catch { available = new List<MagicItemEffectDefinition>(); }
                RefreshEffectCards();
            }
            finally
            {
                lastPreviewMs = (float)timer.Elapsed.TotalMilliseconds;
                if (performanceLog.Value) Logger.LogInfo("Önizleme: " + lastPreviewMs.ToString("0.0") + " ms");
            }
        }

        private float lastStoneMs;
        private int stonesEvaluated;
        private void UpdateStonePreviews()
        {
            if (!IsOpen || picker != PickerKind.Stone || !PreviewValid) return;
            var timer = System.Diagnostics.Stopwatch.StartNew(); int processed = 0;
            foreach (var prefab in filteredStones)
            {
                if (stonePreviews.ContainsKey(prefab.name)) continue;
                stonePreviews[prefab.name] = EvaluateStone(prefab); stonesEvaluated++;
                if (++processed >= 3 || timer.Elapsed.TotalMilliseconds >= 4) break;
            }
            if (processed > 0) lastStoneMs = (float)timer.Elapsed.TotalMilliseconds;
        }

        private StonePreview EvaluateStone(GameObject prefab)
        {
            var result = new StonePreview();
            try
            {
                var input = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
                input.m_dropPrefab = prefab;
                var candidate = ItemCopies.Clone(previewItem);
                // Replace magic data with a deep deserialization so a hypothetical socket can't alter the preview.
                if (!API.ApplyMagicItemJson(candidate, API.GetMagicItemJson(previewItem))) throw new InvalidOperationException("Taş önizlemesi hazırlanamadı.");
                if (!ShardSocketManager.CanSocket(candidate, input, out string reason))
                {
                    result.Reason = Localization.instance.Localize(reason ?? "Taş takılamıyor.");
                    if (ShardSocketManager.ResolveSocketedEffect(candidate, input, out var blockedEffect, out _, out _))
                        DescribeStone(result, blockedEffect);
                    return result;
                }
                if (!ShardSocketManager.AddShard(candidate, input)) throw new InvalidOperationException("Taş önizlemesi hesaplanamadı.");
                var socket = candidate.GetMagicItem().Sockets.Last();
                DescribeStone(result, socket.Effect);
                result.Allowed = true;
                result.Reason = "Takılabilir";
            }
            catch (Exception ex) { result.Reason = ex.Message; }
            return result;
        }

        private static void DescribeStone(StonePreview result, MagicItemEffect effect)
        {
            if (effect == null)
            {
                result.Title = "Bu eşya türünde etkisiz";
                result.Description = "Taş bir soket kullanır; bu eşya türüne büyü etkisi sağlamaz.";
                return;
            }
            if (!MagicItemEffectDefinitions.AllDefinitions.TryGetValue(effect.EffectType, out var definition)) return;
            result.Title = EffectDisplay.Title(definition, effect.EffectValue);
            result.Description = EffectDisplay.Description(definition, effect.EffectValue);
            result.Note = EffectDisplay.SpawnerNote(definition, effect.EffectValue);
        }

        private void DrawLivePreview()
        {
            GUILayout.BeginVertical(panel, GUILayout.Width(layout.PreviewWidth), GUILayout.Height(layout.PanelHeight));
            GUILayout.Label("Önizleme", section);
            GUILayout.Space(4);
            string color = API.GetRarityColorByIndex((int)builder.Rarity);
            GUILayout.BeginHorizontal();
            DrawIcon(builder.Prefab, 52);
            GUILayout.BeginVertical();
            DrawItemHeading(builder.Prefab ? Label(builder.Prefab) : "Henüz eşya seçilmedi", layout.PreviewWidth - 92);
            GUILayout.Label("<color=" + color + ">" + Localization.instance.Localize(API.GetRarityDisplayNameByIndex((int)builder.Rarity)) + "</color>", muted);
            GUILayout.EndVertical(); GUILayout.EndHorizontal();
            string quality = int.TryParse(builder.Quality, out int level) ? level.ToString() : "—";
            string sockets = int.TryParse(builder.SocketCount, out int socketCount) ? socketCount.ToString() : "—";
            GUILayout.Label("Seviye " + quality + "  •  " + builder.Effects.Count + " efekt  •  " + builder.Stones.Count + "/" + sockets + " taş"
                + (builder.Unlimited ? "  •  limitsiz" : ""), badge, GUILayout.Height(32));
            GUILayout.Space(8);
            if (inventoryKey != null)
            {
                int next = GUILayout.SelectionGrid(comparisonTab, new[] { "Değişiklikler", "Yeni eşya", "Mevcut" }, 3, tabs, GUILayout.Height(36));
                if (next != comparisonTab) QueueAction(() => { comparisonTab = next; previewScroll = Vector2.zero; });
            }
            previewScroll = GUILayout.BeginScrollView(previewScroll, GUILayout.ExpandHeight(true));
            if (currentPreviewSignature == null || previewSignature != currentPreviewSignature) GUILayout.Label("Önizleme güncelleniyor…", muted);
            else if (previewError != null) GUILayout.Label("<color=#F2AD7C>Düzeltilmesi gereken</color>\n\n" + SafeLabel(previewError));
            else GUILayout.Label(VisiblePreview);
            GUILayout.EndScrollView();
            GUILayout.Label(PreviewValid ? (inventoryKey == null ? "<color=#9BD38A>✓ Üretime hazır</color>" : "<color=#9BD38A>✓ Geçerli</color>  •  Asıl eşyaya uygula ile kaydet")
                : builder.Prefab ? "Tasarımda düzeltilmesi gereken alan var." : "Henüz eşya seçilmedi.", muted);
            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && PreviewValid;
            if (GUILayout.Button(inventoryKey != null && comparisonTab == 0 ? "Değişiklikleri büyüt" : "Önizlemeyi büyüt", GUILayout.Height(36))) QueueAction(() => showPreview = true);
            GUI.enabled = oldEnabled;
            GUILayout.EndVertical();
        }
    }
}
