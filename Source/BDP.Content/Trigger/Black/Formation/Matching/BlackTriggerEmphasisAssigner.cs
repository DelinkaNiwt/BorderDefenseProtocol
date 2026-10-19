using System;
using System.Collections.Generic;
using System.Linq;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>在候选已经成立后，为每件物品分配一个玩家可选战斗侧重。</summary>
    public static class BlackTriggerEmphasisAssigner
    {
        /// <summary>先用二分图最大匹配实现最大覆盖，再平衡剩余候选。</summary>
        public static void Assign(List<BlackTriggerCandidatePlan> plans, int seed)
        {
            if (plans == null) return;
            foreach (BlackTriggerCandidatePlan plan in plans) if (plan != null) plan.AssignedEmphasis = null;
            MaximizeCoverage(plans, seed);
            Dictionary<string, int> counts = plans
                .Where(value => value?.AssignedEmphasis != null)
                .GroupBy(value => value.AssignedEmphasis.defName, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            foreach (BlackTriggerCandidatePlan plan in plans.Where(value => value != null && value.AssignedEmphasis == null).OrderBy(value => value.ThingDef?.defName ?? string.Empty, StringComparer.Ordinal))
            {
                List<BlackTriggerEmphasisDef> options = OrderedOptions(plan, seed)
                    .OrderBy(value => counts.ContainsKey(value.defName) ? counts[value.defName] : 0)
                    .ThenBy(value => BlackTriggerStableRandom.Hash(seed, (plan.ThingDef?.defName ?? string.Empty) + ":" + value.defName))
                    .ToList();
                if (options.Count == 0) continue;
                plan.AssignedEmphasis = options[0];
                counts[options[0].defName] = counts.ContainsKey(options[0].defName) ? counts[options[0].defName] + 1 : 1;
            }
        }

        /// <summary>用增广路径让不同侧重的覆盖数量达到最大。</summary>
        private static void MaximizeCoverage(List<BlackTriggerCandidatePlan> plans, int seed)
        {
            Dictionary<string, BlackTriggerCandidatePlan> ownerByEmphasis = new Dictionary<string, BlackTriggerCandidatePlan>(StringComparer.Ordinal);
            foreach (BlackTriggerCandidatePlan plan in plans.Where(value => value != null).OrderBy(value => BlackTriggerStableRandom.Hash(seed, value.ThingDef?.defName)))
            {
                TryAssignUnique(plan, ownerByEmphasis, new HashSet<string>(StringComparer.Ordinal), seed);
            }
            foreach (KeyValuePair<string, BlackTriggerCandidatePlan> pair in ownerByEmphasis)
            {
                pair.Value.AssignedEmphasis = pair.Value.AvailableEmphases.FirstOrDefault(value => value?.defName == pair.Key);
            }
        }

        /// <summary>为一个候选寻找可用侧重，必要时递归移动原占用者。</summary>
        private static bool TryAssignUnique(BlackTriggerCandidatePlan plan, Dictionary<string, BlackTriggerCandidatePlan> owners, HashSet<string> visited, int seed)
        {
            foreach (BlackTriggerEmphasisDef emphasis in OrderedOptions(plan, seed))
            {
                if (!visited.Add(emphasis.defName)) continue;
                BlackTriggerCandidatePlan current;
                if (!owners.TryGetValue(emphasis.defName, out current) || TryAssignUnique(current, owners, visited, seed))
                {
                    owners[emphasis.defName] = plan;
                    return true;
                }
            }
            return false;
        }

        /// <summary>以稳定种子排列一个候选允许的侧重。</summary>
        private static List<BlackTriggerEmphasisDef> OrderedOptions(BlackTriggerCandidatePlan plan, int seed)
        {
            return (plan?.AvailableEmphases ?? new List<BlackTriggerEmphasisDef>())
                .Where(value => value != null)
                .OrderBy(value => BlackTriggerStableRandom.Hash(seed, (plan.ThingDef?.defName ?? string.Empty) + ":" + value.defName))
                .ThenBy(value => value.defName, StringComparer.Ordinal)
                .ToList();
        }
    }
}
