using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Requirements;
using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// Combo 使用条件的低频变化监视器。
    /// 它只比较满足状态并请求现有投影重建，不自行发布或修改任何效果。
    /// </summary>
    internal sealed class ComboUseRequirementMonitor
    {
        /// <summary>自动复查间隔；60 tick 等于游戏内约一秒。</summary>
        private const int CheckIntervalTicks = 60;

        /// <summary>
        /// 在稳定错峰时刻检查有条件的 Combo；满足状态变化时返回 true。
        /// </summary>
        internal bool ShouldRefresh(
            int currentTick,
            int stableThingId,
            Pawn pawn,
            ExpressionSnapshot snapshot)
        {
            int stableOffset = (stableThingId & int.MaxValue) % CheckIntervalTicks;
            if (currentTick < 0
                || (currentTick + stableOffset) % CheckIntervalTicks != 0
                || pawn == null
                || snapshot?.Results == null)
            {
                return false;
            }

            HashSet<string> checkedComboDefs = new HashSet<string>();
            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult result = snapshot.Results[i];
                if (result == null
                    || string.IsNullOrWhiteSpace(result.ComboDefName)
                    || !checkedComboDefs.Add(result.ComboDefName))
                {
                    continue;
                }

                ComboDef comboDef = DefDatabase<ComboDef>.GetNamedSilentFail(result.ComboDefName);
                if (comboDef?.UseRequirements == null || comboDef.UseRequirements.Count == 0)
                {
                    continue;
                }

                PawnRequirementCheckResult freshCheck =
                    ComboUseRequirementService.Instance.Evaluate(pawn, comboDef);
                bool publishedSatisfied = result.UseRequirementCheck == null
                    || result.UseRequirementCheck.Satisfied;
                if (freshCheck.Satisfied != publishedSatisfied)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
