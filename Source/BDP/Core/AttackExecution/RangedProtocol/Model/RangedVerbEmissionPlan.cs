using System.Collections.Generic;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 交给 Verb 宿主消费的正式发射计划。
    /// 它把一次攻击会话中要依次发生的发射窗口按顺序收拢成正式对象。
    /// </summary>
    internal sealed class RangedVerbEmissionPlan
    {
        /// <summary>
        /// 当前宿主要顺序消费的正式发射窗口集合。
        /// </summary>
        public IReadOnlyList<RangedVerbEmissionWindowPlan> Windows { get; set; }

        /// <summary>
        /// 当前动作步所属的攻击实例标识。
        /// </summary>
        public string StepAttackInstanceId { get; set; }

        /// <summary>
        /// 当前动作步挂靠的宿主结果标识。
        /// </summary>
        public string StepHostResultId { get; set; }

        /// <summary>
        /// 当前动作步涉及到的来源结果标识集合。
        /// 这组标识只服务诊断与回溯，不反向驱动业务分支。
        /// </summary>
        public IReadOnlyList<string> StepSourceResultIds { get; set; }
        /// <summary>
        /// 当前整份正式发射计划按上游真值预期应落地的 emit 总量。
        /// 宿主消费完成后可用它判断是否完整消费。
        /// </summary>
        public int ExpectedEmitCount { get; set; }
    }
}
