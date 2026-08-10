using BDP.Content.Trion.Talent.Jobs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Trion.Talent.WorkGivers
{
    /// <summary>
    /// 在研究工作中寻找已经装入受检者、等待操作的固定 Trion 天赋检测仪。
    /// </summary>
    public sealed class WorkGiver_OperateTrionDetector : WorkGiver_Scanner
    {
        /// <summary>固定检测仪定义；延迟查询以等待 Def 加载完成。</summary>
        private static ThingDef TrionDetectorDef
        {
            get { return DefDatabase<ThingDef>.GetNamed("BDP_TrionDetector"); }
        }

        /// <summary>操作员从建筑交互格执行检测。</summary>
        public override PathEndMode PathEndMode
        {
            get { return PathEndMode.InteractionCell; }
        }

        /// <summary>只扫描固定 Trion 天赋检测仪。</summary>
        public override ThingRequest PotentialWorkThingRequest
        {
            get { return ThingRequest.ForDef(TrionDetectorDef); }
        }

        /// <summary>执行智识硬门槛、建筑状态、预留与可达检查。</summary>
        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            SkillRecord intellectual = pawn.skills?.GetSkill(SkillDefOf.Intellectual);
            if (intellectual == null || intellectual.TotallyDisabled || intellectual.Level < 10)
            {
                return false;
            }

            Building_TrionDetector detector = thing as Building_TrionDetector;
            if (detector == null || !detector.CanBeOperatedBy(pawn))
            {
                return false;
            }

            return pawn.CanReserveAndReach(
                detector,
                PathEndMode.InteractionCell,
                Danger.Some,
                1,
                -1,
                null,
                forced);
        }

        /// <summary>为合格研究员创建固定检测仪操作工作。</summary>
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            return JobMaker.MakeJob(TrionTalentAssessmentJobDefOf.BDP_OperateTrionDetector, thing);
        }
    }
}
