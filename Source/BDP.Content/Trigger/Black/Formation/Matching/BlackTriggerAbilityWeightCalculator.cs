using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>计算黑触发器适应能力的隐藏形成权重。</summary>
    public static class BlackTriggerAbilityWeightCalculator
    {
        /// <summary>能力适应每项命中的权重增量。</summary>
        public const int WeightPerMatch = BlackTriggerTraitWeightCalculator.WeightPerMatch;

        /// <summary>按不同实际来源能力计算权重，并返回命中稳定键。</summary>
        public static int Resolve(
            List<BlackTriggerAdaptedAbility> adaptedAbilities,
            List<BlackTriggerAbilitySnapshot> sourceAbilities,
            List<string> matchedAbilityKeys)
        {
            if (matchedAbilityKeys != null) matchedAbilityKeys.Clear();
            HashSet<string> matches = new HashSet<string>(StringComparer.Ordinal);
            foreach (BlackTriggerAbilitySnapshot source in sourceAbilities ?? new List<BlackTriggerAbilitySnapshot>())
            {
                if (source == null || source.AbilityDefName.NullOrEmpty()) continue;
                if ((adaptedAbilities ?? new List<BlackTriggerAdaptedAbility>()).Any(value =>
                    value != null
                    && DefDatabase<AbilityDef>.GetNamedSilentFail(value.abilityDefName) != null
                    && value.Matches(source)))
                {
                    matches.Add(source.StableKey);
                }
            }
            if (matchedAbilityKeys != null)
            {
                matchedAbilityKeys.AddRange(matches.OrderBy(value => value, StringComparer.Ordinal));
            }
            return BlackTriggerTraitWeightCalculator.BaseWeight + matches.Count * WeightPerMatch;
        }
    }
}
