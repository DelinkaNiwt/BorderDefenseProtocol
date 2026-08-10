using System;
using BDP.Core.CombatBody;
using BDP.Core.CombatBody.External;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using Verse;
using Verse.AI;

namespace BDP.Core.CombatBodySession
{
    /// <summary>
    /// 战斗会话薄接线服务。
    /// 它统一承接 CombatBody 的正式出入口，并把跨 CombatBody / Trigger / Trion 的事务顺序收口在这里。
    /// </summary>
    internal sealed class CombatBodySessionService : ICombatBodyReader, ICombatBodyCommands, ICombatBodyEvents
    {
        /// <summary>
        /// 战斗体维持消耗对应的统一 `Trion` 键。
        /// </summary>
        private readonly CompCombatBodyHost owner;

        /// <summary>
        /// 原始 CombatBody 相位服务。
        /// </summary>
        private readonly CombatBodyService rawCombatBodyService;

        /// <summary>
        /// 会话跨系统判断策略。
        /// </summary>
        private readonly CombatBodySessionPolicy policy;

        /// <summary>
        /// 战斗会话 `Trion` 绑定器。
        /// </summary>
        private readonly CombatBodySessionTrionBinding trionBinding;

        /// <summary>
        /// 战斗体激活事务。
        /// </summary>
        private readonly CombatBodyActivationTransaction activationTransaction;

        /// <summary>
        /// 战斗体退出事务。
        /// </summary>
        private readonly CombatBodyExitTransaction exitTransaction;

        /// <summary>
        /// 当前是否正在执行退出事务；用于压住服装/装备卸下回调造成的嵌套解除请求。
        /// </summary>
        private bool isExitInProgress;

        /// <summary>
        /// 构造战斗会话薄接线服务。
        /// </summary>
        public CombatBodySessionService(
            CompCombatBodyHost owner,
            CombatBodyService rawCombatBodyService,
            CombatBodySessionPolicy policy)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.rawCombatBodyService = rawCombatBodyService ?? throw new ArgumentNullException(nameof(rawCombatBodyService));
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
            trionBinding = new CombatBodySessionTrionBinding(owner, rawCombatBodyService, () => OwnerPawn, HandleAvailableDepleted);
            activationTransaction = new CombatBodyActivationTransaction(owner, rawCombatBodyService, policy, trionBinding, NotifyCombatBodySessionStateChanged);
            exitTransaction = new CombatBodyExitTransaction(owner, rawCombatBodyService, trionBinding, NotifyCombatBodySessionStateChanged);
        }

        /// <summary>
        /// 当前宿主 Pawn。
        /// </summary>
        internal Pawn OwnerPawn
        {
            get { return owner.parent as Pawn; }
        }

        /// <summary>
        /// 当前会话转发使用的 raw combat body service。
        /// </summary>
        internal CombatBodyService RawCombatBodyService
        {
            get { return rawCombatBodyService; }
        }

        /// <summary>
        /// 当前会话判断策略。
        /// </summary>
        internal CombatBodySessionPolicy Policy
        {
            get { return policy; }
        }

        /// <summary>
        /// 对外转发战斗体阶段变化事件。
        /// </summary>
        public event Action<CombatBodyPhaseChangedArgs> PhaseChanged
        {
            add { rawCombatBodyService.PhaseChanged += value; }
            remove { rawCombatBodyService.PhaseChanged -= value; }
        }

        /// <summary>
        /// 读取当前战斗体阶段。
        /// </summary>
        public CombatBodyPhase Phase
        {
            get { return rawCombatBodyService.Phase; }
        }

        /// <summary>
        /// 读取当前锁定的 Trion 量。
        /// </summary>
        public float AllocatedTrion
        {
            get { return rawCombatBodyService.AllocatedTrion; }
        }

        /// <summary>
        /// 读取进入 Active 的绝对 tick。
        /// </summary>
        public int ActivationTick
        {
            get { return rawCombatBodyService.ActivationTick; }
        }

        /// <summary>
        /// 读取当前崩解原因。
        /// </summary>
        public string CollapseReason
        {
            get { return rawCombatBodyService.CollapseReason; }
        }

        /// <summary>
        /// 判断当前是否允许激活。
        /// </summary>
        public bool CanActivate()
        {
            return rawCombatBodyService.CanActivate();
        }

        /// <summary>
        /// 判断当前是否允许手动关闭。
        /// </summary>
        public bool CanManualDeactivate()
        {
            return !isExitInProgress && rawCombatBodyService.CanManualDeactivate();
        }

        /// <summary>
        /// 判断当前是否处于崩解无敌阶段。
        /// </summary>
        public bool IsInvulnerable()
        {
            return rawCombatBodyService.IsInvulnerable();
        }

