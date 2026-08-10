namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 一轮目标交互输入驱动后的统一结果。
    /// 它只描述主循环下一步怎么走，不承载具体业务解释。
    /// </summary>
    internal sealed class TargetingInteractionDriveResult
    {
        /// <summary>
        /// 当前这轮输入对应的已裁定记录。
        /// </summary>
        public TargetingRecord TargetingRecord { get; set; }

        /// <summary>
        /// 当前主循环是否继续保留在目标交互中。
        /// </summary>
        public bool KeepTargeting { get; set; }

        /// <summary>
        /// 当前主循环是否允许进入确认冻结。
        /// </summary>
        public bool EnterConfirm { get; set; }

        /// <summary>
        /// 当前主循环是否应结束目标交互。
        /// </summary>
        public bool CancelTargeting { get; set; }

        /// <summary>
        /// 当前驱动结果附带的提示或拒绝原因。
        /// </summary>
        public string FeedbackMessage { get; set; }
    }
}
