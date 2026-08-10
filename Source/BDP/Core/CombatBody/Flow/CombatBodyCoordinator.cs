using System;
using BDP.Core.Semantics;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// CombatBody 原始相位服务。
    /// 它只负责相位切换、宿主变换和阶段事件广播，不再直接编排 Trigger / Trion 事务。
    /// </summary>
    internal sealed class CombatBodyService : ICombatBodyReader, ICombatBodyEvents
    {
        /// <summary>
        /// 战斗体阶段真值。
        /// </summary>
        private readonly ICombatBodyPhaseState phaseState;

        /// <summary>
        /// RimWorld 宿主桥。
        /// </summary>
        private readonly ICombatBodyHost host;

        /// <summary>
        /// 战斗体崩解后需要进入的冷却时长。
        /// </summary>
        private readonly int collapseCooldownTicks;

        /// <summary>
        /// 战斗体阶段发生变化时广播。
        /// </summary>
        public event Action<CombatBodyPhaseChangedArgs> PhaseChanged;

        /// <summary>
        /// 构造 CombatBody 原始相位服务。
        /// </summary>
        public CombatBodyService(
            ICombatBodyPhaseState phaseState,
            ICombatBodyHost host,
            int collapseCooldownTicks)
        {
            this.phaseState = phaseState;
            this.host = host;
            this.collapseCooldownTicks = collapseCooldownTicks;
        }

        /// <summary>
        /// 读取当前阶段。
        /// </summary>
        public CombatBodyPhase Phase
        {
            get { return phaseState.Phase; }
        }

        /// <summary>
        /// 读取当前锁定的 Trion 量。
        /// </summary>
        public float AllocatedTrion
        {
            get { return phaseState.AllocatedTrion; }
        }

        /// <summary>
        /// 读取进入 Active 的绝对 tick。
        /// </summary>
        public int ActivationTick
        {
            get { return phaseState.ActivationTick; }
        }

        /// <summary>
        /// 读取当前崩解原因。
        /// </summary>
        public string CollapseReason
        {
            get { return phaseState.CollapseReason; }
        }

        /// <summary>
        /// 读取战斗体崩解的冷却时长配置。
        /// </summary>
        internal int CollapseCooldownTicks
        {
            get { return collapseCooldownTicks; }
        }

        /// <summary>
        /// 判断当前是否允许激活。
        /// </summary>
        public bool CanActivate()
        {
            return phaseState.CanActivate();
        }

        /// <summary>
        /// 判断当前是否允许手动退出。
        /// </summary>
        public bool CanManualDeactivate()
        {
            return phaseState.CanManualDeactivate();
        }

        /// <summary>
        /// 启动一次手动形态切换后的短时准入锁。
        /// </summary>
        internal void BeginManualTransformLock(int lockTicks)
        {
            phaseState.BeginManualTransformLock(lockTicks);
        }

        /// <summary>
        /// 判断当前是否处于不可伤害阶段。
        /// </summary>
        public bool IsInvulnerable()
        {
            return phaseState.IsInvulnerable();
        }

        /// <summary>
        /// 读取剩余冷却时长。
        /// </summary>
        public int GetCooldownRemaining()
        {
            return phaseState.GetCooldownRemaining();
        }

        /// <summary>
        /// 读取剩余崩解表现时长。
        /// </summary>
        public int GetCollapseRemaining()
        {
            return phaseState.GetCollapseRemaining();
        }

        /// <summary>
        /// 进入 Active 相位并执行宿主战斗体变换。
        /// </summary>
        internal bool TryEnterActive(float allocatedTrion)
        {
            CombatBodyPhase previousPhase = phaseState.Phase;
            if (!phaseState.CanActivate())
            {
                return false;
            }

            host.ApplyCombatBodyTransformation();
            phaseState.EnterActive(allocatedTrion);
            NotifyPhaseChanged(previousPhase, phaseState.Phase, null);
            return true;
        }

        /// <summary>
        /// 进入冷却相位并执行宿主恢复。
        /// </summary>
        internal void EnterCooldown(int cooldownTicks, string reason)
        {
            CombatBodyPhase previousPhase = phaseState.Phase;
            if (previousPhase != CombatBodyPhase.Active && previousPhase != CombatBodyPhase.Collapsing)
            {
                return;
            }

            host.RestoreFromCombatBody();
            phaseState.EnterCooldown(cooldownTicks);
            NotifyPhaseChanged(previousPhase, phaseState.Phase, reason);
        }

        /// <summary>
        /// 切入崩解相位。
        /// </summary>
        internal void EnterCollapsing(string reason)
        {
            CombatBodyPhase previousPhase = phaseState.Phase;
            if (previousPhase != CombatBodyPhase.Active)
            {
                return;
            }

            phaseState.EnterCollapsing(reason);
            NotifyPhaseChanged(previousPhase, phaseState.Phase, reason, BuildCollapseSemanticContext(reason));
        }

        /// <summary>
        /// 广播相位变化事件。
        /// </summary>
        private void NotifyPhaseChanged(CombatBodyPhase previousPhase, CombatBodyPhase currentPhase, string reason, ISemanticContext semanticContext = null)
        {
            if (previousPhase == currentPhase && string.IsNullOrEmpty(reason))
            {
                return;
            }

            PhaseChanged?.Invoke(new CombatBodyPhaseChangedArgs
            {
                PreviousPhase = previousPhase,
                CurrentPhase = currentPhase,
                AllocatedTrion = phaseState.AllocatedTrion,
                Reason = reason,
                SemanticContext = semanticContext
            });
        }

        /// <summary>
        /// 为崩解事件构建语义上下文。
        /// </summary>
        private ISemanticContext BuildCollapseSemanticContext(string reason)
        {
            return new SemanticContext
            {
                Id = "collapse_" + (host.Pawn != null ? host.Pawn.ThingID : "unknown"),
                DisplayLabel = "崩裂",
                SourceKind = SemanticSourceKind.CollapseTrigger,
                ReasonKey = reason,
                Instigator = host.Pawn
            };
        }
    }
}
