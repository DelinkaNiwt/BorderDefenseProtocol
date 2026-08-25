using System.Linq;
using BDP.Core.Expressions;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using BDP.Core.VerbHosting;
using BDP.Core.Verbs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// AttackExecution 对内接口获取面。
    /// 主模组内其它系统统一从这里拿正式执行入口，不直接自己拼装执行器。
    /// </summary>
    internal static class AttackExecutionSurfaceAccess
    {
        /// <summary>
        /// 当前默认使用的正式攻击执行入口。
        /// </summary>
        

        /// <summary>
        /// 读取正式攻击执行入口。
        /// 其它系统只允许从这里进入正式执行边界，不自行拼执行器组合。
        /// </summary>
        public static AttackExecutionService ResolveEntry(Pawn pawn)
        {
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            return triggerBody != null ? triggerBody.RuntimeServices?.AttackExecutionService : null;
        }

        /// <summary>
        /// 读取当前 owner 持有的远程模块运行时宿主。
        /// 远程协议与交互前置链都应从这里进入模块会话边界。
        /// </summary>
        internal static RangedAttackModuleRuntimeHost ResolveRangedModuleRuntimeHost(Pawn pawn)
        {
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            return triggerBody != null ? triggerBody.RuntimeServices?.RangedAttackModuleRuntimeHost : null;
        }

        /// <summary>
        /// 读取 Pawn 当前主装备上已发布的战斗投影。
        /// 这条读取口只做纯读，不触发表达重建。
        /// </summary>
        internal static bool TryGetPublishedCombatProjection(Pawn pawn, out TriggerCombatProjectionState projection)
        {
            projection = null;
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            projection = triggerBody != null ? triggerBody.PublishedCombatProjection : null;
            return projection != null && !projection.IsEmpty;
        }

        /// <summary>
        /// 按结果标识读取当前已发布投影里的正式结果。
        /// 它服务攻击执行热路径，不再走读时现算。
        /// </summary>
        internal static bool TryGetPublishedResult(
            Pawn pawn,
            string resultId,
            out TriggerCombatProjectionState projection,
            out FormalExpressionResult result)
        {
            result = null;
            if (!TryGetPublishedCombatProjection(pawn, out projection)
                || string.IsNullOrWhiteSpace(resultId)
                || projection.ResultIndex == null
                || !projection.ResultIndex.TryGetValue(resultId, out result)
                || result == null
                || !result.IsAvailable)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 为指定正式结果创建 targeting 适配源。
        /// 这条工厂口只负责拼装依赖，不在这里承担手动入口专属语义。
        /// </summary>
        public static AttackExecutionTargetingSource CreateTargetingSource(
            Pawn pawn,
            string resultId,
            AttackExecutionReason reason,
            AttackDispatchIntent dispatchIntent)
        {
            if (pawn == null
                || string.IsNullOrWhiteSpace(resultId)
                || ResolveEntry(pawn) == null)
            {
                return null;
            }

            if (!TryCreatePublishedRangedModuleSession(pawn, resultId, out RangedAttackModuleSession moduleSession))
            {
                return null;
            }

            return CreateTargetingSource(
                pawn,
                resultId,
                reason,
                dispatchIntent,
                moduleSession);
        }

        /// <summary>
        /// 用已冻结好的模块会话构造 targeting 适配源。
        /// 这条入口只服务同一次玩家瞄准过程内的稳定会话复用。
        /// </summary>
        internal static AttackExecutionTargetingSource CreateTargetingSource(
            Pawn pawn,
            string resultId,
            AttackExecutionReason reason,
            AttackDispatchIntent dispatchIntent,
            RangedAttackModuleSession moduleSession)
        {
            if (pawn == null
                || string.IsNullOrWhiteSpace(resultId)
                || ResolveEntry(pawn) == null
                || moduleSession == null)
            {
                return null;
            }

            return new AttackExecutionTargetingSource(
                pawn,
                resultId,
                reason,
                dispatchIntent,
                ResolveEntry(pawn),
                moduleSession);
        }

        /// <summary>
        /// 尝试为原版自动远程入口读取表达系统选出的默认远程 formal host 壳。
        /// 自动战斗继续使用原版持续攻击会话，但入口选择不再依赖原版 PrimaryVerb 候选链。
        /// target 用于单侧回退场景下的射程感知主攻选择；为 null 时跳过。
        /// </summary>
        public static bool TryGetAutoRangedVerb(Pawn pawn, bool allowManualCastWeapons, Thing target, out Verb verb)
        {
            verb = null;
            // 对齐原版 Pawn.TryStartAttack 与自动攻击门槛：暴力已禁用时，
            // 不得再由 BDP 后置补丁把任何正式远程攻击 Verb 填回去。
            if (pawn == null
                || pawn.WorkTagIsDisabled(WorkTags.Violent)
                || ResolveEntry(pawn) == null)
            {
                return false;
            }

            if (!TryResolveAutoPrimaryVerb(
                    pawn,
                    WeaponExpressionMode.Ranged,
                    target,
                    out TriggerCombatProjectionState projection,
                    out FormalExpressionResult result,
                    out Verb rangedVerb))
            {
                return false;
            }

            if (!PassesVanillaManualAutoGate(pawn, rangedVerb, allowManualCastWeapons))
            {
                return false;
            }

            if (rangedVerb is BdpVerb_Shoot shootVerb)
            {
                if (!shootVerb.CanAcceptAutoRangedEntryStaging())
                {
                    verb = rangedVerb;
                    return true;
                }

                AttackSessionToken previousToken = shootVerb.HostSessionToken != null
                    ? shootVerb.HostSessionToken.Clone()
                    : null;
                _ = previousToken;
                RangedAttackModuleSession residentSession = shootVerb.HostModuleSession;
                RangedAttackModuleSession stagedSession = CreateRangedModuleSession(pawn, result);
                // 自动远程首发也必须带上命中时刻的正式会话令牌，
                // 否则 TryStartCastOn 内部的正式续射准备会把它当成过期会话直接拒绝。
                AttackSessionToken token = AttackSessionToken.Create(
                    pawn,
                    result.Id,
                    projection.ProjectionVersion);
                shootVerb.StageEntryModuleSession(stagedSession);
                // 自动查询链只准备下一次起手，不得覆盖仍在执行的正式会话令牌。
                // 否则手动 job 结束时的代际清理会误判为非本代而跳过 Reset。
                if (residentSession == null)
                {
                    shootVerb.HostSessionToken = token;
                }
            }

            verb = rangedVerb;
            return true;
        }

        /// <summary>
        /// 复用原版 TryGetAttackVerb 对 onlyManualCast 的自动/手动边界。
        /// 这里只检查 BDP 自动远程是否可被原版自动入口选中，不重新调用 Pawn.TryGetAttackVerb，避免补丁递归。
        /// </summary>
        private static bool PassesVanillaManualAutoGate(Pawn pawn, Verb verb, bool allowManualCastWeapons)
        {
            if (verb?.verbProps == null)
            {
                return false;
            }

            if (!verb.verbProps.onlyManualCast)
            {
                return true;
            }

            if (allowManualCastWeapons)
            {
                return true;
            }

            Job currentJob = pawn?.CurJob;
            return currentJob != null && currentJob.def != JobDefOf.Wait_Combat;
        }

        /// <summary>
        /// 尝试把原版自动近战起手翻译成正式攻击请求。
        /// 只有表达系统给出合法 PrimaryMelee 时才接管；否则完全放行原版近战池。
        /// </summary>
        public static bool TryExecuteAutoMelee(Pawn pawn, Thing target)
        {
            if (pawn == null
                || target == null
                || !target.Spawned
                || ResolveEntry(pawn) == null)
            {
                return false;
            }

            if (!TryResolveAutoPrimaryVerb(
                    pawn,
                    WeaponExpressionMode.Melee,
                    null, // 近战不需要射程感知选择
                    out TriggerCombatProjectionState projection,
                    out FormalExpressionResult result,
                    out Verb meleeVerb))
            {
                return false;
            }

            RangedAttackModuleSession moduleSession = CreateRangedModuleSession(pawn, result);
            return ResolveEntry(pawn).TryExecute(new AttackExecutionRequest
            {
                Pawn = pawn,
                SessionToken = AttackSessionToken.Create(
                    pawn,
                    result.Id,
                    projection.ProjectionVersion),
                AttackContextSnapshot = CreateAttackContextSnapshot(moduleSession),
                Target = target,
                Reason = AttackExecutionReason.AutoMelee,
                DispatchIntent = AttackDispatchIntent.AutoAttackOrder
            });
        }

        /// <summary>
        /// 用当前已发布结果创建一份远程模块会话冻结态。
        /// 这条路径只做一次构造，不参与后续逐帧 targeting 刷新。
        /// </summary>
        internal static bool TryCreatePublishedRangedModuleSession(
            Pawn pawn,
            string resultId,
            out RangedAttackModuleSession moduleSession)
        {
            moduleSession = null;
            if (!TryGetPublishedResult(pawn, resultId, out _, out FormalExpressionResult result))
            {
                return false;
            }

            moduleSession = CreateRangedModuleSession(pawn, result);
            return moduleSession != null;
        }

        /// <summary>
        /// 用指定结果创建一份远程模块会话冻结态。
        /// 协议边界统一只从这里进入模块会话构造。
        /// </summary>
        internal static RangedAttackModuleSession CreateRangedModuleSession(Pawn pawn, FormalExpressionResult result)
        {
            RangedAttackModuleRuntimeHost runtimeHost = ResolveRangedModuleRuntimeHost(pawn);
            return runtimeHost != null && result != null
                ? runtimeHost.CreateSession(pawn, result)
                : null;
        }

        /// <summary>
        /// 用当前已知的冻结节点组装统一攻击上下文快照。
        /// 主模组只负责收口中性节点，不解释节点内部业务含义。
        /// </summary>
        internal static AttackContextSnapshot CreateAttackContextSnapshot(
            RangedAttackModuleSession moduleSession = null,
            ConfirmedInputSnapshot confirmedInput = null,
            ConfirmedInteractionSnapshot confirmedInteraction = null)
        {
            AttackContext attackContext = moduleSession != null && moduleSession.AttackContext != null
                ? moduleSession.AttackContext
                : new AttackContext();

            if (confirmedInput != null)
            {
                attackContext.Set(AttackContextKeys.ConfirmedInput, confirmedInput);
            }

            if (confirmedInteraction != null)
            {
                attackContext.Set(AttackContextKeys.ConfirmedInteraction, confirmedInteraction);
            }

            return attackContext.ToSnapshot();
        }

        /// <summary>
        /// 解析指定 Pawn 当前自动攻击应使用的表达主结果及其 formal host 壳。
        /// 这条路径只读表达系统已选结果，不再复用宿主层的“主攻猜测”规则。
        /// </summary>
        private static bool TryResolveAutoPrimaryVerb(
            Pawn pawn,
            WeaponExpressionMode weaponMode,
            Thing target,
            out TriggerCombatProjectionState projection,
            out FormalExpressionResult result,
            out Verb verb)
        {
            projection = null;
            result = null;
            verb = null;
            if (pawn == null)
            {
                return false;
            }

            if (!TryGetPublishedCombatProjection(pawn, out projection))
            {
                return false;
            }

            ExpressionSnapshot snapshot = projection.Snapshot;
            if (snapshot == null)
            {
                return false;
            }

            if (weaponMode == WeaponExpressionMode.Melee)
            {
                result = snapshot.PrimaryMelee;
                if (result == null
                    || !result.IsAvailable
                    || string.IsNullOrWhiteSpace(result.Id)
                    || !VerbHostSurfaceAccess.TryGetByResultId(pawn, result.Id, out BdpFormalVerbBinding binding))
                {
                    return false;
                }

                verb = binding.ResolveActiveVerb();
            }
            else
            {
                result = snapshot.PrimaryRanged;

                // ★ 单侧回退场景：射程感知的主攻选择。
                // 有明确 target 时：一方射程内另一方射程外则用射程内的；都在射程内 Main 优先。
                // 无 target 时（CurrentEffectiveVerb 路径）：选最长射程，确保 BestAttackTarget
                // 不会因 PrimaryRanged 的射程偏短而漏掉 Sub 侧能覆盖到的目标。
                if (result != null
                    && result.CompositeKind != CompositeExpressionKind.DualWeapon)
                {
                    if (target != null)
                        result = SelectSingleSideRangedByRange(snapshot, pawn, target) ?? result;
                    else
                        result = SelectLongestRangeSingleSideRanged(snapshot) ?? result;
                }

                if (result == null
                    || !result.IsAvailable
                    || string.IsNullOrWhiteSpace(result.Id)
                    || !VerbHostSurfaceAccess.TryGetByResultId(pawn, result.Id, out BdpFormalVerbBinding binding))
                {
                    return false;
                }

                verb = binding.ResolveActiveVerb();
            }

            if (verb == null || !verb.Available())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 在单侧远程结果中按目标距离选择最优攻击方。
        /// 一方射程内另一方射程外时用射程内的；都在射程内时 Main 优先（返回 null 走回默认）。
        /// </summary>
        /// <summary>
        /// SelectSingleSideRangedByRange 的公开入口。
        /// 供 CompTriggerBody.PrepareRangedVerbForTarget 在 PrimaryVerb hint 路径复用。
        /// </summary>
        internal static FormalExpressionResult SelectSingleSideRangedByRangePublic(
            ExpressionSnapshot snapshot, Pawn pawn, Thing target)
        {
            return SelectSingleSideRangedByRange(snapshot, pawn, target);
        }

        private static FormalExpressionResult SelectSingleSideRangedByRange(
            ExpressionSnapshot snapshot, Pawn pawn, Thing target)
        {
            if (snapshot?.Results == null || pawn == null || target == null)
                return null;

            var candidates = snapshot.Results
                .Where(r => r.ResultKind == ExpressionResultKind.Verb
                    && r.IsAvailable
                    && r.WeaponMode == WeaponExpressionMode.Ranged
                    && r.CompositeKind == CompositeExpressionKind.None
                    && r.VerbAttackRole == VerbAttackRole.Primary)
                .ToList();
            if (candidates.Count <= 1)
                return null;

            float dist = pawn.Position.DistanceTo(target.Position);

            FormalExpressionResult mainRanged = candidates.FirstOrDefault(
                r => r.OriginKind == ExpressionOriginKind.Main);
            FormalExpressionResult subRanged = candidates.FirstOrDefault(
                r => r.OriginKind == ExpressionOriginKind.Sub);

            bool mainCovered = mainRanged?.VerbProps != null
                && dist <= mainRanged.VerbProps.range;
            bool subCovered = subRanged?.VerbProps != null
                && dist <= subRanged.VerbProps.range;

            if (mainCovered && !subCovered)
                return mainRanged;
            if (subCovered && !mainCovered)
                return subRanged;
            // 都在或都不在射程 → 维持默认（默认 PrimaryRanged 倾向 Main）
            return null;
        }

        /// <summary>
        /// 在单侧远程结果中选择射程最远的一个。
        /// 供 CurrentEffectiveVerb (target==null) 路径使用，
        /// 确保 BestAttackTarget 用最大射程扫描目标而不会过早过滤。
        /// </summary>
        private static FormalExpressionResult SelectLongestRangeSingleSideRanged(
            ExpressionSnapshot snapshot)
        {
            if (snapshot?.Results == null)
                return null;

            FormalExpressionResult best = null;
            float bestRange = 0f;

            foreach (FormalExpressionResult r in snapshot.Results)
            {
                if (r == null
                    || r.ResultKind != ExpressionResultKind.Verb
                    || !r.IsAvailable
                    || r.WeaponMode != WeaponExpressionMode.Ranged
                    || r.CompositeKind != CompositeExpressionKind.None
                    || r.VerbAttackRole != VerbAttackRole.Primary
                    || r.VerbProps == null)
                    continue;

                if (r.VerbProps.range > bestRange)
                {
                    bestRange = r.VerbProps.range;
                    best = r;
                }
            }

            return best;
        }
    }
}
