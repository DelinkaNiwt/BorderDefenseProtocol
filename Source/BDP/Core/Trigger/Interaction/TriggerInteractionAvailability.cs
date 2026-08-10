namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 交互语义结果的可用性级别。
    /// </summary>
    public enum TriggerInteractionAvailability
    {
        // 当前动作可作为正式请求提交。
        Available,
        // 当前结果只用于展示解释，不应直接提交动作。
        InformationalOnly,
        // 当前动作被正式规则阻塞。
        Blocked
    }
}
