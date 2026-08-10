using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 一次瞄准过程绑定的目标交互会话。
    /// 它负责承载上游输入过程的中性状态，而不解释业务内容。
    /// </summary>
    public sealed class TargetingInteractionSession : IAttackContextNode
    {
        /// <summary>
        /// 当前交互是否已经激活。
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 当前交互推进到的步骤序号。
        /// </summary>
        public int StepIndex { get; set; }

        /// <summary>
        /// 当前交互是否已经完成。
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 当前交互是否已经取消。
        /// </summary>
        public bool IsCanceled { get; set; }

        /// <summary>
        /// 复制当前交互会话节点。
        /// 冻结攻击上下文时使用复制体，避免后续继续共享运行时引用。
        /// </summary>
        public IAttackContextNode Clone()
        {
            return new TargetingInteractionSession
            {
                IsActive = IsActive,
                StepIndex = StepIndex,
                IsCompleted = IsCompleted,
                IsCanceled = IsCanceled
            };
        }

        /// <summary>
        /// 把当前会话复位到激活状态。
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            IsCompleted = false;
            IsCanceled = false;
        }

        /// <summary>
        /// 把当前会话标记为完成。
        /// </summary>
        public void Complete()
        {
            IsActive = false;
            IsCompleted = true;
            IsCanceled = false;
        }

        /// <summary>
        /// 把当前会话标记为取消。
        /// </summary>
        public void Cancel()
        {
            IsActive = false;
            IsCompleted = false;
            IsCanceled = true;
        }

        /// <summary>
        /// 序列化当前交互会话节点。
        /// </summary>
        public void ExposeData()
        {
            bool isActive = IsActive;
            int stepIndex = StepIndex;
            bool isCompleted = IsCompleted;
            bool isCanceled = IsCanceled;

            Scribe_Values.Look(ref isActive, "isActive", false);
            Scribe_Values.Look(ref stepIndex, "stepIndex", 0);
            Scribe_Values.Look(ref isCompleted, "isCompleted", false);
            Scribe_Values.Look(ref isCanceled, "isCanceled", false);

            IsActive = isActive;
            StepIndex = stepIndex;
            IsCompleted = isCompleted;
            IsCanceled = isCanceled;
        }
    }
}
