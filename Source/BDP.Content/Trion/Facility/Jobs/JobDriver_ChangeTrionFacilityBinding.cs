using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Trion.Facility.Jobs
{
    /// <summary>走到核心交互格后执行独立绑定或解绑，不消费资源或供物。</summary>
    public sealed class JobDriver_ChangeTrionFacilityBinding : JobDriver
    {
        /// <summary>建筑目标；普通原版任务保存和恢复这一引用。</summary>
        private Building_TrionFacility Facility => job.targetA.Thing as Building_TrionFacility;

        /// <summary>预留核心及交互格，禁止多人同时完成归属操作。</summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Building_TrionFacility facility = Facility;
            if (facility == null || !pawn.Reserve(facility, job, 1, -1, null, errorOnFailed))
                return false;
            return !facility.def.hasInteractionCell
                || pawn.ReserveSittableOrSpot(facility.InteractionCell, job, errorOnFailed);
        }

        /// <summary>遵循原版寻路、禁止、取消机制；到达时重验资格和当前归属。</summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil change = ToilMaker.MakeToil("ChangeTrionFacilityBinding");
            change.initAction = delegate
            {
                CompTrionFacilityControl control = Facility?.ControlComp;
                bool success = control != null && (job.def == TrionFacilityJobDefOf.BDP_BindTrionFacility
                    ? control.TryBind(change.actor)
                    : control.TryUnbind(change.actor));
                if (!success)
                {
                    Messages.Message("BDP_TrionFacility_BindingFailed".Translate(),
                        MessageTypeDefOf.RejectInput, historical: false);
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            change.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return change;
        }
    }
}
