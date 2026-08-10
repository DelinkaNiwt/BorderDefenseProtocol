using System;
using BDP.Core.AttackExecution;
using BDP.Core.Expressions;
using BDP.Core.Trigger;
using BDP.Core.VerbHosting;
using Verse;

namespace BDP.Core.Verbs
{
    /// <summary>
    /// 原版 VerbTracker 正式持有的远程宿主壳。
    /// 它只保存稳定槽位身份，并在运行时从当前 binding 读取真实会话表面。
    /// </summary>
    public sealed class BdpVerb_FormalHostShoot : BdpVerb_Shoot
    {
        /// <summary>
        /// 当前正式宿主壳所属的 Trigger owner。
        /// </summary>
        private CompTriggerBody owner;

        /// <summary>
        /// 当前正式宿主壳对应的固定槽位身份。
        /// </summary>
        private BdpFormalVerbHostSlot slot;

        /// <summary>
        /// 读档后首次重绑时的一次性保态标记。
        /// 只有恢复回来的同会话 formal host 壳才允许跳过一次 Reset。
        /// </summary>
        private bool preserveLoadedStateOnce;

        /// <summary>
        /// 用 owner 与固定槽位初始化正式宿主壳。
        /// </summary>
        internal void InitializeFormalHost(CompTriggerBody owner, BdpFormalVerbHostSlot slot)
        {
            this.owner = owner;
            this.slot = slot;
            loadID = BuildFormalHostLoadId(owner, slot, "Ranged");
            verbTracker = owner != null ? owner.VerbTracker : verbTracker;
            caster = owner != null ? owner.OwnerPawn : caster;
        }

        /// <summary>
        /// 标记这条壳 verb 在首次 post-load rebind 时保留已恢复状态。
        /// </summary>
        internal void MarkPreserveLoadedStateOnce()
        {
            preserveLoadedStateOnce = true;
        }

        /// <summary>
        /// 在读档后的正式重绑发生前，先给恢复回来的壳补上一层最小 fallback 表面。
        /// 这样原版 PostLoadInit 检查不会因为 verbProps 为空而先把它判死。
        /// </summary>
        internal void ApplyPostLoadFallbackSurface(VerbProperties fallbackVerbProps)
        {
            if (verbProps == null)
            {
                verbProps = fallbackVerbProps;
            }

            caster = owner != null ? owner.OwnerPawn : caster;
            verbTracker = owner != null ? owner.VerbTracker : verbTracker;
        }

        /// <summary>
        /// 按当前 binding 同步正式宿主壳的最小运行时表面。
        /// </summary>
        internal void SyncFormalBinding(BdpFormalVerbBindingState bindingState, VerbProperties fallbackVerbProps)
        {
            string previousResultId = HostResultId;
            AttackSessionToken previousToken = HostSessionToken != null ? HostSessionToken.Clone() : null;
            string nextResultId = bindingState != null && bindingState.IsAvailable ? bindingState.ResultId : null;
            AttackSessionToken nextSessionToken = bindingState != null && bindingState.IsAvailable
                ? bindingState.SessionToken
                : null;
            bool resetApplied = ShouldResetForBindingChange(bindingState, fallbackVerbProps);
            RangedAttackModuleSession sessionBeforeReset = HostModuleSession;
            if (resetApplied)
            {
                AttackExecutionDiagnostics.LogFormalHostSessionTokenSync(
                    CasterPawn,
                    this,
                    slot,
                    loadID,
                    previousResultId,
                    nextResultId,
                    previousToken,
                    nextSessionToken,
                    HostSessionToken,
                    true,
                    "before_binding_reset");
                Reset();
            }

            if (previousResultId != nextResultId)
            {
                AttackExecutionDiagnostics.LogFormalHostRebind(
                    slot,
                    WeaponExpressionMode.Ranged,
                    loadID,
                    previousResultId,
                    nextResultId,
                    resetApplied);
            }

            if (resetApplied || HostSessionToken == null)
            {
                HostSessionToken = nextSessionToken != null ? nextSessionToken.Clone() : null;
            }

            if (resetApplied || !AttackSessionTokensEquivalent(previousToken, HostSessionToken))
            {
                AttackExecutionDiagnostics.LogFormalHostSessionTokenSync(
                    CasterPawn,
                    this,
                    slot,
                    loadID,
                    previousResultId,
                    nextResultId,
                    previousToken,
                    nextSessionToken,
                    HostSessionToken,
                    resetApplied,
                    "after_binding_sync");
            }

            verbProps = bindingState != null && bindingState.VerbProps != null ? bindingState.VerbProps : fallbackVerbProps;
            tool = bindingState != null ? bindingState.Tool : null;
            maneuver = bindingState != null ? bindingState.Maneuver : null;
            caster = owner != null ? owner.OwnerPawn : caster;
            verbTracker = owner != null ? owner.VerbTracker : verbTracker;
        }

