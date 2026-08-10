using System.Collections.Generic;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// Targeting 阶段的中性扩展输入状态。
    /// 它只描述输入进度事实，不携带任何具体业务语义。
    /// </summary>
    public sealed class TargetingInputState : IAttackContextNode
    {
        /// <summary>
        /// 当前是否已经进入扩展输入流程。
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 当前输入流程所在的步骤序号。
        /// </summary>
        public int StepIndex { get; set; }

        /// <summary>
        /// 当前扩展输入是否已经完成。
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// 当前输入状态附带的中性标签。
        /// </summary>
        public List<string> Tags { get; } = new List<string>();

        /// <summary>
        /// 复制当前输入状态节点。
        /// 冻结攻击上下文时使用复制体，避免后续继续共享运行时引用。
        /// </summary>
        public IAttackContextNode Clone()
        {
            TargetingInputState clone = new TargetingInputState
            {
                IsActive = IsActive,
                StepIndex = StepIndex,
                IsComplete = IsComplete
            };

            clone.Tags.AddRange(Tags);
            return clone;
        }

        /// <summary>
        /// 序列化当前输入状态节点。
        /// </summary>
        public void ExposeData()
        {
            bool isActive = IsActive;
            int stepIndex = StepIndex;
            bool isComplete = IsComplete;
            List<string> tags = Tags;

            Scribe_Values.Look(ref isActive, "isActive", false);
            Scribe_Values.Look(ref stepIndex, "stepIndex", 0);
            Scribe_Values.Look(ref isComplete, "isComplete", false);
            Scribe_Collections.Look(ref tags, "tags", LookMode.Value);

            IsActive = isActive;
            StepIndex = stepIndex;
            IsComplete = isComplete;
            Tags.Clear();
            if (tags != null)
            {
                Tags.AddRange(tags);
            }
        }
    }
}
