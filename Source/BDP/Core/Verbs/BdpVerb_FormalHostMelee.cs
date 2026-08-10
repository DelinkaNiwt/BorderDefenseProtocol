using System;
using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Expressions;
using BDP.Core.Trigger;
using BDP.Core.VerbHosting;
using Verse;

namespace BDP.Core.Verbs
{
    /// <summary>
    /// 原版 VerbTracker 正式持有的近战宿主壳。
    /// 它只保存稳定槽位身份，并在运行时从当前 binding 读取真实会话表面。
    /// </summary>
    public sealed class BdpVerb_FormalHostMelee : BdpVerb_MeleeAttackDamage
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
        /// 当前 formal host 最近一次按 binding 同步到的基准 VerbProps。
        /// 它代表绑定层真值，不会随着 step-local 换刀而改变。
        /// </summary>
        private VerbProperties bindingVerbProps;

        /// <summary>
        /// 当前 formal host 最近一次按 binding 同步到的基准 Tool。
        /// </summary>
        private Tool bindingTool;

        /// <summary>
        /// 当前 formal host 最近一次按 binding 同步到的基准 Maneuver。
        /// </summary>
        private ManeuverDef bindingManeuver;

        /// <summary>
        /// 用 owner 与固定槽位初始化正式宿主壳。
        /// </summary>
        internal void InitializeFormalHost(CompTriggerBody owner, BdpFormalVerbHostSlot slot)
        {
            this.owner = owner;
            this.slot = slot;
            loadID = BuildFormalHostLoadId(owner, slot, "Melee");
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
            string previousResultId = ResultId;
            AttackSessionToken previousToken = HostSessionToken != null ? HostSessionToken.Clone() : null;
            AttackSessionToken previousPlanToken = PlanSessionToken != null ? PlanSessionToken.Clone() : null;
            string nextResultId = bindingState != null && bindingState.IsAvailable ? bindingState.ResultId : null;
            AttackSessionToken nextSessionToken = bindingState != null && bindingState.IsAvailable
                ? bindingState.SessionToken
                : null;
            VerbProperties nextBindingVerbProps = bindingState != null && bindingState.VerbProps != null ? bindingState.VerbProps : fallbackVerbProps;
            Tool nextBindingTool = bindingState != null ? bindingState.Tool : null;
            ManeuverDef nextBindingManeuver = bindingState != null ? bindingState.Maneuver : null;
            string resetReason = DescribeBindingResetReason(bindingState, fallbackVerbProps);
            bool resetApplied = ShouldResetForBindingChange(bindingState, fallbackVerbProps);
            if (resetApplied)
            {
                AttackExecutionDiagnostics.LogFormalHostBindingReset(
                    CasterPawn,
                    this,
                    slot,
                    WeaponExpressionMode.Melee,
                    loadID,
                    previousResultId,
                    nextResultId,
                    previousToken,
                    previousPlanToken,
                    NextRuntimeStepIndex,
                    resetReason);
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
                    WeaponExpressionMode.Melee,
                    loadID,
                    previousResultId,
                    nextResultId,
                    resetApplied);
            }

            ResultId = nextResultId;
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

