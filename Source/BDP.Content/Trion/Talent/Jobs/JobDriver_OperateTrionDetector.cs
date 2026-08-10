using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Trion.Talent.Jobs
{
    /// <summary>
    /// 研究员在固定 Trion 天赋检测仪交互格执行检测。
    /// 操作员更换或断电只结束当前工作，不拥有也不清空建筑累计进度。
    /// </summary>
    public sealed class JobDriver_OperateTrionDetector : JobDriver
    {
        /// <summary>固定检测仪目标索引。</summary>
        private const TargetIndex DetectorIndex = TargetIndex.A;

        /// <summary>当前固定检测仪。</summary>
        private Building_TrionDetector Detector
        {
            get { return job.GetTarget(DetectorIndex).Thing as Building_TrionDetector; }
        }

        /// <summary>预留建筑和交互格，避免多个研究员同时操作。</summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Building_TrionDetector detector = Detector;
            if (detector == null || !pawn.Reserve(detector, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            return !detector.def.hasInteractionCell
                || pawn.ReserveSittableOrSpot(detector.InteractionCell, job, errorOnFailed);
        }

        /// <summary>前往交互格，并按研究速度持续写入建筑工作量。</summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(DetectorIndex);
            yield return Toils_Goto.GotoThing(DetectorIndex, PathEndMode.InteractionCell);

            Toil operate = ToilMaker.MakeToil("OperateTrionDetector");
            operate.tickIntervalAction = delegate(int delta)
            {
                Building_TrionDetector detector = Detector;
                Pawn actor = operate.actor;
                if (detector == null || !detector.CanBeOperatedBy(actor))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                float workAmount = actor.GetStatValue(StatDefOf.ResearchSpeed);
                workAmount *= detector.GetStatValue(StatDefOf.ResearchSpeedFactor);
                actor.skills.Learn(SkillDefOf.Intellectual, 0.1f * delta);
                actor.GainComfortFromCellIfPossible(delta, chairsOnly: true);

                if (detector.AddWork(workAmount * delta, actor))
                {
                    ReadyForNextToil();
                }
            };
            operate.FailOnCannotTouch(DetectorIndex, PathEndMode.InteractionCell);
            operate.WithEffect(EffecterDefOf.Research, DetectorIndex);
            operate.defaultCompleteMode = ToilCompleteMode.Never;
            operate.activeSkill = delegate { return SkillDefOf.Intellectual; };
            yield return operate;
        }
    }
}
