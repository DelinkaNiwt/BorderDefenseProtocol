using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BDP.Content.Trion.Talent.Jobs
{
    /// <summary>
    /// 受检者侧配合等待工作；检测真值仍只由操作员工作提交。
    /// </summary>
    public sealed class JobDriver_WaitForTrionTalentAssessment : JobDriver
    {
        /// <summary>检测设备目标。</summary>
        private const TargetIndex DeviceIndex = TargetIndex.A;

        /// <summary>操作员目标。</summary>
        private const TargetIndex OperatorIndex = TargetIndex.B;

        /// <summary>无需抢占设备，设备由操作员独占预留。</summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        /// <summary>便携检测时受检者留在原地等待操作员靠近。</summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(OperatorIndex);

            Toil wait = ToilMaker.MakeToil("WaitForTrionTalentAssessment");
            wait.initAction = delegate { pawn.pather.StopDead(); };
            wait.defaultCompleteMode = ToilCompleteMode.Never;
            wait.handlingFacing = true;
            yield return wait;
        }
    }
}
