using BDP.Core.Trigger;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 已发布表达结果的公开来源槽位引用。
    /// 它只提供外部业务执行事务所需的稳定身份，不暴露内部表达对象。
    /// </summary>
    public sealed class ExpressionPublishedSourceReference
    {
        /// <summary>
        /// 供 Core 快照构建器使用的空构造。
        /// </summary>
        internal ExpressionPublishedSourceReference()
        {
        }

        /// <summary>
        /// 用稳定来源身份构造公开引用。
        /// </summary>
        public ExpressionPublishedSourceReference(
            string chipThingId,
            string chipDefName,
            TriggerSide side,
            int slotIndex)
        {
            ChipThingId = chipThingId;
            ChipDefName = chipDefName;
            Side = side;
            SlotIndex = slotIndex;
        }

        /// <summary>
        /// 来源芯片实例 ThingID。
        /// </summary>
        public string ChipThingId { get; internal set; }

        /// <summary>
        /// 来源芯片 DefName。
        /// </summary>
        public string ChipDefName { get; internal set; }

        /// <summary>
        /// 来源槽位所属侧别。
        /// </summary>
        public TriggerSide Side { get; internal set; }

        /// <summary>
        /// 来源槽位序号。
        /// </summary>
        public int SlotIndex { get; internal set; }
    }
}
