namespace BDP.Core.Information
{
    /// <summary>
    /// 所属 Def 允许使用的信息组及其顺序。
    /// </summary>
    public sealed class ChipFeatureGroupSlot
    {
        /// <summary>允许使用的信息组 Def；所属方可以是功能或父信息组。</summary>
        public ChipFeatureGroupDef Group;

        /// <summary>信息组在当前功能中的显示顺序。</summary>
        public int Order;
    }
}
