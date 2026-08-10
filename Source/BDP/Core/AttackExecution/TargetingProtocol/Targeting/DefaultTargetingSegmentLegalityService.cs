using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 默认 `Targeting（瞄准阶段）` 段合法性查询服务。
    /// 它只复用当前 `Verb（动词）` 与原版目标参数，给出 `segment-only verdict（仅单段裁定）`，不接管最终确认责任。
    /// </summary>
    public sealed class DefaultTargetingSegmentLegalityService : ITargetingSegmentLegalityService
    {
        /// <summary>
        /// 默认服务实例。
        /// </summary>
        public static readonly DefaultTargetingSegmentLegalityService Instance = new DefaultTargetingSegmentLegalityService();

        /// <summary>
        /// 查询当前这一段是否合法。
        /// 这里返回的只是 `segment-only verdict（仅单段裁定）`，不回答“这次攻击最终能否正式落地”。
        /// </summary>
        public TargetingSegmentLegalityResult Evaluate(TargetingSegmentLegalityRequest request)
        {
            if (request == null)
            {
                return TargetingSegmentLegalityResult.Reject("bdp_targeting_segment_request_missing");
            }

            if (request.Verb == null)
            {
                return TargetingSegmentLegalityResult.Reject("bdp_targeting_segment_verb_missing");
            }

            if (!request.CandidateTarget.IsValid)
            {
                return TargetingSegmentLegalityResult.Reject("bdp_targeting_segment_target_invalid");
            }

            Map map = request.Pawn != null ? request.Pawn.Map : request.Verb.Caster?.Map;
            if (map == null)
            {
                return TargetingSegmentLegalityResult.Reject("bdp_targeting_segment_map_missing");
            }

            if (request.TargetingParameters != null
                && !request.TargetingParameters.CanTarget(request.CandidateTarget.ToTargetInfo(map), request.Verb))
            {
                return TargetingSegmentLegalityResult.Reject("bdp_targeting_segment_target_rejected");
            }

            if (request.RequireHittableNow && !request.Verb.CanHitTargetFrom(request.OriginCell, request.CandidateTarget))
            {
                return TargetingSegmentLegalityResult.Reject("bdp_targeting_segment_cannot_hit");
            }

            return TargetingSegmentLegalityResult.Legal();
        }
    }
}
