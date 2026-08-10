using BDP.Core.Trigger;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一条 Verb 结果对应的最小宿主来源键。
    /// 它只描述宿主来源事实，不持有运行时宿主对象。
    /// </summary>
    internal sealed class ExpressionVerbHostKey
    {
        /// <summary>
        /// 来源芯片的 ThingID。
        /// </summary>
        public string ChipThingId { get; set; }

        /// <summary>
        /// 来源所在的 Trigger 侧别。
        /// </summary>
        public TriggerSide Side { get; set; }

        /// <summary>
        /// 当前 Verb 对应的固定宿主入口。
        /// </summary>
        public ExpressionVerbHostSlot HostSlot { get; set; }

        /// <summary>
        /// 当前来源的形态键。
        /// </summary>
        public string ModeKey { get; set; }
    }
}
