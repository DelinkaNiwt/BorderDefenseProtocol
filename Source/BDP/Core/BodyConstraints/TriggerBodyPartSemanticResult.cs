using BDP.Core.Trigger;

namespace BDP.Core.BodyConstraints
{
    /// <summary>
    /// Trigger 身体部位语义解析结果。
    /// 它只描述身体事实，不持有槽位、芯片或运行时状态。
    /// </summary>
    internal sealed class TriggerBodyPartSemanticResult
    {
        /// <summary>
        /// 构造一份身体部位语义解析结果。
        /// </summary>
        public TriggerBodyPartSemanticResult(bool isManipulationLimb, TriggerSide? resolvedSide)
        {
            IsManipulationLimb = isManipulationLimb;
            ResolvedSide = resolvedSide;
        }

        /// <summary>
        /// 当前部位是否属于可操作肢体链。
        /// </summary>
        public bool IsManipulationLimb { get; private set; }

        /// <summary>
        /// 当前部位解析出的 Trigger 侧别。
        /// Main 固定代表右侧，Sub 固定代表左侧。
        /// </summary>
        public TriggerSide? ResolvedSide { get; private set; }

        /// <summary>
        /// 当前结果是否足以禁用对应 Trigger 侧。
        /// </summary>
        public bool CanDisableTrigger
        {
            get
            {
                return IsManipulationLimb && ResolvedSide.HasValue;
            }
        }
    }
}

