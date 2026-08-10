namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 目标交互推进裁决。
    /// 它是当前输入帧经过模块处理后形成的正式推进结果。
    /// </summary>
    public sealed class TargetingAdvanceDecision
    {
        /// <summary>
        /// 当前推进结果种类。
        /// 默认继续收集，以兼容原版单步输入之外的扩展场景。
        /// </summary>
        public TargetingAdvanceKind Kind { get; set; } = TargetingAdvanceKind.Continue;

        /// <summary>
        /// 当前推进裁决是否允许进入确认冻结。
        /// </summary>
        public bool AllowsConfirm => Kind == TargetingAdvanceKind.Complete;

        /// <summary>
        /// 当前推进裁决是否要求取消本次交互。
        /// </summary>
        public bool IsCanceled => Kind == TargetingAdvanceKind.Cancel;

        /// <summary>
        /// 当前推进裁决是否拒绝这一轮输入。
        /// </summary>
        public bool IsRejected => Kind == TargetingAdvanceKind.Reject;

        /// <summary>
        /// 当前推进裁决附带的拒绝或提示原因。
        /// </summary>
        public string Reason { get; set; }
    }
}
