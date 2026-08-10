using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 芯片仓内部容器组件配置。
    /// </summary>
    public sealed class CompProperties_ChipContainer : CompProperties
    {
        /// <summary>
        /// 芯片仓最多持有的芯片数量。
        /// </summary>
        public int maxCapacity = 12;

        /// <summary>
        /// 构造芯片仓内部容器组件配置。
        /// </summary>
        public CompProperties_ChipContainer()
        {
            compClass = typeof(CompChipContainer);
        }
    }
}
