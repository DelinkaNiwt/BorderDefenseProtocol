using System;
using System.Collections.Generic;
using System.Linq;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>计算黑触发器适应特性的隐藏形成权重。</summary>
    public static class BlackTriggerTraitWeightCalculator
    {
        /// <summary>没有适应特性命中时的基础权重。</summary>
        public const int BaseWeight = 1;

        /// <summary>每项未压制命中特性增加的权重。</summary>
        public const int WeightPerMatch = 3;

        /// <summary>按不同实际来源特性计算权重，并返回命中稳定键。</summary>
        public static int Resolve(
            List<BlackTriggerAdaptedTrait> adaptedTraits,
            List<BlackTriggerTraitSnapshot> sourceTraits,
            List<string> matchedTraitKeys)
        {
            if (matchedTraitKeys != null) matchedTraitKeys.Clear();
            HashSet<string> matches = new HashSet<string>(StringComparer.Ordinal);
            foreach (BlackTriggerTraitSnapshot source in sourceTraits ?? new List<BlackTriggerTraitSnapshot>())
            {
                if (source == null || source.WasSuppressed || source.TraitDef == null) continue;
                if ((adaptedTraits ?? new List<BlackTriggerAdaptedTrait>()).Any(value => value?.Matches(source) == true))
                {
                    matches.Add(source.StableKey);
                }
            }
            if (matchedTraitKeys != null)
            {
                matchedTraitKeys.AddRange(matches.OrderBy(value => value, StringComparer.Ordinal));
            }
            return BaseWeight + matches.Count * WeightPerMatch;
        }
    }
}
