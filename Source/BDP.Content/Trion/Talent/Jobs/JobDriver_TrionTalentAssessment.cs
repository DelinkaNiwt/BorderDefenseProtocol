using System.Collections.Generic;
using BDP.Content.Trion.Talent;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Trion.Talent.Jobs
{
    /// <summary>
    /// 操作员侧双人 Trion 天赋检测时序。
    /// </summary>
    public sealed class JobDriver_TrionTalentAssessment : JobDriver
    {
        /// <summary>设备目标。</summary>
        private const TargetIndex DeviceIndex = TargetIndex.A;

        /// <summary>受检者目标。</summary>
        private const TargetIndex SubjectIndex = TargetIndex.B;

        /// <summary>双方就位后的检测持续时间。</summary>
        private const int AssessmentTicks = 600;

        /// <summary>当前检测设备。</summary>
        private Thing_TrionPortableDetector Device
        {
            get { return job.GetTarget(DeviceIndex).Thing as Thing_TrionPortableDetector; }
        }

        /// <summary>当前受检者。</summary>
        private Pawn Subject
        {
            get { return job.GetTarget(SubjectIndex).Pawn; }
        }

        /// <summary>预留设备和受检者，避免并行重复检测。</summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(DeviceIndex), job, 1, 1, null, errorOnFailed)
                && pawn.Reserve(job.GetTarget(SubjectIndex), job, 1, -1, null, errorOnFailed);
        }

        /// <summary>编排双人到位、检测、提交和成功消耗。</summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(StopSubjectWaitingJob);
            this.FailOnDestroyedNullOrForbidden(SubjectIndex);
            this.FailOn(() => !DeviceStillUsable());

            yield return Toils_General.Do(delegate
            {
                TrionTalentAssessmentResult result = TrionTalentAssessmentService.Instance.CanAssess(pawn, Subject);
                if (!result.Succeeded || !StartSubjectWaitingJob())
                {
                    if (!result.Succeeded)
                    {
                        Messages.Message(result.Message, MessageTypeDefOf.RejectInput, false);
                    }

                    EndJobWith(JobCondition.Incompletable);
                }
            });

            yield return Toils_Goto.GotoThing(DeviceIndex, PathEndMode.Touch)
                .FailOnDespawnedNullOrForbidden(DeviceIndex);
            job.count = 1;
            yield return Toils_Haul.StartCarryThing(
                DeviceIndex,
                putRemainderInQueue: false,
                subtractNumTakenFromJobCount: false,
                failIfStackCountLessThanJobCount: true);
            yield return Toils_Goto.GotoThing(SubjectIndex, PathEndMode.Touch);

            int remainingTicks = AssessmentTicks;
            Toil assessment = ToilMaker.MakeToil("TrionTalentAssessmentWork");
            assessment.initAction = delegate
            {
                remainingTicks = AssessmentTicks;
                pawn.pather.StopDead();
            };
            assessment.tickAction = delegate
            {
                TrionTalentAssessmentResult current = TrionTalentAssessmentService.Instance.CanAssess(pawn, Subject);
                if (!current.Succeeded || !DeviceStillUsable())
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (!SubjectIsReady())
                {
                    return;
                }

                remainingTicks--;
                if (remainingTicks <= 0)
                {
                    ReadyForNextToil();
                }
            };
            assessment.defaultCompleteMode = ToilCompleteMode.Never;
            assessment.handlingFacing = true;
            yield return assessment;

            yield return Toils_General.Do(CommitAssessment);
        }

        /// <summary>给受检者下发独立等待工作。</summary>
        private bool StartSubjectWaitingJob()
        {
            Job waitJob = JobMaker.MakeJob(
                TrionTalentAssessmentJobDefOf.BDP_WaitForTrionTalentAssessment,
                Device,
                pawn);
            return Subject.jobs.TryTakeOrderedJob(waitJob, JobTag.Misc);
        }

        /// <summary>判断受检者是否已在正确工作位置配合等待。</summary>
        private bool SubjectIsReady()
        {
            if (Subject?.jobs?.curJob?.def != TrionTalentAssessmentJobDefOf.BDP_WaitForTrionTalentAssessment)
            {
                return false;
            }

            return Subject.Position.AdjacentTo8WayOrInside(pawn.Position);
        }

        /// <summary>检查设备存续与固定设备供电。</summary>
        private bool DeviceStillUsable()
        {
            Thing device = Device;
            if (device == null || device.Destroyed)
            {
                return false;
            }

            return true;
        }

        /// <summary>完成前重验并提交；便携设备仅在成功后消耗。</summary>
        private void CommitAssessment()
        {
            TrionTalentAssessmentResult eligibility = TrionTalentAssessmentService.Instance.CanAssess(pawn, Subject);
            if (!eligibility.Succeeded)
            {
                Messages.Message(eligibility.Message, MessageTypeDefOf.RejectInput, false);
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            TrionTalentAssessmentResult result = TrionTalentAssessmentService.Instance.TryCommit(pawn, Subject);
            if (!result.Succeeded)
            {
                Messages.Message(result.Message, MessageTypeDefOf.RejectInput, false);
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            Messages.Message(result.Message, Subject, MessageTypeDefOf.PositiveEvent, false);
            if (result.Succeeded && Device is Thing_TrionPortableDetector portable)
            {
                portable.Destroy(DestroyMode.Vanish);
            }
        }

        /// <summary>操作员工作无论如何退出，都结束受检者等待。</summary>
        private void StopSubjectWaitingJob(JobCondition condition)
        {
            if (Subject?.jobs?.curJob?.def == TrionTalentAssessmentJobDefOf.BDP_WaitForTrionTalentAssessment)
            {
                Subject.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
    }
}
