using System.Collections.Generic;
using BDP.Core.Trion;
using RimWorld;
using Verse;

namespace BDP.Core.Genes
{
    /// <summary>
    /// Trion 腺体基因。
    /// 只承担 Pawn 侧的基因身份、Trion 派生值刷新协作与 GUI 入口职责。
    /// </summary>
    public sealed class Gene_TrionGland : Gene
    {
        /// <summary>
        /// 上次观察到的有效状态，用于捕获被其它基因覆盖造成的失活与恢复。
        /// </summary>
        private bool lastObservedActive;

        /// <summary>
        /// 是否已经建立有效状态观察基线；读档首次 Tick 不应改写当前量。
        /// </summary>
        private bool activeObservationInitialized;

        /// <summary>
        /// 基因加入后，通知宿主刷新 Trion 派生值。
        /// </summary>
        public override void PostAdd()
        {
            base.PostAdd();
            CompTrion comp = pawn?.GetComp<CompTrion>();
            TrionEligibilityChangeReason reason = comp != null && comp.HasCompletedInitialResourceSetup
                ? TrionEligibilityChangeReason.RuntimeGranted
                : TrionEligibilityChangeReason.InitialSetup;
            RefreshTrionDerivedStats(reason);
            lastObservedActive = Active;
            activeObservationInitialized = true;
        }

        /// <summary>
        /// 基因移除后，通知宿主刷新 Trion 派生值。
        /// </summary>
        public override void PostRemove()
        {
            base.PostRemove();
            RefreshTrionDerivedStats(TrionEligibilityChangeReason.Lost);
            activeObservationInitialized = false;
        }

        /// <summary>
        /// 捕获基因被覆盖或解除覆盖造成的 Active（有效）状态变化。
        /// </summary>
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            bool active = Active;
            if (!activeObservationInitialized)
            {
                lastObservedActive = active;
                activeObservationInitialized = true;
                return;
            }

            if (active == lastObservedActive)
            {
                return;
            }

            lastObservedActive = active;
            RefreshTrionDerivedStats(
                active
                    ? TrionEligibilityChangeReason.RuntimeGranted
                    : TrionEligibilityChangeReason.Lost);
        }

        /// <summary>
        /// Pawn 侧 Trion GUI 入口。
        /// 第二阶段先挂住入口，后续再接入正式状态条。
        /// </summary>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerable<Gizmo> baseGizmos = base.GetGizmos();
            if (baseGizmos != null)
            {
                foreach (Gizmo gizmo in baseGizmos)
                {
                    if (gizmo != null)
                    {
                        yield return gizmo;
                    }
                }
            }

            foreach (Gizmo gizmo in TrionGeneGizmoBridge.BuildGizmos(pawn))
            {
                if (gizmo != null)
                {
                    yield return gizmo;
                }
            }
        }

        /// <summary>
        /// 刷新宿主 Pawn 的 Trion 派生值。
        /// </summary>
        private void RefreshTrionDerivedStats(TrionEligibilityChangeReason reason)
        {
            CompTrion comp = pawn?.GetComp<CompTrion>();
            if (comp != null)
            {
                comp.RefreshDerivedStats(reason);
            }
        }
    }
}
