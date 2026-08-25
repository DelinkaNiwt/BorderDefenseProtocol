using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.Debuff
{
    /// <summary>
    /// 铅块负重专用 Hediff（健康状态）。
    /// 只负责把当前严重度显示为与移动速度降低比例一致的标签后缀。
    /// </summary>
    public sealed class Hediff_LeadWeight : HediffWithComps
    {
        /// <summary>
        /// 返回带当前移动速度降低比例的玩家可见标签。
        /// 严重度 0.97 表示移动速度降低 97%，因此显示“铅块负重97%”。
        /// </summary>
        public override string LabelBase
        {
            get
            {
                return base.LabelBase + Mathf.Clamp01(Severity).ToStringPercent("0");
            }
        }
    }
}
