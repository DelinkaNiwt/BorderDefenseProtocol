using System;
using System.Collections.Generic;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>与形成事件绑定、读档后不变化的稳定随机工具。</summary>
    public static class BlackTriggerStableRandom
    {
        /// <summary>从候选计划中稳定取出一项。</summary>
        public static BlackTriggerCandidatePlan Pick(IList<BlackTriggerCandidatePlan> candidates, int seed, string salt)
        {
            if (candidates == null || candidates.Count == 0) return null;
            return candidates[Index(candidates.Count, seed, salt)];
        }

        /// <summary>按冻结正整数权重稳定抽取一件候选。</summary>
        public static BlackTriggerCandidatePlan PickWeighted(IList<BlackTriggerCandidatePlan> candidates, int seed, string salt)
        {
            if (candidates == null || candidates.Count == 0) return null;
            int total = 0;
            for (int index = 0; index < candidates.Count; index++)
            {
                total += Math.Max(1, candidates[index]?.FormationWeight ?? 1);
            }
            if (total <= 0) return null;
            int roll = Index(total, seed, salt);
            for (int index = 0; index < candidates.Count; index++)
            {
                roll -= Math.Max(1, candidates[index]?.FormationWeight ?? 1);
                if (roll < 0) return candidates[index];
            }
            return candidates[candidates.Count - 1];
        }

        /// <summary>在指定范围内产生稳定索引。</summary>
        public static int Index(int count, int seed, string salt)
        {
            if (count <= 0) return -1;
            int hash = Hash(seed, salt);
            return Math.Abs(hash == int.MinValue ? int.MaxValue : hash) % count;
        }

        /// <summary>组合整数种子与文本盐值。</summary>
        public static int Hash(int seed, string salt)
        {
            unchecked
            {
                int hash = seed;
                string value = salt ?? string.Empty;
                for (int index = 0; index < value.Length; index++) hash = hash * 31 + value[index];
                return hash;
            }
        }
    }
}
