using System;
using BDP.Core.CombatBody;
using BDP.Core.CombatBody.External;
using BDP.Core.Trigger;
using BDP.Core.Trion;
using RimWorld;
using Verse;

namespace BDP.Core.CombatBodySession
{
    /// <summary>
    /// 战斗体退出事务。
    /// 它负责收口从 Active 或 Collapsing 退出时的主链清理顺序。
    /// </summary>
    internal sealed class CombatBodyExitTransaction
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
        /// 战斗体会话 Trion 绑定器。
        /// </summary>
        private readonly CombatBodySessionTrionBinding trionBinding;

        /// <summary>
        /// 退出完成后的状态通知。
        /// </summary>
        private readonly Action notifyCombatBodySessionStateChanged;

        /// <summary>
        /// 构造退出事务。
        /// </summary>
        public CombatBodyExitTransaction(
            CompCombatBodyHost owner,
            CombatBodyService rawCombatBodyService,
            CombatBodySessionTrionBinding trionBinding,
            Action notifyCombatBodySessionStateChanged)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.rawCombatBodyService = rawCombatBodyService ?? throw new ArgumentNullException(nameof(rawCombatBodyService));
            this.trionBinding = trionBinding ?? throw new ArgumentNullException(nameof(trionBinding));
            this.notifyCombatBodySessionStateChanged = notifyCombatBodySessionStateChanged ?? throw new ArgumentNullException(nameof(notifyCombatBodySessionStateChanged));
        }

        /// <summary>
        /// 按指定退出模式执行关闭收尾。
        /// </summary>
        public void Execute(Pawn ownerPawn, CombatBodySessionExitMode exitMode)
        {
            if (!CanDeactivate(exitMode))
            {
                return;
            }

            if (exitMode == CombatBodySessionExitMode.Collapse)
            {
                RemoveCollapsePendingHediff(ownerPawn);
                ReleaseCollapseSmoke(ownerPawn);
                CombatBodyCollapseExtensionRegistry.Execute(ownerPawn);
                // 取消征召是崩解主链的固定步骤，不依赖任何外部附加扩展。
                TryReleaseDraft(ownerPawn);
            }

            ITrionCommands trionCommands = TrionSurfaceAccess.ResolveCommands(ownerPawn);
            ITriggerLoadoutCommands triggerLoadoutCommands = TriggerSurfaceAccess.ResolveLoadoutCommands(ownerPawn);
            if (exitMode == CombatBodySessionExitMode.Release)
            {
                DeactivateAllSlots(triggerLoadoutCommands);
            }

            trionBinding.ClearActiveRuntime();

            if (trionCommands != null)
            {
                trionCommands.Release(rawCombatBodyService.AllocatedTrion);
                trionCommands.SetFrozen(false);
            }

            rawCombatBodyService.EnterCooldown(ResolveCooldownTicks(exitMode), ResolveExitReason(exitMode));
            owner.WoundRuntime.ClearActiveRuntime(ownerPawn);

            if (exitMode == CombatBodySessionExitMode.Collapse)
            {
                trionCommands?.TrySetCurrent(0f);
                ApplyCollapseAftereffect(ownerPawn);
                TriggerSurfaceAccess.ResolveComp(ownerPawn)?.SetCombatBodyUnavailableDisabled(false);
            }

            CombatBodyCollapseExtensionRegistry.Clear(ownerPawn);
            notifyCombatBodySessionStateChanged();
        }

        /// <summary>
        /// 取消当前小人的征召状态。
        /// 它是崩解主链上的固定步骤，不依赖是否注册外部附加扩展。
        /// </summary>
        private static void TryReleaseDraft(Pawn ownerPawn)
        {
            if (ownerPawn?.drafter != null)
            {
                ownerPawn.drafter.Drafted = false;
            }
        }

        /// <summary>
        /// 在被动崩解正式收尾时，于外部附加扩展执行前在原地释放一次原版烟雾。
        /// </summary>
        private static void ReleaseCollapseSmoke(Pawn ownerPawn)
        {
            if (ownerPawn == null || !ownerPawn.Spawned || ownerPawn.Map == null)
            {
                return;
            }

            GenExplosion.DoExplosion(
                ownerPawn.Position,
                ownerPawn.Map,
                2.0f,
                DamageDefOf.Smoke,
                null,
                -1,
                -1f,
                null,
                null,
                null,
                null,
                null,
                0f,
                1,
                GasType.BlindSmoke,
                postExplosionGasAmount: 8);
        }

        /// <summary>
        /// 判断当前是否允许退出。
        /// </summary>
        private bool CanDeactivate(CombatBodySessionExitMode exitMode)
        {
            if (exitMode == CombatBodySessionExitMode.Release)
            {
                return rawCombatBodyService.CanManualDeactivate();
            }

            CombatBodyPhase phase = rawCombatBodyService.Phase;
            return phase == CombatBodyPhase.Active || phase == CombatBodyPhase.Collapsing;
        }

        /// <summary>
        /// 关闭三侧 Trigger 激活入口。
        /// </summary>
        private void DeactivateAllSlots(ITriggerLoadoutCommands triggerLoadoutCommands)
        {
            triggerLoadoutCommands?.RequestDeactivate(TriggerSide.Main);
            triggerLoadoutCommands?.RequestDeactivate(TriggerSide.Sub);
            triggerLoadoutCommands?.RequestDeactivate(TriggerSide.Special);
        }

        /// <summary>
        /// 解析退出后的冷却时长。
        /// </summary>
        private int ResolveCooldownTicks(CombatBodySessionExitMode exitMode)
        {
            return exitMode == CombatBodySessionExitMode.Collapse
                ? rawCombatBodyService.CollapseCooldownTicks
                : 0;
        }

        /// <summary>
        /// 解析退出原因。
        /// </summary>
        private string ResolveExitReason(CombatBodySessionExitMode exitMode)
        {
            switch (exitMode)
            {
                case CombatBodySessionExitMode.Collapse:
                    return "Collapse";
                default:
                    return "Release";
            }
        }

        /// <summary>
        /// 在被动崩解退场后挂上后遗症。
        /// </summary>
        private static void ApplyCollapseAftereffect(Pawn ownerPawn)
        {
            if (ownerPawn?.health?.hediffSet == null)
            {
                return;
            }

            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("BDP_CombatBodyCollapseAftereffect");
            if (def == null)
            {
                return;
            }

            if (ownerPawn.health.hediffSet.GetFirstHediffOfDef(def, false) != null)
            {
                return;
            }

            ownerPawn.health.AddHediff(def);
        }

        /// <summary>
        /// 在崩解表现正式进入收尾时移除延时崩解显示 hediff。
        /// </summary>
        private static void RemoveCollapsePendingHediff(Pawn ownerPawn)
        {
            if (ownerPawn?.health?.hediffSet == null)
            {
                return;
            }

            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("BDP_CombatBodyCollapsePending");
            if (def == null)
            {
                return;
            }

            Hediff hediff = ownerPawn.health.hediffSet.GetFirstHediffOfDef(def, false);
            if (hediff == null)
            {
                return;
            }

            ownerPawn.health.RemoveHediff(hediff);
        }
    }
}
