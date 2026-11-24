// Methods from RandyKnapp/ValheimMods EpicLoot/src/Magic/MagicItem.cs,
// commit 69704d5fbe86910e6b0de7f68c054028f20e3826. Compared against installed
// EpicLoot 0.14.13 DLL in work/Comparison-api-check. Test fixture only.
using System.Collections.Generic;
using System.Linq;
namespace EpicLoot
{
    public partial class MagicItem
    {
        public IEnumerable<MagicItemEffect> GetSocketedEffects() => Sockets.Where(x => x?.Effect != null).Select(x => x.Effect);
        public List<MagicItemEffect> GetEffects(string effectType = null, bool includeSocketed = false)
        {
            IEnumerable<MagicItemEffect> source = Effects;
            if (includeSocketed && Sockets.Count > 0) source = source.Concat(GetSocketedEffects());
            return effectType == null ? source.ToList() : source.Where(x => x.EffectType == effectType).ToList();
        }
        public float GetTotalEffectValue(string effectType, float scale = 1.0f, bool includeSocketed = true)
            => SumEffectValue(effectType, scale, includeSocketed);
        public float SumEffectValue(string effectType, float scale = 1.0f, bool includeSocketed = true)
        {
            float total = 0f;
            for (int i = 0; i < Effects.Count; i++)
                if (Effects[i].EffectType == effectType) total += Effects[i].EffectValue;
            if (includeSocketed)
                for (int i = 0; i < Sockets.Count; i++)
                {
                    MagicItemEffect effect = Sockets[i]?.Effect;
                    if (effect != null && effect.EffectType == effectType) total += effect.EffectValue;
                }
            return total * scale;
        }
    }
}
