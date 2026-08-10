using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Thing
{
    /// <summary>把 Content 成品芯片组件挂到唯一成品 ThingDef。</summary>
    public sealed class CompProperties_ManufacturedChip : CompProperties
    {
        /// <summary>声明当前属性对应的组件类型。</summary>
        public CompProperties_ManufacturedChip()
        {
            compClass = typeof(CompManufacturedChip);
        }
    }
}
