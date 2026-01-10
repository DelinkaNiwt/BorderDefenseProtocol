using Verse;

namespace ProjectTrion.Components
{
    /// <summary>
    /// Trion触发器组件的定义。
    /// 在XML中定义组件的属性（占用、消耗、功能等）。
    ///
    /// Definition of a Trion trigger component.
    /// Defines component properties in XML (reserved, consumption, function, etc.)
    /// </summary>
    public class TriggerMountDef : Def
    {
        /// <summary>
        /// 组件占用的Trion容量。
        /// 战斗体生成时从Available中扣除，战斗体解除时返还（主动）或流失（被动）。
        /// Trion capacity reserved by this component.
        /// Deducted from Available when combat body is generated, returned or lost when destroyed.
        /// </summary>
        public float reservedCost = 10f;

        /// <summary>
        /// 激活组件时的一次性Trion消耗。
        /// One-time Trion cost to activate this component.
        /// </summary>
        public float activationCost = 5f;

        /// <summary>
        /// 激活组件的导引时间（Tick数）。
        /// 在此期间内，组件不提供功能。
        /// Guidance/warm-up time for activation (in ticks).
        /// Component provides no function during this time.
        /// </summary>
        public int activationGuidanceTicks = 0;

        /// <summary>
        /// 组件激活后的持续消耗速率（每60Tick）。
        /// 仅在IsActive时产生消耗。
        /// Continuous consumption rate when active (per 60 ticks).
        /// Only applies when IsActive.
        /// </summary>
        public float consumptionRate = 0f;

        /// <summary>
        /// 组件的使用费用（每次使用）。
        /// 例如武器射击、护盾防御、能力释放等。
        /// 应由应用层在使用时主动调用Consume()。
        /// Usage cost per use (e.g., per shot, per block, per ability cast).
        /// Application layer should call Consume() when component is used.
        /// </summary>
        public float usageCost = 0f;

        /// <summary>
        /// 组件的详细描述。
        /// 显示在游戏内UI中，描述组件的功能。
        /// Component description shown in-game UI.
        /// </summary>
        public new string description = "";

        /// <summary>
        /// 是否可以堆叠（多个同样的组件）。
        /// Whether multiple instances of this component can be mounted.
        /// </summary>
        public bool canStack = true;

        /// <summary>
        /// 最多可以堆叠的数量。
        /// Maximum stack count if canStack is true.
        /// </summary>
        public int maxStackCount = 5;

        /// <summary>
        /// 组件所属的分类。
        /// Category of this component (e.g., weapon, utility, ability).
        /// </summary>
        public string category = "utility";

        /// <summary>
        /// 组件的Tier级别（用于平衡）。
        /// Tier level of this component for balance purposes.
        /// </summary>
        public int tier = 1;

        /// <summary>
        /// 是否在初始化战斗体时默认激活。
        /// Whether this component is activated by default when combat body is generated.
        /// </summary>
        public bool activateByDefault = false;
    }
}
