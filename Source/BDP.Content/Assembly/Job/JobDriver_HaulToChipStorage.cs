using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 搬运芯片到芯片仓的 JobDriver。
    /// </summary>
    public class JobDriver_HaulToChipStorage : JobDriver
    {
        /// <summary>
        /// 被搬运芯片的目标索引。
        /// </summary>
        private const TargetIndex ChipIndex = TargetIndex.A;

        /// <summary>
        /// 芯片仓的目标索引。
        /// </summary>
        private const TargetIndex StorageIndex = TargetIndex.B;

        /// <summary>
        /// 插入芯片等待时间，单位为 tick（刻）。
        /// </summary>
        private const int InsertTicks = 30;

        /// <summary>
        /// 当前 Job 指向的芯片仓。
        /// </summary>
        private Thing Storage
        {
            get { return job.GetTarget(StorageIndex).Thing; }
        }

        /// <summary>
        /// 当前 Job 指向的芯片仓内部容器组件。
        /// </summary>
        private CompChipContainer StorageComp
        {
            get { return Storage?.TryGetComp<CompChipContainer>(); }
        }

        /// <summary>
        /// 当前小人正在搬运的芯片。
        /// </summary>
        private Thing CarriedChip
        {
            get { return pawn?.carryTracker?.CarriedThing; }
        }

        /// <summary>
        /// 预留芯片和芯片仓，避免多个小人同时搬运同一芯片或挤同一容器。
        /// </summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(ChipIndex), job, 1, 1, null, errorOnFailed)
                && pawn.Reserve(job.GetTarget(StorageIndex), job, 1, -1, null, errorOnFailed);
        }

        /// <summary>
        /// 执行搬运、走到芯片仓、等待并尝试插入芯片。
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(StorageIndex);

            yield return Toils_Goto.GotoThing(ChipIndex, PathEndMode.Touch)
                .FailOnDespawnedNullOrForbidden(ChipIndex)
                .FailOn(() => StorageComp == null || !StorageComp.CanAcceptMore);

            yield return Toils_Haul.StartCarryThing(
                ChipIndex,
                putRemainderInQueue: false,
                subtractNumTakenFromJobCount: false,
                failIfStackCountLessThanJobCount: true);

            yield return Toils_Goto.GotoThing(StorageIndex, PathEndMode.Touch);

            Toil waitToil = Toils_General.Wait(InsertTicks, StorageIndex)
                .WithProgressBarToilDelay(StorageIndex)
                .FailOnDespawnedOrNull(StorageIndex);
            waitToil.handlingFacing = true;
            yield return waitToil;

            yield return Toils_General.Do(TryInsertCarriedChip);
        }

        /// <summary>
        /// 在最终时刻尝试把小人携带的芯片交给芯片仓。
        /// </summary>
        private void TryInsertCarriedChip()
        {
            Thing chip = CarriedChip;
            CompChipContainer container = StorageComp;
            if (chip == null || container == null || !container.TryAcceptChip(chip))
            {
                DropCarriedChipNearStorage();
                return;
            }

            chip.def.soundDrop.PlayOneShot(SoundInfo.InMap(Storage));
            MoteMaker.ThrowText(Storage.DrawPos, pawn.Map, "BDP_Job_ChipStorage_Stored".Translate());
        }

        /// <summary>
        /// 放入失败时把仍在手上的芯片落在芯片仓附近，避免吞物品。
        /// </summary>
        private void DropCarriedChipNearStorage()
        {
            if (CarriedChip == null || pawn?.carryTracker == null)
            {
                return;
            }

            IntVec3 dropCell = Storage != null ? Storage.Position : pawn.Position;
            Thing droppedThing;
            pawn.carryTracker.TryDropCarriedThing(dropCell, ThingPlaceMode.Near, out droppedThing);
        }
    }
}
