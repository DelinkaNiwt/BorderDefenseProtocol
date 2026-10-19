namespace BDP.Core.Information
{
    /// <summary>
    /// 功能 Def 接收一个属性的排序槽位。
    /// 槽位表示允许接收，不表示每个实现必须提供该属性。
    /// </summary>
    public sealed class ChipFeaturePropertySlot
    {
        /// <summary>被允许接收的属性 Def。</summary>
        public ChipFeaturePropertyDef Property;

        /// <summary>属性在该功能中的显示顺序。</summary>
        public int Order;
    }
}
