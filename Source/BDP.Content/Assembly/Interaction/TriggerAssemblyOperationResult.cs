namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配操作结果。
    /// 它只用于窗口反馈，不参与 Trigger 装载真值保存。
    /// </summary>
    internal sealed class TriggerAssemblyOperationResult
    {
        /// <summary>
        /// 操作是否成功。
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// 机器可读的原因码。
        /// </summary>
        public string ReasonCode { get; private set; }

        /// <summary>
        /// 玩家可读的简短提示。
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// 构造操作结果。
        /// </summary>
        private TriggerAssemblyOperationResult(bool success, string reasonCode, string message)
        {
            Success = success;
            ReasonCode = reasonCode;
            Message = message;
        }

        /// <summary>
        /// 创建成功结果。
        /// </summary>
        internal static TriggerAssemblyOperationResult Ok(string reasonCode, string message)
        {
            return new TriggerAssemblyOperationResult(true, reasonCode, message);
        }

        /// <summary>
        /// 创建失败结果。
        /// </summary>
        internal static TriggerAssemblyOperationResult Fail(string reasonCode, string message)
        {
            return new TriggerAssemblyOperationResult(false, reasonCode, message);
        }
    }
}
