using RimWorld;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// `Targeting（瞄准阶段）` 的中性段合法性请求。
    /// 它只描述“从哪个格子到哪个候选目标”这一段输入事实，不携带任何业务语义，也不等同于最终确认结论。
    /// </summary>
    public sealed class TargetingSegmentLegalityRequest
    {
        /// <summary>
        /// 当前目标交互所属 Pawn。
        /// </summary>
        public Pawn Pawn { get; set; }

        /// <summary>
        /// 当前目标交互借用的原版 Verb。
        /// </summary>
        public Verb Verb { get; set; }

        /// <summary>
        /// 当前目标交互使用的原版目标参数。
        /// </summary>
        public TargetingParameters TargetingParameters { get; set; }

        /// <summary>
        /// 当前这段合法性查询的起点格。
        /// </summary>
        public IntVec3 OriginCell { get; set; }

        /// <summary>
        /// 当前这段合法性查询的候选目标。
        /// </summary>
        public LocalTargetInfo CandidateTarget { get; set; }

        /// <summary>
        /// 当前是否要求这一段在此刻立即可成立。
        /// </summary>
        public bool RequireHittableNow { get; set; } = true;

        /// <summary>
        /// 从当前 `TargetingRecord（瞄准阶段记录）` 构造一份中性段合法性请求。
        /// 这个请求只表达单段事实，不把模块的最终确认责任提前上提到主模组。
        /// </summary>
        public static TargetingSegmentLegalityRequest FromRecord(
            TargetingRecord record,
            IntVec3 originCell,
            LocalTargetInfo candidateTarget,
            bool requireHittableNow = true)
        {
            return new TargetingSegmentLegalityRequest
            {
                Pawn = record != null ? record.Pawn : null,
                Verb = record != null ? record.Verb : null,
                TargetingParameters = record != null ? record.TargetingParameters : null,
                OriginCell = originCell,
                CandidateTarget = candidateTarget,
                RequireHittableNow = requireHittableNow
            };
        }
    }
}
