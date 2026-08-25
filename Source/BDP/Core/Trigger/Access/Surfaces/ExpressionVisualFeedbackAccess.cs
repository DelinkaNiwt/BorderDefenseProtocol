using BDP.Core.Expressions;
using BDP.Core.Trigger.Runtime;
using UnityEngine;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 内容层向正式表达视觉发布短暂反馈的中性入口。
    /// 调用方只提交表达 Hediff 与位移参数，不接触 Trigger 内部运行时状态。
    /// </summary>
    public static class ExpressionVisualFeedbackAccess
    {
        /// <summary>为指定表达 Hediff 当前绑定的全部正式结果发布一次受击视觉冲量。</summary>
        public static void NotifyImpact(
            Hediff expressionHediff,
            Vector3 direction,
            int durationTicks,
            float distance)
        {
            BdpExpressionHostHediff hostHediff = expressionHediff as BdpExpressionHostHediff;
            Pawn pawn = hostHediff?.pawn;
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            TriggerVisualRuntimeStateOwner owner =
                triggerBody?.RuntimeServices?.TriggerVisualRuntimeStateOwner;
            if (hostHediff == null
                || owner == null
                || hostHediff.ExpressionResults == null
                || durationTicks <= 0
                || distance <= 0f)
            {
                return;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            for (int index = 0; index < hostHediff.ExpressionResults.Count; index++)
            {
                FormalExpressionResult result = hostHediff.ExpressionResults[index];
                if (result == null || string.IsNullOrWhiteSpace(result.Id))
                {
                    continue;
                }

                owner.PublishExpressionVisualImpulse(
                    result.Id,
                    direction,
                    currentTick,
                    durationTicks,
                    distance);
            }
        }
    }
}
