using System.Collections.Generic;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 一次正式攻击请求生成的高层执行编排。
    /// 它只描述“这次执行该如何组织”，不直接等于运行时真正消费的动作步。
    /// </summary>
    internal sealed class AttackExecutionPlan
    {
        /// <summary>
        /// 当前计划对应的原始请求。
        /// </summary>
        public AttackExecutionRequest Request { get; set; }

        /// <summary>
        /// 当前编排采用的推进方式。
        /// </summary>
        public AttackDriveMode DriveMode { get; set; }

        /// <summary>
        /// 当前计划包含的执行组集合。
        /// 组号从 0 开始，组与组之间按顺序推进。
        /// </summary>
        public IReadOnlyList<AttackExecutionGroup> Groups { get; set; }

        /// <summary>
        /// 当前计划包含的扁平施放动作集合。
        /// 它仍然属于计划层展开结果，不直接等于运行时动作步。
        /// </summary>
        public IReadOnlyList<AttackExecutionCast> Casts { get; set; }

        /// <summary>
        /// 当前计划涉及到的正式结果标识集合。
        /// 单条计划通常只有一个，双侧复合计划会包含多个来源结果。
        /// </summary>
        public IReadOnlyList<string> InvolvedResultIds { get; set; }

        /// <summary>
        /// 当前编排包含多少个执行组。
        /// 该值应与 Groups.Count 保持一致。
        /// </summary>
        public int GroupCount { get; set; }

        /// <summary>
        /// 当前计划绑定的攻击实例标识。
        /// 它只服务跨层追踪，不参与攻击语义判断。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 读取当前编排的首个执行组。
        /// 当前阶段新的计划执行器应优先从这里开始推进。
        /// </summary>
        public AttackExecutionGroup PrimaryGroup
        {
            get
            {
                return Groups != null && Groups.Count > 0 ? Groups[0] : null;
            }
        }

        /// <summary>
        /// 读取当前编排的首个施放动作。
        /// 它只回答“计划从哪个 cast 开始”，不代表后续运行时必须把这条 cast 视为整个会话宿主。
        /// </summary>
        public AttackExecutionCast PrimaryCast
        {
            get
            {
                return Casts != null && Casts.Count > 0 ? Casts[0] : null;
            }
        }
    }
}
