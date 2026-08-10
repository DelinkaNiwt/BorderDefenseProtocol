namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// Targeting 阶段的中性段合法性查询服务。
    /// 它只判断从一个起点到一个候选目标这一段是否按现有目标规则成立。
    /// </summary>
    public interface ITargetingSegmentLegalityService
    {
        /// <summary>
        /// 查询当前段是否合法。
        /// </summary>
        TargetingSegmentLegalityResult Evaluate(TargetingSegmentLegalityRequest request);
    }
}
