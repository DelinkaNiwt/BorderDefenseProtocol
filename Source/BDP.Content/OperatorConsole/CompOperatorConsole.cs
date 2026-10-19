using System.Collections.Generic;
using BDP.Core.Permissions;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.OperatorConsole
{
    /// <summary>
    /// 操作员控制台的运行状态组件。
    /// 真实运行条件在这里集中判断，下游许可不复制这些前置条件。
    /// </summary>
    public sealed class CompOperatorConsole : ThingComp
    {
        /// <summary>当前正在操作的 Pawn；仅为运行时瞬态引用。</summary>
        private Pawn operatorPawn;

        /// <summary>最近一次操作员心跳的游戏 tick。</summary>
        private int lastOperatorHeartbeatTick = -1;

        /// <summary>上帝模式下持久化的强制运作覆盖。</summary>
        private bool forcedOperational;

        /// <summary>读取 Def 配置。</summary>
        private CompProperties_OperatorConsole Props
        {
            get { return (CompProperties_OperatorConsole)props; }
        }

        /// <summary>当前操作员，仅供运行时诊断读取。</summary>
        public Pawn OperatorPawn
        {
            get { return operatorPawn; }
        }

        /// <summary>当前是否由调试覆盖强制运作。</summary>
        public bool IsForcedOperational
        {
            get { return forcedOperational; }
        }

        /// <summary>
        /// 当前控制台是否满足对外发布许可的完整运作条件。
        /// 强制运作会绕过电力、操作员和 Trion 上游许可。
        /// </summary>
        public bool IsOperational
        {
            get
            {
                if (forcedOperational)
                {
                    return true;
                }

                return HasPower
                    && HasCurrentOperatorHeartbeat
                    && HasTrionRelayCorePermission;
            }
        }

        /// <summary>当前是否接入并开启原版电力组件。</summary>
        public bool HasPower
        {
            get
            {
                CompPowerTrader power = parent?.TryGetComp<CompPowerTrader>();
                return power != null && power.PowerOn;
            }
        }

        /// <summary>当前操作员心跳是否仍在有效窗口内。</summary>
        public bool HasCurrentOperatorHeartbeat
        {
            get
            {
                if (operatorPawn == null || !operatorPawn.Spawned || operatorPawn.Map != parent?.Map)
                {
                    return false;
                }

                int currentTick = GetCurrentTick();
                int heartbeatWindow = Props != null ? Mathf.Max(1, Props.operatorHeartbeatWindowTicks) : 120;
                return lastOperatorHeartbeatTick >= 0
                    && currentTick - lastOperatorHeartbeatTick <= heartbeatWindow
                    && operatorPawn.Faction == parent.Faction
                    && operatorPawn.health?.capacities?.CapableOf(PawnCapacityDefOf.Manipulation) == true;
            }
        }

        /// <summary>当前地图是否存在本建筑所属派系的 Trion 中继核心许可。</summary>
        public bool HasTrionRelayCorePermission
        {
            get
            {
                return parent != null
                    && MapPermissionAccess.HasPermission(
                        parent.Map,
                        parent.Faction,
                        OperatorConsolePermissionKeys.TrionRelayCoreActive);
            }
        }

        /// <summary>操作员进入交互格后刷新心跳。</summary>
        /// <param name="pawn">当前执行操作工作的 Pawn。</param>
        public void NotifyOperatorHeartbeat(Pawn pawn)
        {
            bool wasOperational = IsOperational;
            if (pawn == null || parent == null || pawn.Map != parent.Map || pawn.Faction != parent.Faction)
            {
                return;
            }

            operatorPawn = pawn;
            lastOperatorHeartbeatTick = GetCurrentTick();
            RefreshOperationalGlowIfChanged(wasOperational);
        }

        /// <summary>清除指定 Pawn 的操作状态，避免结束旧工作时误清除新操作员。</summary>
        /// <param name="pawn">准备清除的操作员。</param>
        public void ClearOperator(Pawn pawn)
        {
            bool wasOperational = IsOperational;
            if (pawn == null || operatorPawn == pawn)
            {
                operatorPawn = null;
                lastOperatorHeartbeatTick = -1;
                RefreshOperationalGlowIfChanged(wasOperational);
            }
        }

        /// <summary>组件 tick 中清理失效的运行时操作员引用。</summary>
        public override void CompTick()
        {
            base.CompTick();
            if (operatorPawn != null && !HasCurrentOperatorHeartbeat)
            {
                ClearOperator(operatorPawn);
            }
        }

        /// <summary>显示控制台真实状态与调试覆盖状态。</summary>
        public override string CompInspectStringExtra()
        {
            return forcedOperational
                ? "BDP_OperatorConsole_Status_ForcedRunning".Translate()
                : (IsOperational
                    ? "BDP_OperatorConsole_Status_Running".Translate()
                    : "BDP_OperatorConsole_Status_Stopped".Translate());
        }

        /// <summary>
        /// 提供与原版电力设施一致的强制运行切换。
        /// 该按钮只修改本组件覆盖，不伪造电力、Pawn 工作或 Trion 资源。
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (!DebugSettings.ShowDevGizmos && !DebugSettings.godMode)
            {
                yield break;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "BDP_OperatorConsole_Debug_ForceOperation".Translate(),
                defaultDesc = "BDP_OperatorConsole_Debug_ForceOperationDesc".Translate(),
                isActive = delegate { return forcedOperational; },
                toggleAction = delegate
                {
                    bool wasOperational = IsOperational;
                    forcedOperational = !forcedOperational;
                    RefreshOperationalGlowIfChanged(wasOperational);
                }
            };
        }

        /// <summary>保存强制运行覆盖；操作员引用和心跳读档后重新建立。</summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref forcedOperational, "bdpOperatorConsoleForcedOperational", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                operatorPawn = null;
                lastOperatorHeartbeatTick = -1;
                RefreshOperationalGlow();
            }
        }

        /// <summary>
        /// 在控制台运作状态翻转时刷新建筑的原版光照组件。
        /// </summary>
        private void RefreshOperationalGlowIfChanged(bool wasOperational)
        {
            if (wasOperational != IsOperational)
            {
                (parent as Building_OperatorConsole)?.RefreshOperationalGlow();
            }
        }

        /// <summary>
        /// 在组件生成或读档完成后刷新一次建筑的原版光照组件。
        /// </summary>
        private void RefreshOperationalGlow()
        {
            (parent as Building_OperatorConsole)?.RefreshOperationalGlow();
        }

        /// <summary>安全读取当前游戏 tick。</summary>
        private static int GetCurrentTick()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }
    }
}
