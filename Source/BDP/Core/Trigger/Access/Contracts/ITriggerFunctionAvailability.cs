namespace BDP.Core.Trigger
{
    /// <summary>可选的宿主功能发布限制；不改变装配资格或槽位激活事实。</summary>
    public interface ITriggerFunctionAvailability
    {
        /// <summary>宿主当前是否允许触发体功能发布与执行。</summary>
        bool TriggerFunctionsAvailable { get; }
    }
}
