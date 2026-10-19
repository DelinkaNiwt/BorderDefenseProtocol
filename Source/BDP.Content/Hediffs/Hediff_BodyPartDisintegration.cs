using Verse;

namespace BDP.Content.Hediffs
{
    /// <summary>
    /// 身体部位裂解标记的玩家可见 Hediff（健康状态）表现。
    /// </summary>
    public sealed class Hediff_BodyPartDisintegration : Hediff
    {
        /// <summary>
        /// 在健康面板标签后显示当前严重度百分比。
        /// </summary>
        public override string LabelBase
        {
            get
            {
                return HediffLabelUtility.AppendSeverityPercent(
                    base.LabelBase,
                    Severity);
            }
        }
    }
}
