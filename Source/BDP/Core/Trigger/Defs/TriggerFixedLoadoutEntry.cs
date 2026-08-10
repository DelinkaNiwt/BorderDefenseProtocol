using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体首次生成时的一条芯片预装声明。
    /// 这是中性Core配置，不包含任何具体触发器类别语义。
    /// </summary>
    public sealed class TriggerFixedLoadoutEntry
    {
        /// <summary>
        /// 目标槽区；-1只作为XML漏填哨兵，合法配置必须填写Main、Sub或Special。
        /// </summary>
        public TriggerSide side = (TriggerSide)(-1);

        /// <summary>
        /// 面向作者的1-based槽位编号；运行时会转换为内部0-based索引。
        /// </summary>
        public int slotNumber;

        /// <summary>
        /// 要创建并装入目标槽位的芯片物品定义。
        /// </summary>
        public ThingDef chipDef;
    }
}