        /// <summary>
        /// 读取剩余冷却时长。
        /// </summary>
        public int GetCooldownRemaining()
        {
            return rawCombatBodyService.GetCooldownRemaining();
        }

        /// <summary>
        /// 读取剩余崩解表现时长。
        /// </summary>
        public int GetCollapseRemaining()
        {
            return rawCombatBodyService.GetCollapseRemaining();
        }

        /// <summary>
        /// 请求激活战斗体。
        /// 激活顺序统一收口在这里，raw combat body service 只负责相位和宿主变换。
        /// </summary>
        public bool TryActivate()
        {
            if (!CanActivate())
            {
                return false;
            }

            bool activated = activationTransaction.TryActivate(OwnerPawn);
            if (activated)
            {
                BeginManualTransformLock();
            }

            return activated;
        }

        /// <summary>
        /// 请求主动解除战斗体。
        /// </summary>
        public void RequestRelease()
        {
            if (!CanManualDeactivate())
            {
                return;
            }

            ExecuteExit(CombatBodySessionExitMode.Release);
            BeginManualTransformLock();
        }

        /// <summary>
        /// 请求触发战斗体崩解。
        /// 当前只负责切入崩解相位，不在这里直接做退出收尾。
        /// </summary>
        public void TriggerCollapse(string reason)
        {
            if (rawCombatBodyService.Phase != CombatBodyPhase.Active)
            {
                return;
            }

            CombatBodyCollapseExtensionRegistry.Prepare(OwnerPawn);
            rawCombatBodyService.EnterCollapsing(reason);
            if (policy.TryResolvePrimaryTrigger(OwnerPawn, out CompTriggerBody trigger))
            {
                trigger.SetCombatBodyUnavailableDisabled(true);
            }

            ApplyCollapsePendingHediff(OwnerPawn);
            OwnerPawn?.jobs?.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false, canReturnToPool: false);
            NotifyCombatBodySessionStateChanged();
        }

        /// <summary>
        /// 在崩解表现结束后推进正式收尾。
        /// </summary>
        public void FinalizeCollapse()
        {
            if (rawCombatBodyService.Phase != CombatBodyPhase.Collapsing)
            {
                return;
            }

            ExecuteExit(CombatBodySessionExitMode.Collapse);
        }

        /// <summary>
        /// 处理可用 Trion 见底事件。
        /// 它只负责切入崩解相位；90 ticks 崩解表现结束后，由宿主 CompTick 推进正式收尾。
        /// </summary>
        private void HandleAvailableDepleted()
        {
            if (rawCombatBodyService.Phase != CombatBodyPhase.Active)
            {
                return;
            }

            TriggerCollapse("TrionAvailableDepleted");
        }

        /// <summary>
        /// 给当前 Pawn 挂上崩解表现期显示 hediff。
        /// 它只用于显示，不参与崩解业务判断。
        /// </summary>
        private static void ApplyCollapsePendingHediff(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("BDP_CombatBodyCollapsePending");
            if (def == null)
            {
                return;
            }

            if (pawn.health.hediffSet.GetFirstHediffOfDef(def, false) != null)
            {
                return;
            }

            pawn.health.AddHediff(def);
        }

        /// <summary>
        /// 通知当前战斗会话状态已经变化，需要重新裁定 Trigger 正式投影。
        /// </summary>
        private void NotifyCombatBodySessionStateChanged()
        {
            if (!policy.TryResolvePrimaryTrigger(OwnerPawn, out CompTriggerBody trigger))
            {
                return;
            }

            trigger.RuntimeCoordinator?.MarkDirty(ProjectionDirtyReason.CombatBodySessionStateChanged);
            trigger.RuntimeCoordinator?.RebuildAndPublish();
        }

        /// <summary>
        /// 在读档后恢复战斗会话所需的轻量运行时订阅。
        /// Trion 标量由 CompTrion 序列化恢复；持续消耗登记表是运行时账本，读档后由各 owner 重新注册。
        /// 这里负责补回战斗体维护消耗与事件句柄。
        /// </summary>
        internal void RestoreAfterLoad()
        {
            trionBinding.RestoreAfterLoad();
        }

        /// <summary>
        /// 按全局 XML 配置启动一次手动形态切换锁。
        /// </summary>
        private void BeginManualTransformLock()
        {
            int lockTicks = CombatBodyHostConfigResolver.Resolve().manualTransformLockTicks;
            rawCombatBodyService.BeginManualTransformLock(lockTicks);
        }

        /// <summary>
        /// 按指定退出语义执行战斗体关闭事务。
        /// </summary>
        private void ExecuteExit(CombatBodySessionExitMode exitMode)
        {
            if (isExitInProgress)
            {
                return;
            }

            isExitInProgress = true;
            try
            {
                exitTransaction.Execute(OwnerPawn, exitMode);
            }
            finally
            {
                isExitInProgress = false;
            }
        }

    }
}
