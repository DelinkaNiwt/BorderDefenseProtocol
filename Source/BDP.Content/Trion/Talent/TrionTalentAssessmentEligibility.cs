using BDP.Core.Genes;
using BDP.Core.Trion;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Talent
{
    /// <summary>
    /// 所有检测设备共用的操作员与受检者资格规则。
    /// </summary>
    public static class TrionTalentAssessmentEligibility
    {
        /// <summary>判断角色是否属于玩家可安排进入固定检测仪的群体。</summary>
        public static bool IsPlayerControlledSubject(Pawn subject)
        {
            return subject != null
                && (subject.IsColonist || subject.IsSlaveOfColony || subject.IsPrisonerOfColony);
        }

        /// <summary>检查不依赖具体操作员的受检者资格。</summary>
        public static TrionTalentAssessmentResult CanSelectSubject(Pawn subject)
        {
            if (subject == null)
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_MissingSubject".Translate());
            }

            if (subject.Dead)
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_DeadSubject".Translate());
            }

            if (!IsPlayerControlledSubject(subject))
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_PlayerSubjectOnly".Translate());
            }

            if (!subject.RaceProps.Humanlike)
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_HumanlikeOnly".Translate());
            }

            if (TrionGlandEligibility.HasActiveTrionGland(subject))
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_HasGland".Translate());
            }

            ITrionReader reader = TrionSurfaceAccess.ResolveReader(subject);
            if (reader == null || reader.TrionCapacityPotential <= 0)
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_NoPotential".Translate());
            }

            CompTrionTalentAssessment assessment = subject.GetComp<CompTrionTalentAssessment>();
            if (assessment == null)
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_MissingState".Translate());
            }

            if (assessment.IsCompleted)
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_AlreadyCompleted".Translate());
            }

            return TrionTalentAssessmentResult.Eligible();
        }

        /// <summary>
        /// 执行无副作用资格检查。
        /// </summary>
        public static TrionTalentAssessmentResult CanAssess(Pawn operatorPawn, Pawn subject)
        {
            if (operatorPawn == null || subject == null)
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_MissingOperatorOrSubject".Translate());
            }

            if (operatorPawn == subject)
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_SelfAssessment".Translate());
            }

            if (operatorPawn.Dead)
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_DeadOperator".Translate());
            }

            SkillRecord intellectual = operatorPawn.skills?.GetSkill(SkillDefOf.Intellectual);
            if (intellectual == null || intellectual.TotallyDisabled || intellectual.Level < 10)
            {
                return TrionTalentAssessmentResult.Fail("BDP_Message_TrionTalent_IntellectualRequired".Translate());
            }

            return CanSelectSubject(subject);
        }
    }
}
