using BDP.Core.Expressions;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 从正式表达结果构造直视许可上下文的中性辅助入口。
    /// </summary>
    internal static class DirectTargetLineOfSightPermissionAccess
    {
        /// <summary>
        /// 按当前 Pawn 和正式表达结果即时查询是否可放宽直达目标视线。
        /// </summary>
        /// <param name="pawn">当前攻击 Pawn。</param>
        /// <param name="result">当前正式表达结果。</param>
        /// <returns>当前许可链允许放宽时返回 true。</returns>
        internal static bool ShouldRelaxDirectTargetLineOfSight(
            Pawn pawn,
            FormalExpressionResult result)
        {
            return ShouldRelaxDirectTargetLineOfSight(
                pawn,
                result,
                LocalTargetInfo.Invalid);
        }

        /// <summary>
        /// 按具体目标查询是否可放宽直达目标视线。
        /// 新的间接攻击许可链在这里执行目标屋顶后置条件；旧许可链继续保留兼容性。
        /// </summary>
        internal static bool ShouldRelaxDirectTargetLineOfSight(
            Pawn pawn,
            FormalExpressionResult result,
            LocalTargetInfo target)
        {
            if (pawn == null || result == null)
            {
                LogDecision(pawn, result, false, "missing_pawn_or_result");
                return false;
            }

            // 动态许可只补足静态声明为 Required 的表达；原本 NotRequired 的表达保持原行为。
            if (result.ResolvedVerbSpec == null
                || !result.ResolvedVerbSpec.RequiresDirectTargetLineOfSight)
            {
                LogDecision(pawn, result, false, "static_requirement_not_required_or_missing");
                return false;
            }

            MapPermissionContext context = new MapPermissionContext(
                pawn,
                result.Id,
                result.DeclaredPermissionKeys);
            bool allowed = DirectTargetLineOfSightPermissionResolverRegistry
                .ShouldRelaxDirectTargetLineOfSight(context);
            bool indirectAllowed = IndirectRangedAttackPermissionAccess.TryResolve(
                pawn,
                result,
                target,
                out _);
            allowed = allowed || indirectAllowed;
            LogDecision(pawn, result, allowed, "resolver_chain_completed");
            return allowed;
        }

        /// <summary>
        /// 记录一次动态直视许可访问结果，帮助区分结果缺失、静态要求和许可链失败。
        /// </summary>
        private static void LogDecision(
            Pawn pawn,
            FormalExpressionResult result,
            bool allowed,
            string stage)
        {
            string pawnId = pawn != null ? pawn.ThingID : "null";
            string resultId = result != null ? result.Id : "null";
            string declaredKeys = result != null && result.DeclaredPermissionKeys != null
                ? string.Join(",", result.DeclaredPermissionKeys)
                : "null";
            bool requiresDirectLos = result != null
                && result.ResolvedVerbSpec != null
                && result.ResolvedVerbSpec.RequiresDirectTargetLineOfSight;
            BdpDiagnostics.AttackExecutionThrottled(
                "direct_los.access." + pawnId + "." + resultId,
                "直视许可访问 | stage=" + stage
                + " | pawn=" + pawnId
                + " | result=" + resultId
                + " | requiresDirectLos=" + requiresDirectLos
                + " | declaredKeys=" + declaredKeys
                + " | allowed=" + allowed,
                30);
        }
    }
}
