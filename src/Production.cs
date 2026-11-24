using System;
using System.Collections;
using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private Coroutine production;
        private bool stopProduction;
        private int produced, requested;

        private void BeginSpawn()
        {
            if (production != null) throw new InvalidOperationException("Üretim devam ediyor; önce durdur.");
            requested = builder.RequestedCount;
            var template = builder.Prepare();
            produced = 0; stopProduction = false;
            production = StartCoroutine(Produce(template, toInventory));
        }

        private IEnumerator Produce(ItemDrop.ItemData template, bool inventory)
        {
            // Yield before delivery so the coroutine handle is installed before it can finish.
            yield return null;
            try
            {
                while (produced < requested && !stopProduction && Player.m_localPlayer && ZNetScene.instance)
                {
                    string failure = null;
                    bool full = false;
                    for (int batch = 0; batch < 8 && produced < requested && !stopProduction; batch++)
                    {
                        try
                        {
                            if (!Spawner.Deliver(ItemCopies.Clone(template), inventory)) { full = true; break; }
                            produced++;
                        }
                        catch (Exception ex) { failure = ex.Message; RecordError(ex); Logger.LogWarning(ex); break; }
                    }
                    status = $"{produced}/{requested} eşya üretildi.";
                    if (failure != null) { status += " " + failure; yield break; }
                    if (full) { status += " Envanterde yer kalmadı."; yield break; }
                    yield return null;
                }
                status = $"{produced}/{requested} eşya üretildi." + (produced < requested ? " Üretim durduruldu." : " Tamamlandı.");
            }
            finally { production = null; lastOperation = status; }
        }
    }
}