            bindingVerbProps = nextBindingVerbProps;
            bindingTool = nextBindingTool;
            bindingManeuver = nextBindingManeuver;
            verbProps = nextBindingVerbProps;
            tool = nextBindingTool;
            maneuver = nextBindingManeuver;
            caster = owner != null ? owner.OwnerPawn : caster;
            verbTracker = owner != null ? owner.VerbTracker : verbTracker;
        }

        /// <summary>
        /// 当前 formal host 会话开始新一轮近战前，按原版兼容权重预排这一轮的 Tool 序列。
        /// BDP 只记录序列结果，不把选择权重新交回原版 VerbTracker。
        /// </summary>
        internal void PrepareStepToolSequenceForCurrentRound(LocalTargetInfo target, int plannedStepCount, int roundOrdinal)
        {
            BdpFormalVerbBinding binding = owner != null ? owner.VerbHostManager.TryGetBinding(slot) : null;
            IReadOnlyList<MeleeToolSurface> candidateSurfaces = binding?.State?.DeclaredMeleeToolSurfaces;
            if (candidateSurfaces == null || candidateSurfaces.Count == 0)
            {
                ApplyPreparedStepToolIndices(new List<int> { 0 });
                return;
            }

            IReadOnlyList<int> preparedStepToolIndices = VanillaCompatibleMeleeToolSelector.PrepareStepToolSequence(
                owner != null ? owner.OwnerPawn : caster as Pawn,
                target,
                ResolveBoundResult(binding),
                candidateSurfaces,
                plannedStepCount,
                AttackInstanceId,
                roundOrdinal);
            ApplyPreparedStepToolIndices(preparedStepToolIndices);
        }

        /// <summary>
        /// 按当前轮预排结果把 formal host 切到指定 step 应使用的 Tool 表面。
        /// 它只修改当前这刀的运行时表面，不改变 binding 层宣称的稳定身份。
        /// </summary>
        internal void ApplyStepToolSurface(int stepIndex)
        {
            MeleeToolSurface surface = ResolvePreparedStepToolSurface(stepIndex);
            if (surface == null)
            {
                surface = BuildFallbackSurface();
            }

            if (surface == null)
            {
                return;
            }

            verbProps = surface.VerbProps ?? bindingVerbProps;
            tool = surface.Tool ?? bindingTool;
            maneuver = surface.Maneuver ?? bindingManeuver;
        }

        /// <summary>
        /// formal host 复位时同时清空 binding 基面缓存。
        /// 这样新的 binding 同步不会把旧结果的基面残留进来。
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            bindingVerbProps = null;
            bindingTool = null;
            bindingManeuver = null;
        }

        /// <summary>
        /// 当 binding 身份或声明面切换时，正式宿主壳必须重置旧会话状态。
        /// 否则旧近战状态会残留到新的表达结果上。
        /// </summary>
        private bool ShouldResetForBindingChange(BdpFormalVerbBindingState bindingState, VerbProperties fallbackVerbProps)
        {
            string nextResultId = bindingState != null && bindingState.IsAvailable ? bindingState.ResultId : null;
            VerbProperties nextVerbProps = bindingState != null && bindingState.VerbProps != null ? bindingState.VerbProps : fallbackVerbProps;
            Tool nextTool = bindingState != null ? bindingState.Tool : null;
            ManeuverDef nextManeuver = bindingState != null ? bindingState.Maneuver : null;
            if (preserveLoadedStateOnce && ResultId == nextResultId)
            {
                preserveLoadedStateOnce = false;
                return false;
            }

            preserveLoadedStateOnce = false;
            return ResultId != nextResultId
                || bindingVerbProps != nextVerbProps
                || bindingTool != nextTool
                || bindingManeuver != nextManeuver;
        }

        /// <summary>
        /// 仅用于诊断输出：说明这次 binding 同步为什么会触发 reset。
        /// 它不参与业务判断，只把边界事实翻译成人能看懂的原因串。
        /// </summary>
        private string DescribeBindingResetReason(BdpFormalVerbBindingState bindingState, VerbProperties fallbackVerbProps)
        {
            string nextResultId = bindingState != null && bindingState.IsAvailable ? bindingState.ResultId : null;
            VerbProperties nextVerbProps = bindingState != null && bindingState.VerbProps != null ? bindingState.VerbProps : fallbackVerbProps;
            Tool nextTool = bindingState != null ? bindingState.Tool : null;
            ManeuverDef nextManeuver = bindingState != null ? bindingState.Maneuver : null;
            if (preserveLoadedStateOnce && ResultId == nextResultId)
            {
                return "preserve_loaded_state_once";
            }

            List<string> reasons = new List<string>();
            if (ResultId != nextResultId)
            {
                reasons.Add("result_id_changed");
            }

            if (bindingVerbProps != nextVerbProps)
            {
                reasons.Add("verb_props_changed");
            }

            if (bindingTool != nextTool)
            {
                reasons.Add("tool_changed");
            }

            if (bindingManeuver != nextManeuver)
            {
                reasons.Add("maneuver_changed");
            }

            return reasons.Count > 0
                ? string.Join("|", reasons)
                : "binding_unchanged";
        }

        /// <summary>
        /// 仅用于诊断比较：判断前后 formal host 会话令牌是否等价。
        /// </summary>
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
        /// 解析当前 step 应该绑定到 formal host 的 Tool 表面。
        /// </summary>
        private MeleeToolSurface ResolvePreparedStepToolSurface(int stepIndex)
        {
            BdpFormalVerbBinding binding = owner != null ? owner.VerbHostManager.TryGetBinding(slot) : null;
            IReadOnlyList<MeleeToolSurface> candidateSurfaces = binding?.State?.DeclaredMeleeToolSurfaces;
            if (candidateSurfaces == null || candidateSurfaces.Count == 0)
            {
                return null;
            }

            int preparedToolIndex = ResolvePreparedStepToolIndex(stepIndex);
            if (preparedToolIndex < 0 || preparedToolIndex >= candidateSurfaces.Count)
            {
                preparedToolIndex = 0;
            }

            return candidateSurfaces[preparedToolIndex];
        }

        /// <summary>
        /// 在还没有多 Tool 候选集时，用 binding 当前基面构造最小回退表面。
        /// </summary>
        private MeleeToolSurface BuildFallbackSurface()
        {
            if (bindingVerbProps == null)
            {
                return null;
            }

            return new MeleeToolSurface
            {
                Tool = bindingTool,
                VerbProps = bindingVerbProps,
                Maneuver = bindingManeuver,
                DamageDef = bindingVerbProps.meleeDamageDef,
                DeclaredIndex = 0
            };
        }

        /// <summary>
        /// 从当前 binding 解析 formal host 正在承接的正式结果。
        /// selector 只需要它的稳定标识参与种子计算。
        /// </summary>
        private FormalExpressionResult ResolveBoundResult(BdpFormalVerbBinding binding)
        {
            if (owner?.PublishedCombatProjection?.ResultIndex == null
                || binding == null
                || string.IsNullOrWhiteSpace(binding.ResultId))
            {
                return null;
            }

            owner.PublishedCombatProjection.ResultIndex.TryGetValue(binding.ResultId, out FormalExpressionResult result);
            return result;
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
        /// 判断当前读档恢复回来的近战 formal host 会话是否仍可续接。
        /// 近战没有 burst cursor，只校验 binding 身份是否仍然自洽。
        /// </summary>
        internal bool CanResumeLoadedSession()
        {
            BdpFormalVerbBinding binding = owner != null ? owner.VerbHostManager.TryGetBinding(slot) : null;
            return binding != null
                && binding.IsAvailable
                && binding.ResolveActiveVerb() == this
                && HostSessionToken != null
                && !string.IsNullOrWhiteSpace(HostSessionToken.ResultId)
                && binding.ResultId == HostSessionToken.ResultId;
        }

        /// <summary>
        /// 当前正式近战宿主壳是否仍需要进入 active tick 队列。
        /// 只有 binding 身份仍然自洽，且近战宿主自身仍有活跃运行时时才继续 tick。
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
        /// 当正式近战宿主壳被原版战斗链路选中起手时，主动把自己挂入 owner 的活跃 tick 队列。
        /// 这样近战后续推进不需要再靠全 binding 常驻扫描。
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
    }
}
