using System;
using BDP.Core.CombatBody;
using BDP.Core.Trion;
using Verse;

namespace BDP.Core.CombatBodySession
{
    /// <summary>
    /// 战斗会话 `Trion` 绑定器。
    /// 它只负责维护战斗体活动期的 drain 与事件订阅生命周期。
    /// </summary>
    internal sealed class CombatBodySessionTrionBinding
    {
        /// <summary>
        /// 战斗体宿主 comp。
        /// </summary>
        private readonly CompCombatBodyHost owner;

        /// <summary>
        /// 原始战斗体相位服务。
        /// </summary>
        private readonly CombatBodyService rawCombatBodyService;

        /// <summary>
        /// 当前会话用于解析宿主 Pawn 的委托。
        /// </summary>
        private readonly Func<Pawn> ownerPawnAccessor;

        /// <summary>
        /// 可用 `Trion` 见底时的回调。
        /// </summary>
        private readonly Action onAvailableDepleted;

        /// <summary>
        /// 当前可用 `Trion` 见底事件的订阅句柄。
        /// </summary>
        private Action availableDepletedHandler;

        /// <summary>
        /// 战斗体维持消耗对应的统一 `Trion` 键。
        /// </summary>
        private static readonly TrionDrainKey combatBodyMaintenanceDrainKey =
            new TrionDrainKey("CombatBody", "Maintenance", -1, string.Empty);

        /// <summary>
        /// 构造战斗会话 `Trion` 绑定器。
        /// </summary>
        public CombatBodySessionTrionBinding(
            CompCombatBodyHost owner,
            CombatBodyService rawCombatBodyService,
            Func<Pawn> ownerPawnAccessor,
            Action onAvailableDepleted)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.rawCombatBodyService = rawCombatBodyService ?? throw new ArgumentNullException(nameof(rawCombatBodyService));
            this.ownerPawnAccessor = ownerPawnAccessor ?? throw new ArgumentNullException(nameof(ownerPawnAccessor));
            this.onAvailableDepleted = onAvailableDepleted ?? throw new ArgumentNullException(nameof(onAvailableDepleted));
        }

        /// <summary>
        /// 在战斗体进入 Active 后挂接运行时 `Trion` 绑定。
        /// </summary>
        public void BindActiveRuntime()
        {
            TryRegisterCombatBodyMaintenanceDrain(TrionSurfaceAccess.ResolveCommands(ResolveOwnerPawn()));
            SubscribeAvailableDepleted();
        }

        /// <summary>
        /// 在战斗体退出 Active 后移除运行时 `Trion` 绑定。
        /// </summary>
        public void ClearActiveRuntime()
        {
            TryUnregisterCombatBodyMaintenanceDrain(TrionSurfaceAccess.ResolveCommands(ResolveOwnerPawn()));
            UnsubscribeAvailableDepleted();
        }

        /// <summary>
        /// 在读档后恢复战斗会话所需的轻量运行时绑定。
        /// </summary>
        public void RestoreAfterLoad()
        {
            if (rawCombatBodyService.Phase == CombatBodyPhase.Active)
            {
                BindActiveRuntime();
                return;
            }

            UnsubscribeAvailableDepleted();
        }

        /// <summary>
        /// 解析当前会话宿主 Pawn。
        /// </summary>
        private Pawn ResolveOwnerPawn()
        {
            return ownerPawnAccessor();
        }

        /// <summary>
        /// 注册战斗体维持消耗。
        /// </summary>
        private void TryRegisterCombatBodyMaintenanceDrain(ITrionCommands trionCommands)
        {
            if (trionCommands == null || owner.MaintenanceDrainPerSecond <= 0f)
            {
                return;
            }

            trionCommands.RegisterDrain(combatBodyMaintenanceDrainKey, owner.MaintenanceDrainPerSecond);
        }

        /// <summary>
        /// 注销战斗体维持消耗。
        /// </summary>
        private void TryUnregisterCombatBodyMaintenanceDrain(ITrionCommands trionCommands)
        {
            if (trionCommands == null)
            {
                return;
            }

            trionCommands.UnregisterDrain(combatBodyMaintenanceDrainKey);
        }

        /// <summary>
        /// 订阅可用 `Trion` 见底事件。
        /// </summary>
        private void SubscribeAvailableDepleted()
        {
            UnsubscribeAvailableDepleted();

            ITrionEvents trionEvents = TrionSurfaceAccess.ResolveEvents(ResolveOwnerPawn());
            if (trionEvents == null)
            {
                return;
            }

            availableDepletedHandler = HandleAvailableDepleted;
            trionEvents.AvailableDepleted += availableDepletedHandler;
        }

        /// <summary>
        /// 取消订阅可用 `Trion` 见底事件。
        /// </summary>
        private void UnsubscribeAvailableDepleted()
        {
            ITrionEvents trionEvents = TrionSurfaceAccess.ResolveEvents(ResolveOwnerPawn());
            if (trionEvents != null && availableDepletedHandler != null)
            {
                trionEvents.AvailableDepleted -= availableDepletedHandler;
            }

            availableDepletedHandler = null;
        }

        /// <summary>
        /// 处理可用 `Trion` 见底事件。
        /// </summary>
        private void HandleAvailableDepleted()
        {
            if (rawCombatBodyService.Phase != CombatBodyPhase.Active)
            {
                return;
            }

            onAvailableDepleted();
        }
    }
}

