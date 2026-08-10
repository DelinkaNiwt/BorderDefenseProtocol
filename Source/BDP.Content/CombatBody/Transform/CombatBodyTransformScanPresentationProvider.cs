using System.Collections.Generic;
using BDP.Core.CombatBody;
using BDP.Core.CombatBody.Presentation;
using Verse;

namespace BDP.Content.CombatBody.Transform
{
    /// <summary>
    /// Content.dll 提供的战斗体扫描变换表现。
    /// 它在真实形态切换前后捕获完整人物最终画面，并在切换完成后生成一个短命 Mote。
    /// </summary>
    public sealed class CombatBodyTransformScanPresentationProvider : ICombatBodyTransformPresentationProvider
    {
        /// <summary>
        /// 扫描 Mote 的正式 Def 名称。
        /// </summary>
        private const string ScanMoteDefName = "BDP_Mote_CombatBodyScan";

        /// <summary>
        /// 按 Pawn 实体编号暂存的形态切换前完整人物快照。
        /// </summary>
        private readonly Dictionary<int, PendingTransformCapture> pendingByPawnId =
            new Dictionary<int, PendingTransformCapture>();

        /// <summary>
        /// 按 Pawn 实体编号保存已经成功生成的扫描动画占用截止 tick。
        /// </summary>
        private readonly Dictionary<int, int> activeScanUntilTickByPawnId =
            new Dictionary<int, int>();

        /// <summary>
        /// 在宿主形态切换前捕获并暂存退场完整人物画面。
        /// </summary>
        public void Begin(Pawn pawn, CombatBodyTransformDirection direction)
        {
            if (pawn == null)
            {
                return;
            }

            if (pendingByPawnId.TryGetValue(pawn.thingIDNumber, out PendingTransformCapture previous))
            {
                pendingByPawnId.Remove(pawn.thingIDNumber);
                CombatBodyPawnVisualCapture.Release(previous.OutgoingSnapshot);
            }

            if (!ShouldPresentTransform(pawn, direction)
                || IsScanWindowActive(pawn)
                || !CanPresent(pawn))
            {
                return;
            }

            CombatBodyPawnVisualSnapshot outgoingSnapshot =
                CombatBodyPawnVisualCapture.Capture(pawn);
            if (outgoingSnapshot == null)
            {
                return;
            }

            pendingByPawnId[pawn.thingIDNumber] =
                new PendingTransformCapture(direction, outgoingSnapshot);
        }

        /// <summary>
        /// 在真实形态切换完成后捕获入场完整人物画面并生成扫描 Mote。
        /// </summary>
        public void End(Pawn pawn, CombatBodyTransformDirection direction)
        {
            if (pawn == null
                || !pendingByPawnId.TryGetValue(pawn.thingIDNumber, out PendingTransformCapture pending))
            {
                return;
            }

            pendingByPawnId.Remove(pawn.thingIDNumber);
            if (pending.Direction != direction
                || !ShouldPresentTransform(pawn, direction)
                || !CanPresent(pawn))
            {
                CombatBodyPawnVisualCapture.Release(pending.OutgoingSnapshot);
                return;
            }

            CombatBodyPawnVisualSnapshot incomingSnapshot =
                CombatBodyPawnVisualCapture.Capture(pawn);
            if (incomingSnapshot == null)
            {
                CombatBodyPawnVisualCapture.Release(pending.OutgoingSnapshot);
                return;
            }

            Mote_CombatBodyScan mote = null;
            bool moteOwnsSnapshots = false;
            try
            {
                ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ScanMoteDefName);
                if (moteDef == null)
                {
                    return;
                }

                mote = ThingMaker.MakeThing(moteDef) as Mote_CombatBodyScan;
                if (mote == null)
                {
                    return;
                }

                mote.Configure(pawn, direction, pending.OutgoingSnapshot, incomingSnapshot);
                mote.Attach(pawn);
                mote.exactPosition = pawn.DrawPos;
                GenSpawn.Spawn(mote, pawn.Position, pawn.Map);
                moteOwnsSnapshots = true;
                int currentTick = Find.TickManager?.TicksGame ?? 0;
                activeScanUntilTickByPawnId[pawn.thingIDNumber] =
                    currentTick + Mote_CombatBodyScan.DurationTicks;
            }
            finally
            {
                if (!moteOwnsSnapshots && (mote == null || !mote.Spawned))
                {
                    CombatBodyPawnVisualCapture.Release(pending.OutgoingSnapshot);
                    CombatBodyPawnVisualCapture.Release(incomingSnapshot);
                }
            }
        }

        /// <summary>
        /// 判断当前形态切换原因是否需要扫描表现。
        /// 被动崩解及其紧急脱离扩展共享 Collapsing 退出链，只保留瞬时恢复与既有烟雾。
        /// </summary>
        private static bool ShouldPresentTransform(
            Pawn pawn,
            CombatBodyTransformDirection direction)
        {
            if (direction != CombatBodyTransformDirection.Exit)
            {
                return true;
            }

            ICombatBodyReader combatBodyReader = CombatBodySurfaceAccess.ResolveReader(pawn);
            return combatBodyReader == null
                || combatBodyReader.Phase != CombatBodyPhase.Collapsing;
        }

        /// <summary>
        /// 判断当前 Pawn 是否适合启动一次可见扫描表现。
        /// </summary>
        private static bool CanPresent(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && pawn.Map != null
                && Find.CameraDriver != null
                && (Find.UIRoot == null || !Find.UIRoot.HideMotes)
                && Find.CameraDriver.CurrentViewRect.ExpandedBy(3).Contains(pawn.Position);
        }

        /// <summary>
        /// 判断指定 Pawn 是否已经有一段尚未结束的扫描动画。
        /// 过期记录在下一次查询时就地移除，不长期持有 Pawn 或 Mote 引用。
        /// </summary>
        private bool IsScanWindowActive(Pawn pawn)
        {
            if (pawn == null
                || !activeScanUntilTickByPawnId.TryGetValue(pawn.thingIDNumber, out int endTick))
            {
                return false;
            }

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (currentTick < endTick)
            {
                return true;
            }

            activeScanUntilTickByPawnId.Remove(pawn.thingIDNumber);
            return false;
        }

        /// <summary>
        /// 单次同步宿主事务跨越形态切换前后的临时捕获状态。
        /// </summary>
        private sealed class PendingTransformCapture
        {
            /// <summary>
            /// 创建一条等待形态切换完成的捕获状态。
            /// </summary>
            internal PendingTransformCapture(
                CombatBodyTransformDirection direction,
                CombatBodyPawnVisualSnapshot outgoingSnapshot)
            {
                Direction = direction;
                OutgoingSnapshot = outgoingSnapshot;
            }

            /// <summary>
            /// 本轮形态切换方向。
            /// </summary>
            internal CombatBodyTransformDirection Direction { get; }

            /// <summary>
            /// 真实形态切换前冻结的完整人物画面。
            /// </summary>
            internal CombatBodyPawnVisualSnapshot OutgoingSnapshot { get; }
        }
    }
}
