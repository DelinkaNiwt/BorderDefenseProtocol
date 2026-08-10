using Verse;

namespace BDP.Core.CombatBody.Wounds
{
    /// <summary>
    /// 战斗体伤口 Trion 流失提示组件配置。
    /// 该配置没有玩法字段，只负责把 HediffDef 接到显示组件。
    /// </summary>
    public sealed class CombatBodyWoundTrionInfoHediffCompProperties : HediffCompProperties
    {
        /// <summary>
        /// 绑定到战斗体伤口 Trion 流失提示组件。
        /// </summary>
        public CombatBodyWoundTrionInfoHediffCompProperties()
        {
            compClass = typeof(CombatBodyWoundTrionInfoHediffComp);
        }
    }
}
