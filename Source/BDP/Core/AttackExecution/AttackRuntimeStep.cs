using System.Collections.Generic;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 运行时动作步。
    /// 它描述“引擎下一次真正要消费的攻击动作单位”，不回收表达真值，也不替代高层计划模型。
    /// </summary>
    internal sealed class AttackRuntimeStep
    {
        /// <summary>
        /// 当前动作步所属的攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前动作步所属的执行组编号。
        /// </summary>
        public int GroupIndex { get; set; }

        /// <summary>
        /// 当前动作步在整轮运行时步骤中的顺序编号。
        /// </summary>
        public int StepIndex { get; set; }

        /// <summary>
        /// 当前动作步对应的武器模式。
        /// </summary>
        public WeaponExpressionMode WeaponMode { get; set; }

        /// <summary>
        /// 当前动作步的运行时落地方式。
        /// </summary>
        public AttackGroupExecutionKind ExecutionKind { get; set; }

        /// <summary>
        /// 当前动作步挂靠的宿主结果标识。
        /// 它回答“这一步由哪条宿主结果承接会话”。
        /// </summary>
        public string HostResultId { get; set; }

        /// <summary>
        /// 当前动作步实际要命中的目标。
        /// </summary>
        public LocalTargetInfo Target { get; set; }

        /// <summary>
        /// 当前动作步归并后的计划层 cast 集合。
        /// 它只服务回溯与诊断，不要求运行时逐条消费。
        /// </summary>
        public IReadOnlyList<AttackExecutionCast> Casts { get; set; }

        /// <summary>
        /// 当前动作步真正要落地的 emit 集合。
        /// 运行时宿主最终只消费这里。
        /// </summary>
        public IReadOnlyList<AttackExecutionEmit> Emits { get; set; }

        /// <summary>
        /// 当前动作步完成后，到下一步建议等待多少 tick。
        /// </summary>
        public int IntervalTicksAfter { get; set; }

        /// <summary>
        /// 当前动作步是否属于本次请求主入口选中的主动作步。
        /// </summary>
        public bool IsPrimarySelection { get; set; }
    }
}
