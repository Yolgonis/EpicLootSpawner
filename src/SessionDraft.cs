using System;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using EpicLoot;
using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private DraftStore draftStore;
        private SavedDraft loadedDraft;
        private bool restoreDraftPending, draftSaveBlocked;
        private float draftSaveDue;
        private string observedSaveDraft;
        private string blockedRecipeSignature;

        private void InitializeDraftStore()
        {
            draftStore = new DraftStore(Path.Combine(Paths.ConfigPath, "EpicLootSpawner.draft.json"), RarityCount);
            Run(() => { loadedDraft = draftStore.Load(); restoreDraftPending = loadedDraft != null; });
        }
        private void RestoreSessionDraft()
        {
            if (!restoreDraftPending) return;
            restoreDraftPending = false;
            try
            {
                builder.RestoreDraft(loadedDraft.Recipe);
                draftSaveBlocked = false; blockedRecipeSignature = null;
                toInventory = loadedDraft.ToInventory; stoneRarity = (ItemRarity)loadedDraft.StoneRarity;
                inventoryKey = null; RecordDesign(); RefreshEffects();
                status = "Son tasarım geri yüklendi. Envanter düzenlemesi için eşyayı tekrar seç.";
            }
            catch
            {
                draftSaveBlocked = true;
                blockedRecipeSignature = JsonConvert.SerializeObject(builder.Draft());
                throw;
            }
        }
        private SavedDraft CurrentSavedDraft() => new SavedDraft { Recipe = builder.Draft(), ToInventory = toInventory, StoneRarity = (int)stoneRarity };
        private void SaveSessionDraft()
        {
            UnlockDraftForNewRecipe(builder.Draft());
            if (draftStore == null || !draftStore.Ready || draftSaveBlocked) return;
            draftStore.Save(CurrentSavedDraft());
        }
        private void UnlockDraftForNewRecipe(Recipe recipe)
        {
            if (draftSaveBlocked && !string.IsNullOrWhiteSpace(recipe.Prefab) && JsonConvert.SerializeObject(recipe) != blockedRecipeSignature)
            {
                draftSaveBlocked = false;
                blockedRecipeSignature = null;
            }
        }
        private void UpdateSessionDraft()
        {
            if (!IsOpen || draftStore == null || !draftStore.Ready) return;
            var draft = CurrentSavedDraft();
            UnlockDraftForNewRecipe(draft.Recipe);
            string signature = JsonConvert.SerializeObject(draft);
            if (signature != observedSaveDraft)
            {
                observedSaveDraft = signature;
                draftSaveDue = Time.unscaledTime + 2;
            }
            if (draftSaveDue <= 0 || Time.unscaledTime < draftSaveDue) return;
            draftSaveDue = 0;
            Run(SaveSessionDraft);
        }
    }
}
