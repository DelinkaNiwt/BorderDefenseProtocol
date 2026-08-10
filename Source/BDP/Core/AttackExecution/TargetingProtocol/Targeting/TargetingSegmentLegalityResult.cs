namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// Targeting 阶段的中性段合法性结果。
    /// 它只回答是否合法，以及不合法时的拒绝原因。
    /// </summary>
    public sealed class TargetingSegmentLegalityResult
    {
        /// <summary>
        /// 当前段是否合法。
        /// </summary>
        public bool IsLegal { get; set; }

        /// <summary>
        /// 当前段不合法时的中性拒绝原因。
        /// </summary>
        public string RejectReason { get; set; }

        /// <summary>
        /// 构造合法结果。
        /// </summary>
        public static TargetingSegmentLegalityResult Legal()
        {
            return new TargetingSegmentLegalityResult
            {
                IsLegal = true
            };
        }

        /// <summary>
        /// 构造拒绝结果。
        /// </summary>
        public static TargetingSegmentLegalityResult Reject(string reason)
        {
            return new TargetingSegmentLegalityResult
            {
                IsLegal = false,
                RejectReason = reason
            };
        }
    }
}
