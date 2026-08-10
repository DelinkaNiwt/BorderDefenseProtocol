using BDP.Core.Trion;
using BDP.Content.Trion.Talent.Capacity;
using Verse;

namespace BDP.Content.Trion.Talent
{
    /// <summary>
    /// Trion 天赋检测的唯一正式提交服务。
    /// </summary>
    public sealed class TrionTalentAssessmentService
    {
        /// <summary>共享服务实例。</summary>
        public static readonly TrionTalentAssessmentService Instance = new TrionTalentAssessmentService();

        /// <summary>禁止外部创建重复服务。</summary>
        private TrionTalentAssessmentService()
        {
        }

        /// <summary>执行无副作用资格检查。</summary>
        public TrionTalentAssessmentResult CanAssess(Pawn operatorPawn, Pawn subject)
        {
            return TrionTalentAssessmentEligibility.CanAssess(operatorPawn, subject);
        }

        /// <summary>
        /// 完成前重新验证，并原子写入永久检测记录。
        /// </summary>
        public TrionTalentAssessmentResult TryCommit(Pawn operatorPawn, Pawn subject)
        {
            TrionTalentAssessmentResult eligibility = CanAssess(operatorPawn, subject);
            if (!eligibility.Succeeded)
            {
                return eligibility;
            }

            ITrionReader reader = TrionSurfaceAccess.ResolveReader(subject);
            if (reader == null)
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_CommitFailed".Translate());
            }

            TrionCapacityPotentialBandDef band = TrionCapacityPotentialBandResolver.Instance.Resolve(reader.TrionCapacityPotential);
            if (band == null)
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_InvalidBand".Translate());
            }

            CompTrionTalentAssessment assessment = subject.GetComp<CompTrionTalentAssessment>();
            if (assessment == null || !assessment.TryMarkCompleted())
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_CommitFailed".Translate());
            }

            return TrionTalentAssessmentResult.Success(band, reader.InnateTrionIntensity);
        }
    }
}