        private static bool AttackSessionTokensEquivalent(AttackSessionToken left, AttackSessionToken right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            return left.AttackInstanceId == right.AttackInstanceId
                && left.ResultId == right.ResultId
                && left.ProjectionVersion == right.ProjectionVersion
                && left.OwnerPawnThingId == right.OwnerPawnThingId;
        }

        /// <summary>
        /// 当 binding 身份或声明面切换时，正式宿主壳必须重置旧会话状态。
        /// 否则旧的 Bursting / warmup 残留会把新表达结果一起卡死。
        /// </summary>
        private bool ShouldResetForBindingChange(BdpFormalVerbBindingState bindingState, VerbProperties fallbackVerbProps)
        {
            string nextResultId = bindingState != null && bindingState.IsAvailable ? bindingState.ResultId : null;
            VerbProperties nextVerbProps = bindingState != null && bindingState.VerbProps != null ? bindingState.VerbProps : fallbackVerbProps;
            Tool nextTool = bindingState != null ? bindingState.Tool : null;
            ManeuverDef nextManeuver = bindingState != null ? bindingState.Maneuver : null;
            if (preserveLoadedStateOnce && HostResultId == nextResultId)
            {
                preserveLoadedStateOnce = false;
                return false;
            }

            preserveLoadedStateOnce = false;
            return HostResultId != nextResultId
                || verbProps != nextVerbProps
                || tool != nextTool
                || maneuver != nextManeuver;
        }

        /// <summary>
        /// 为内部持有的 formal host 壳生成稳定 loadID。
        /// 这样它即使脱离原版 VerbTracker 列表，也仍有稳定身份可用于诊断和运行时排查。
        /// </summary>
        private static string BuildFormalHostLoadId(CompTriggerBody owner, BdpFormalVerbHostSlot slot, string mode)
        {
            string parentThingId = owner?.parent != null && !string.IsNullOrWhiteSpace(owner.parent.ThingID)
                ? owner.parent.ThingID
                : "NoParent";
            return "BDP_FormalHost_" + parentThingId + "_" + slot + "_" + mode;
        }

        /// <summary>
        /// 当前正式宿主壳只有在 binding 可用时才允许被原版战斗系统挑中。
        /// </summary>
        public override bool Available()
        {
            BdpFormalVerbBinding binding = owner != null ? owner.VerbHostManager.TryGetBinding(slot) : null;
            return binding != null
                && binding.IsAvailable
                && binding.ResolveActiveVerb() == this
                && !string.IsNullOrWhiteSpace(binding.ResultId)
                && base.Available();
        }

        /// <summary>
        /// dual formal host 只做引擎接口适配：当当前宿主是 dual 复合结果时，命中判定转译为 sides 聚合。
        /// 非 dual 宿主继续沿用原版 base 行为。
        /// </summary>
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (TryEvaluateDualAdapterLegality(root, targ, out bool anySideAllowed))
            {
                return anySideAllowed;
            }

            return base.CanHitTargetFrom(root, targ);
        }

        /// <summary>
        /// 判断当前读档恢复回来的远程 formal host 会话是否仍可续接。
        /// 它只校验最小真值，不在这里重建运行时派生 plan。
        /// </summary>
        internal bool CanResumeLoadedSession()
        {
            BdpFormalVerbBinding binding = owner != null ? owner.VerbHostManager.TryGetBinding(slot) : null;
            return binding != null
                && binding.IsAvailable
                && binding.ResolveActiveVerb() == this
                && HostSessionToken != null
                && !string.IsNullOrWhiteSpace(HostSessionToken.ResultId)
                && binding.ResultId == HostSessionToken.ResultId
                && HasValidLoadedBurstCursor();
        }

        /// <summary>
        /// 当前正式远程宿主壳是否仍需要进入 active tick 队列。
        /// 只有 binding 身份仍然自洽，且远程宿主自身仍有活跃运行时时才继续 tick。
        /// </summary>
        internal bool ShouldTickAsFormalHost()
        {
            BdpFormalVerbBinding binding = owner != null ? owner.VerbHostManager.TryGetBinding(slot) : null;
            return binding != null
                && binding.IsAvailable
                && binding.ResolveActiveVerb() == this
                && RequiresFormalHostRuntimeTick();
        }

        /// <summary>
        /// 当正式远程宿主壳被原版战斗链路选中起手时，主动把自己挂入 owner 的活跃 tick 队列。
        /// 这样后续 burst 推进就不需要再靠全 binding 常驻扫描。
        /// </summary>
        public override bool TryStartCastOn(
            LocalTargetInfo castTarg,
            LocalTargetInfo destTarg,
            bool surpriseAttack = false,
            bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false,
            bool nonInterruptingSelfCast = false)
        {
            bool started = base.TryStartCastOn(
                castTarg,
                destTarg,
                surpriseAttack,
                canHitNonTargetPawns,
                preventFriendlyFire,
                nonInterruptingSelfCast);
            if (started)
            {
                owner?.VerbHostManager?.NotifyFormalHostSessionStarted(slot);
            }

            return started;
        }

