using System;
using System.Linq;
using EpicLoot;
using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private Vector2 disenchantScroll;

        private void DrawReturnedStones(System.Collections.Generic.IEnumerable<ItemDrop.ItemData> stones)
        {
            foreach (var group in stones.GroupBy(x => Localization.instance.Localize(API.GetItemDisplayName(x))))
                GUILayout.Label("• " + group.Count() + " × " + group.Key);
        }

        private void DrawDisenchant(int id)
        {
            bool singleStone = disenchantPlan != null && disenchantPlan.SocketIndex >= 0;
            GUILayout.Label(singleStone ? "TEK TAŞ ÇIKAR" : "EŞYANIN BÜYÜSÜNÜ KALDIR", heading);
            GUILayout.Label(singleStone ? "Yalnız seçili taş iade edilir. Büyü, diğer taşlar ve soket kapasitesi korunur." : "Büyü ve tüm soketler kaldırılır. Takılı taşlar envantere iade edilir.", muted);
            GUILayout.Label("İşlem asıl eşyaya uygulanır; hazırlanmış taslak kullanılmaz.", muted);
            int next = GUILayout.SelectionGrid(removalTab, new[] { singleStone ? "Taş seçimi" : "İade taşları", "Değişiklikler", "Son eşya" }, 3, tabs, GUILayout.Height(36));
            if (next != removalTab) QueueAction(() => { removalTab = next; disenchantScroll = Vector2.zero; });
            disenchantScroll = GUILayout.BeginScrollView(disenchantScroll, GUILayout.ExpandHeight(true));
            if (disenchantPlan != null)
            {
                if (removalTab == 0 && singleStone)
                {
                    for (int i = 0; i < removalStoneCards.Length; i++)
                    {
                        int index = i; var info = removalStoneCards[i];
                        GUILayout.BeginVertical(card); GUILayout.BeginHorizontal();
                        DrawIcon(ObjectDB.instance.GetItemPrefab(info.Prefab), 36);
                        bool current = i == disenchantPlan.SocketIndex;
                        var content = new GUIContent((current ? "● " : "○ ") + info.Name, "Taşı seç • Soket " + (i + 1) + "\n" + info.Name);
                        const float nameWidth = 520;
                        float nameHeight = Math.Max(44, theme.button.CalcHeight(content, nameWidth));
                        if (GUILayout.Button(content, current ? selected : theme.button, GUILayout.Width(nameWidth), GUILayout.Height(nameHeight)))
                            QueueAction(() =>
                            {
                                DisenchantService.CheckSource(Player.m_localPlayer.GetInventory(), disenchantPlan);
                                OpenRemoval(index);
                            });
                        GUILayout.EndHorizontal();
                        GUILayout.Label("Bu soketteki katkı", muted);
                        GUILayout.Label(info.Title, section); GUILayout.Label(info.Description);
                        if (!string.IsNullOrEmpty(info.Note)) GUILayout.Label(info.Note, muted);
                        GUILayout.EndVertical();
                    }
                }
                else if (removalTab == 0)
                {
                    GUILayout.Label("İade edilecek taşlar", section);
                    DrawReturnedStones(disenchantPlan.ReturnedStones);
                    if (disenchantPlan.ReturnedStones.Count == 0) GUILayout.Label("Bu eşyada iade edilecek taş yok.", muted);
                }
                else GUILayout.Label(removalTab == 1 ? disenchantComparison : disenchantTooltip);
            }
            if (disenchantReason != null) GUILayout.Label(SafeLabel(disenchantReason), errorHint);
            GUILayout.EndScrollView();
            GUILayout.Label("İade: " + (disenchantPlan?.ReturnedStones.Count ?? 0) + " taş • Eşya seviyesi ve dayanıklılık oranı korunur.", muted);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && disenchantReason == null && disenchantPlan != null;
            if (GUILayout.Button(singleStone ? "Seçili taşı çıkar ve iade et" : "Büyüyü kaldır ve taşları iade et", primary, GUILayout.Height(46))) QueueDesignAction(ApplyDisenchant);
            GUI.enabled = enabled;
            if (GUILayout.Button("Vazgeç", GUILayout.Height(36))) QueueAction(() => { showDisenchant = false; disenchantPlan = null; });
        }
    }
}
