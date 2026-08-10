using System.Collections.Generic;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击计划中的单个执行组。
    /// 它描述一个执行窗口内应如何组织这一批最小攻击动作。
    /// 它只承担编排与推进边界，不持有 emit 级发射真值。
    /// </summary>
    internal sealed class AttackExecutionGroup
    {
        /// <summary>
        /// 当前执行组所属的攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前执行组的顺序编号。
        /// </summary>
        public int GroupIndex { get; set; }

        /// <summary>
        /// 当前执行组的时机模式。
        /// </summary>
        public AttackGroupTimingMode TimingMode { get; set; }

        /// <summary>
        /// 当前执行组的正式落地方式。
        /// 它区分“走效果层直发”还是“走 Verb 会话层落地”。
        /// </summary>
        public AttackGroupExecutionKind ExecutionKind { get; set; }

        /// <summary>
        /// 当前执行组包含的施放动作集合。
        /// </summary>
        public IReadOnlyList<AttackExecutionCast> Casts { get; set; }

        /// <summary>
        /// 当前执行组完成后，到下一个执行组建议等待多少 tick。
        /// </summary>
        public int DelayAfterGroupTicks { get; set; }
    }
}
