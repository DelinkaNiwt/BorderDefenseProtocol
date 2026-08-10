using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BDP.Content.Trion.Talent.Capacity
{
    /// <summary>
    /// 把精确容量潜质解析为玩家可见模糊档位。
    /// </summary>
    public sealed class TrionCapacityPotentialBandResolver
    {
        /// <summary>共享无状态解析器。</summary>
        public static readonly TrionCapacityPotentialBandResolver Instance = new TrionCapacityPotentialBandResolver();

        /// <summary>禁止外部创建重复实例。</summary>
        private TrionCapacityPotentialBandResolver()
        {
        }

        /// <summary>
        /// 查找唯一覆盖指定容量的档位；配置错误时拒绝猜测。
        /// </summary>
        public TrionCapacityPotentialBandDef Resolve(int capacity)
        {
            List<TrionCapacityPotentialBandDef> matches = DefDatabase<TrionCapacityPotentialBandDef>.AllDefsListForReading
                .Where(def => def.Contains(capacity))
                .ToList();
            if (matches.Count == 1)
            {
                return matches[0];
            }

            Log.ErrorOnce(
                "[BDP.TrionTalentAssessment] 容量 " + capacity + " 匹配到 " + matches.Count + " 个检测档位，请检查档位断档或重叠。",
                17431001 + capacity);
            return null;
        }
    }
}
