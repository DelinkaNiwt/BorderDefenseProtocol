using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达结果的来源追踪。
    /// 它只用于少数需要回到来源槽位执行事务的流程，不参与战斗语义计算。
    /// </summary>
    internal sealed class ExpressionSourceReference : IExposable
    {
        /// <summary>
        /// 来源芯片实例的 ThingID。
        /// </summary>
        public string ChipThingId { get; set; }

        /// <summary>
        /// 来源芯片定义名。
        /// </summary>
        public string ChipDefName { get; set; }

        /// <summary>
        /// 来源槽位所在侧别。
        /// </summary>
        public TriggerSide Side { get; set; }

        /// <summary>
        /// 来源槽位序号。
        /// </summary>
        public int SlotIndex { get; set; }

        /// <summary>
        /// 存读档来源追踪。
        /// </summary>
        public void ExposeData()
        {
            string chipThingId = ChipThingId;
            string chipDefName = ChipDefName;
            TriggerSide side = Side;
            int slotIndex = SlotIndex;
            Scribe_Values.Look(ref chipThingId, "chipThingId");
            Scribe_Values.Look(ref chipDefName, "chipDefName");
            Scribe_Values.Look(ref side, "side", TriggerSide.Main);
            Scribe_Values.Look(ref slotIndex, "slotIndex", 0);
            ChipThingId = chipThingId;
            ChipDefName = chipDefName;
            Side = side;
            SlotIndex = slotIndex;
        }
    }
}
