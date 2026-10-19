using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Trion.Facility.Jobs
{
    /// <summary>普通搬运者已经备齐供物，指定献祭者只负责前往核心和最终一次提交。</summary>
    public sealed class JobDriver_SacrificeToTrionFacility : JobDriver
    {
        /// <summary>本次执行针对的核心。</summary>
        private Building_TrionFacility Facility => job.targetA.Thing as Building_TrionFacility;

        /// <summary>只预留核心与交互格；供物已经在本核心原版容器中，不再预留地图堆栈。</summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Building_TrionFacility facility = Facility;
            return facility?.SacrificeComp?.CanFinish(pawn) == true
                && pawn.Reserve(facility, job, 1, -1, null, errorOnFailed)
                && pawn.ReserveSittableOrSpot(facility.InteractionCell, job, errorOnFailed);
        }

        /// <summary>取消准备、失去资格或供物缺失都会结束此任务，实物仍保留在核心中。</summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Facility?.SacrificeComp?.CanFinish(pawn) != true);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.Do(CommitSacrifice);
        }

        /// <summary>按实际容器内整批供物重验价值、权限与容量；成功后清除准备状态。</summary>
        private void CommitSacrifice()
        {
            string problem;
            if (!Facility.ControlComp.TrySacrifice(pawn, Facility.SacrificeComp.LoadedBatch(), out problem))
            {
                Messages.Message(problem.Translate(), MessageTypeDefOf.RejectInput, historical: false);
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            Facility.SacrificeComp.ClearPreparation(Facility.Map);
        }
    }
}
