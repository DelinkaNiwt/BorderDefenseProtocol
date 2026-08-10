using System.Collections.Generic;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击执行链到视觉运行时状态的桥接器。
    /// 它只发布当轮动态执行标识，不参与视觉静态投影构建。
    /// </summary>
    internal static class AttackExecutionVisualRuntimeBridge
    {
        /// <summary>
        /// 根据远程执行上下文发布视觉动态状态。
        /// </summary>
        internal static void Publish(RangedAttackExecutionContext context)
        {
            if (context == null)
            {
                return;
            }

            PublishCore(
                context.Pawn,
                context.ProjectionVersion,
                context.ProtocolResult?.Entry != null
                    ? context.ProtocolResult.Entry.AttackInstanceId
                    : context.Step != null ? context.Step.AttackInstanceId : null,
                context.HostResultId,
                CollectCastResultIds(context.Step),
                CollectEmitSourceResultIds(context.Step));
        }

        /// <summary>
        /// 根据近战执行上下文发布视觉动态状态。
        /// </summary>
        internal static void Publish(MeleeAttackExecutionContext context)
        {
            if (context == null)
            {
                return;
            }

            List<string> castResultIds = new List<string>();
            if (context.Result != null && !string.IsNullOrWhiteSpace(context.Result.Id))
            {
                castResultIds.Add(context.Result.Id);
            }

            PublishCore(
                context.Pawn,
                context.ProjectionVersion,
                context.Cast != null ? context.Cast.AttackInstanceId : null,
                context.Result != null ? context.Result.Id : null,
                castResultIds,
                castResultIds);
        }

        /// <summary>
        /// 清理指定会话持有的视觉动态执行态。
        /// </summary>
        internal static void Clear(Pawn pawn, AttackSessionToken token)
        {
            if (pawn == null || token == null)
            {
                return;
            }

            TriggerVisualRuntimeStateOwner owner = ResolveOwner(pawn);
            owner?.ClearExecutionState(token.AttackInstanceId, token.ProjectionVersion);
        }

        /// <summary>
        /// 发布视觉动态状态到 Trigger owner。
        /// </summary>
        private static void PublishCore(
            Pawn pawn,
            int projectionVersion,
            string attackInstanceId,
            string activeHostResultId,
            IReadOnlyList<string> activeCastResultIds,
            IReadOnlyList<string> activeEmitSourceResultIds)
        {
            if (pawn == null || projectionVersion <= 0)
            {
                return;
            }

            TriggerVisualRuntimeStateOwner owner = ResolveOwner(pawn);
            owner?.PublishExecutionState(
                projectionVersion,
                attackInstanceId,
                activeHostResultId,
                activeCastResultIds,
                activeEmitSourceResultIds);
        }

        /// <summary>
        /// 收集当前运行时动作步涉及的 cast 结果标识。
        /// </summary>
        private static IReadOnlyList<string> CollectCastResultIds(AttackRuntimeStep step)
        {
            List<string> result = new List<string>();
            if (step?.Casts == null)
            {
                return result;
            }

            for (int i = 0; i < step.Casts.Count; i++)
            {
                AttackExecutionCast cast = step.Casts[i];
                if (cast != null && !string.IsNullOrWhiteSpace(cast.ResultId))
                {
                    result.Add(cast.ResultId);
                }
            }

            return result;
        }

        /// <summary>
        /// 收集当前运行时动作步涉及的 emit 源结果标识。
        /// </summary>
        private static IReadOnlyList<string> CollectEmitSourceResultIds(AttackRuntimeStep step)
        {
            List<string> result = new List<string>();
            if (step?.Emits == null)
            {
                return result;
            }

            for (int i = 0; i < step.Emits.Count; i++)
            {
                AttackExecutionEmit emit = step.Emits[i];
                if (emit != null && !string.IsNullOrWhiteSpace(emit.SourceResultId))
                {
                    result.Add(emit.SourceResultId);
                }
            }

            return result;
        }

        /// <summary>
        /// 从 Pawn 当前主装备解析视觉运行时状态 owner。
        /// </summary>
        private static TriggerVisualRuntimeStateOwner ResolveOwner(Pawn pawn)
        {
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            return triggerBody != null && triggerBody.RuntimeServices != null
                ? triggerBody.RuntimeServices.TriggerVisualRuntimeStateOwner
                : null;
        }
    }
}
