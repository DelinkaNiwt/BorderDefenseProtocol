using System.Collections.Generic;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 已确认的目标交互快照。
    /// 它服务执行段读取，不解释任何具体业务语义。
    /// </summary>
    public sealed class ConfirmedInteractionSnapshot : IAttackContextNode
    {
        /// <summary>
        /// 当前冻结快照对应的步骤序号。
        /// </summary>
        public int StepIndex { get; set; }

        /// <summary>
        /// 当前冻结快照是否已完成交互。
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// 当前冻结快照附带的中性标签。
        /// </summary>
        public List<string> Tags { get; } = new List<string>();

        /// <summary>
        /// 复制当前交互冻结快照。
        /// 冻结到攻击上下文时使用复制体，避免继续共享运行时引用。
        /// </summary>
        public IAttackContextNode Clone()
        {
            ConfirmedInteractionSnapshot clone = new ConfirmedInteractionSnapshot
            {
                StepIndex = StepIndex,
                IsComplete = IsComplete
            };

            clone.Tags.AddRange(Tags);
            return clone;
        }

        /// <summary>
        /// 统一序列化当前交互冻结快照。
        /// </summary>
        public void ExposeData()
        {
            int stepIndex = StepIndex;
            bool isComplete = IsComplete;
            List<string> tags = Tags;
            Scribe_Values.Look(ref stepIndex, "stepIndex", 0);
            Scribe_Values.Look(ref isComplete, "isComplete", false);
            Scribe_Collections.Look(ref tags, "tags", LookMode.Value);
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
