using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击执行链默认语义目标解析器。
    /// 它只负责从已经冻结的执行上下文里取出语义目标，不重猜任何业务模块规则。
    /// </summary>
    internal static class AttackExecutionSemanticTargetResolver
    {
        /// <summary>
        /// 根据当前准备上下文解析默认语义目标。
        /// 优先读取 `ConfirmedTargetSnapshot（已确认目标冻结快照）` 里的 `SemanticTarget（语义目标）`，
        /// 找不到时才回退到请求自身的 `Target（导航目标）`。
        /// </summary>
        /// <param name="request">已经通过准备阶段的攻击执行上下文。</param>
        /// <returns>当前执行链应该默认消费的语义目标；没有可用目标时返回 `LocalTargetInfo.Invalid`。</returns>
        public static LocalTargetInfo Resolve(AttackExecutionPreparedContext request)
        {
            ConfirmedTargetSnapshot confirmedTarget = request?.AttackContextSnapshot?.GetNode(AttackContextKeys.ConfirmedTarget) as ConfirmedTargetSnapshot;
            if (confirmedTarget != null && confirmedTarget.SemanticTarget.IsValid)
            {
                return confirmedTarget.SemanticTarget;
            }

            return request != null ? request.Target : LocalTargetInfo.Invalid;
        }
    }
}