        /// <summary>
        /// 解析当前 formal host 壳的有效结果标识。
        /// 优先使用会话级 HostResultId（已应用执行上下文时）；
        /// 未设置时回退到槽位绑定的 ResultId（投影同步时已写入，始终可用）。
        /// </summary>
        private string ResolveEffectiveResultId()
        {
            if (!string.IsNullOrWhiteSpace(HostResultId))
            {
                return HostResultId;
            }

            if (owner?.VerbHostManager != null && slot != BdpFormalVerbHostSlot.None)
            {
                BdpFormalVerbBinding binding = owner.VerbHostManager.TryGetBinding(slot);
                if (binding != null && !string.IsNullOrWhiteSpace(binding.ResultId))
                {
                    return binding.ResultId;
                }
            }

            return null;
        }

        /// <summary>
        /// 若当前 formal host 对应 dual 复合结果，则把 CanHitTargetFrom 转译为”任一 side 当前可执行”。
        /// 返回 true 表示本次调用已由 dual 适配层接管；false 表示应回退到 base。
        /// </summary>
        private bool TryEvaluateDualAdapterLegality(IntVec3 root, LocalTargetInfo target, out bool anySideAllowed)
        {
            anySideAllowed = false;
            string effectiveResultId = ResolveEffectiveResultId();
            if (owner?.PublishedCombatProjection?.CompositeReferenceIndex == null
                || owner.PublishedCombatProjection.ResultIndex == null
                || string.IsNullOrWhiteSpace(effectiveResultId)
                || !owner.PublishedCombatProjection.ResultIndex.TryGetValue(effectiveResultId, out FormalExpressionResult hostResult)
                || hostResult == null
                || hostResult.CompositeKind != CompositeExpressionKind.DualWeapon
                || !owner.PublishedCombatProjection.CompositeReferenceIndex.TryGetValue(effectiveResultId, out CompositeExpressionReference reference)
                || reference == null)
            {
                return false;
            }

            bool baseCanHit = base.CanHitTargetFrom(root, target);
            bool mainAllowed = EvaluateDualSourceDirectTargetLegality(reference.MainSourceResultId, root, target);
            bool subAllowed = EvaluateDualSourceDirectTargetLegality(reference.SubSourceResultId, root, target);
            anySideAllowed = mainAllowed || subAllowed;
            string reason = ResolveDualAdapterReason(anySideAllowed, baseCanHit, mainAllowed, subAllowed);
            AttackExecutionDiagnostics.LogDualRangedHostLosProbe(
                CasterPawn,
                effectiveResultId,
                root,
                target,
                anySideAllowed,
                baseCanHit,
                reference.MainSourceResultId,
                mainAllowed,
                reference.SubSourceResultId,
                subAllowed,
                reason);
            return true;
        }

        /// <summary>
        /// 为 dual adapter 探针输出最小原因标签。
        /// </summary>
        private static string ResolveDualAdapterReason(bool anySideAllowed, bool baseCanHit, bool mainAllowed, bool subAllowed)
        {
            if (anySideAllowed && !baseCanHit)
            {
                return "adapter_allows_while_base_rejects";
            }

            if (!anySideAllowed && baseCanHit)
            {
                return "adapter_rejects_while_base_allows";
            }

            if (anySideAllowed)
            {
                return mainAllowed && subAllowed
                    ? "adapter_allows_both_sides"
                    : "adapter_allows_single_side";
            }

            return "adapter_rejects_all_sides";
        }

        /// <summary>
        /// 按当前已发布投影判断 dual 某一来源侧在真实目标上的必要直射准入。
        /// 不要求必要直射的侧直接放行；其余侧读取自己的 formal host 壳做 CanHitTargetFrom。
        /// </summary>
        private bool EvaluateDualSourceDirectTargetLegality(string sourceResultId, IntVec3 root, LocalTargetInfo target)
        {
            if (owner?.PublishedCombatProjection?.ResultIndex == null
                || string.IsNullOrWhiteSpace(sourceResultId)
                || !owner.PublishedCombatProjection.ResultIndex.TryGetValue(sourceResultId, out FormalExpressionResult sourceResult)
                || sourceResult == null)
            {
                return false;
            }

            ResolvedVerbSpec resolvedSpec = sourceResult.ResolvedVerbSpec;
            if (resolvedSpec == null || !resolvedSpec.RequiresDirectTargetLineOfSight)
            {
                return true;
            }

            if (!target.IsValid
                || owner.VerbHostManager == null
                || !owner.VerbHostManager.TryGetByResultId(sourceResultId, out BdpFormalVerbBinding binding))
            {
                return false;
            }

            Verb sourceVerb = binding.ResolveActiveVerb();
            return sourceVerb != null && sourceVerb.CanHitTargetFrom(root, target);
        }
    }
}
