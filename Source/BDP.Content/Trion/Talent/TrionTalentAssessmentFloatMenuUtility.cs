using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Trion.Talent
{
    /// <summary>
    /// 一次性便携检测仪使用的原版右键菜单与目标选择入口。
    /// </summary>
    public static class TrionTalentAssessmentFloatMenuUtility
    {
        /// <summary>正式操作员工作定义名称。</summary>
        private const string AssessmentJobDefName = "BDP_TrionTalentAssessment";

        /// <summary>
        /// 按操作员和设备状态构造右键菜单。
        /// </summary>
        public static IEnumerable<FloatMenuOption> BuildOptions(
            Pawn operatorPawn,
            Thing_TrionPortableDetector device)
        {
            string rejection = GetDeviceOrOperatorRejection(operatorPawn, device);
            if (rejection != null)
            {
                yield return new FloatMenuOption(
                    "BDP_Command_TrionTalent_StartRejected".Translate(rejection),
                    null);
                yield break;
            }

            yield return new FloatMenuOption("BDP_Command_TrionTalent_Start".Translate(), delegate
            {
                BeginTargeting(operatorPawn, device);
            });
        }

        /// <summary>
        /// 使用原版 Targeter 让玩家选择受检者。
        /// </summary>
        private static void BeginTargeting(
            Pawn operatorPawn,
            Thing_TrionPortableDetector device)
        {
            TargetingParameters parameters = TargetingParameters.ForPawns();
            Find.Targeter.BeginTargeting(
                parameters,
                target =>
                {
                    Pawn subject = target.Pawn;
                    TrionTalentAssessmentResult result = TrionTalentAssessmentService.Instance.CanAssess(operatorPawn, subject);
                    if (!result.Succeeded)
                    {
                        Messages.Message(result.Message, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(AssessmentJobDefName);
                    if (jobDef == null)
                    {
                        Messages.Message("BDP_Message_TrionTalent_MissingWorkDefinition".Translate(), MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    Job job = JobMaker.MakeJob(jobDef, device, subject);
                    operatorPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                },
                target =>
                {
                    Pawn subject = target.Pawn;
                    if (subject != null)
                    {
                        GenDraw.DrawTargetHighlight(subject);
                    }
                });
        }

        /// <summary>
        /// 检查不依赖受检者的入口条件。
        /// </summary>
        private static string GetDeviceOrOperatorRejection(
            Pawn operatorPawn,
            Thing_TrionPortableDetector device)
        {
            if (device == null || device.Destroyed || !device.Spawned)
            {
                return "BDP_Message_TrionTalent_DeviceUnavailable".Translate();
            }

            if (operatorPawn == null || !operatorPawn.CanReach(device, PathEndMode.Touch, Danger.Some))
            {
                return "BDP_Message_TrionTalent_Unreachable".Translate();
            }

            SkillRecord intellectual = operatorPawn.skills?.GetSkill(SkillDefOf.Intellectual);
            if (intellectual == null || intellectual.TotallyDisabled || intellectual.Level < 10)
            {
                return "BDP_Message_TrionTalent_IntellectualShort".Translate();
            }

            return null;
        }
    }
}
