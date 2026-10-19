using System.Collections.Generic;
using System.Linq;
using BDP.Core.AttackExecution;
using BDP.Core.Expressions;
using BDP.Core.Semantics;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.Verbs
{
    /// <summary>
    /// BDP 近战伤害 Verb 宿主。
    /// 第一阶段保留原版近战伤害规则，只在伤害真正落地前压入当前攻击语义。
    /// </summary>
    public partial class BdpVerb_MeleeAttackDamage : Verb_MeleeAttackDamage, IBdpSemanticCarrier, IAttackEffectTraceCarrier
    {
        /// <summary>
        /// 把 BDP 的 burstShotCount（连续攻击次数）接入原版近战实际连击初始化。
        /// 原版近战默认 ShotsPerBurst（本轮实际攻击次数）为 1，若不重写则只会执行首击。
        /// </summary>
        protected override int ShotsPerBurst => BurstShotCount;

        /// <summary>
        /// 当前 burst 最近一击是否真正命中目标。
        /// 它只保存瞬时结果，不参与存档。
        /// </summary>
        internal bool LastMeleeBurstShotHit { get; private set; }

        /// <summary>
        /// 当前 burst 最近一击是否已经完成合法的近战判定。
        /// miss/dodge（未命中/闪避）也属于已完成的一击，但目标失效等硬失败不属于。
        /// 通常只作瞬时判定；已起手的自动有限计划单独保存它，防止读档误取消剩余段。
        /// </summary>
        internal bool LastMeleeBurstShotCompleted { get; private set; }

        /// <summary>
        /// 清除一次 burst 入口尚未产生结果时的瞬时状态。
        /// </summary>
        internal void ClearLastMeleeBurstShotResult()
        {
            LastMeleeBurstShotHit = false;
            LastMeleeBurstShotCompleted = false;
        }

        /// <summary>
        /// 当前近战轮每个 step 结束后的等待 tick 序列。
        /// </summary>
        private List<int> stepIntervalTicks;

        /// <summary>
        /// 当前近战轮预排好的 step-tool 索引序列。
        /// </summary>
        private List<int> preparedStepToolIndices;

        /// <summary>
        /// 当前近战起手对应的正式结果标识。
        /// 它服务读档后会话身份恢复与语义回溯。
        /// </summary>
        private string resultId;

        /// <summary>
        /// 当前近战会话最近一次命中的投影版本号。
        /// 它服务版本失效校验，避免旧近战会话跨投影继续执行。
        /// </summary>
        private AttackSessionToken hostSessionToken;

        /// <summary>
        /// 当前整次近战计划对应的正式会话令牌。
        /// 它用于 run（连续段）之间的正式续接，不等于当前宿主会话身份。
        /// </summary>
        private AttackSessionToken planSessionToken;

        /// <summary>
        /// 当前整次近战计划冻结下来的攻击上下文快照。
        /// 它用于后续 run 续接时重建正式请求，不回表达层重算业务。
        /// </summary>
        private AttackContextSnapshot planAttackContextSnapshot;

        /// <summary>
        /// 当前 run 打完后应继续消费哪一个 runtime step。
        /// 没有后续时为 -1；持续攻击整轮收口后可回到 0。
        /// </summary>
        private int nextRuntimeStepIndex = -1;

        /// <summary>
        /// 当前整次近战计划的派单意图。
        /// 它用于续接时保持与首段一致的正式语义。
        /// </summary>
        private AttackDispatchIntent planDispatchIntent;

        /// <summary>
        /// 当前整次近战计划最初来自哪条正式入口。
        /// 它只服务续接请求重建与诊断，不承担业务判断。
        /// </summary>
        private AttackExecutionReason planReason;

        /// <summary>
        /// 当前近战起手所属的攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前近战起手对应的正式结果标识。
        /// </summary>
        public string ResultId
        {
            get { return resultId; }
            set { resultId = value; }
        }

        /// <summary>
        /// 当前近战 formal host 会话持有的正式攻击会话令牌。
        /// 它服务版本失效校验和读档后的会话重绑。
        /// </summary>
        internal AttackSessionToken HostSessionToken
        {
            get { return hostSessionToken; }
            set { hostSessionToken = value; }
        }

        /// <summary>
        /// 当前整次近战计划对应的正式会话令牌。
        /// </summary>
        internal AttackSessionToken PlanSessionToken
        {
            get { return planSessionToken; }
            set { planSessionToken = value; }
        }

        /// <summary>
        /// 当前整次近战计划冻结下来的攻击上下文快照。
        /// </summary>
        internal AttackContextSnapshot PlanAttackContextSnapshot
        {
            get { return planAttackContextSnapshot; }
            set { planAttackContextSnapshot = value; }
        }

        /// <summary>
        /// 当前 run 打完后应继续消费的 runtime step 索引。
        /// </summary>
        internal int NextRuntimeStepIndex
        {
            get { return nextRuntimeStepIndex; }
            set { nextRuntimeStepIndex = value; }
        }

        /// <summary>
        /// 当前整次近战计划的派单意图。
        /// </summary>
        internal AttackDispatchIntent PlanDispatchIntent
        {
            get { return planDispatchIntent; }
            set { planDispatchIntent = value; }
        }

        /// <summary>
        /// 当前整次近战计划最初来自哪条正式入口。
        /// </summary>
        internal AttackExecutionReason PlanReason
        {
            get { return planReason; }
            set { planReason = value; }
        }

        /// <summary>
        /// 当前这次近战攻击携带的语义。
        /// </summary>
        public ISemanticContext SemanticContext { get; set; }

        /// <summary>
        /// 当前近战计划携带的可选命中部位策略。
        /// 具体命中部位不在这里冻结，避免连击沿用已经失效的旧部位。
        /// </summary>
        private HitPartPolicyConfig hitPartPolicy;

        /// <summary>
        /// 序列化近战 formal host 会话跨档续接所需的最小版本身份。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref resultId, "resultId");
            Scribe_Deep.Look(ref hostSessionToken, "hostSessionToken");
            Scribe_Deep.Look(ref planSessionToken, "planSessionToken");
            Scribe_Deep.Look(ref planAttackContextSnapshot, "planAttackContextSnapshot");
            Scribe_Values.Look(ref nextRuntimeStepIndex, "nextRuntimeStepIndex", -1);
            Scribe_Values.Look(ref planDispatchIntent, "planDispatchIntent", AttackDispatchIntent.ImmediateCast);
            Scribe_Values.Look(ref planReason, "planReason", AttackExecutionReason.Manual);
            Scribe_Collections.Look(ref stepIntervalTicks, "stepIntervalTicks", LookMode.Value);
            Scribe_Collections.Look(ref preparedStepToolIndices, "preparedStepToolIndices", LookMode.Value);
            ExposeAutomaticContinuation();
        }

        /// <summary>
        /// 把正式执行层整理好的最小上下文绑定到当前近战 Verb。
        /// Verb 仍只关心真正伤害落地时需要带上的信息。
        /// </summary>
        internal void ApplyExecutionContext(MeleeAttackExecutionContext context)
        {
            CancelAutomaticContinuation();
            AttackSessionToken previousSessionToken = HostSessionToken != null ? HostSessionToken.Clone() : null;
            if (context == null)
            {
                AttackExecutionVisualRuntimeBridge.Clear(CasterPawn, previousSessionToken);
                AttackInstanceId = null;
                ResultId = null;
                HostSessionToken = null;
                PlanSessionToken = null;
                PlanAttackContextSnapshot = null;
                NextRuntimeStepIndex = -1;
                PlanDispatchIntent = AttackDispatchIntent.ImmediateCast;
                PlanReason = AttackExecutionReason.Manual;
                SemanticContext = null;
                hitPartPolicy = null;
                return;
            }

            AttackInstanceId = context.Cast != null ? context.Cast.AttackInstanceId : null;
            ResultId = context.Result != null ? context.Result.Id : null;
            HostSessionToken = AttackSessionToken.Create(
                context.Pawn,
                ResultId,
                context.ProjectionVersion,
                AttackInstanceId);
            PlanSessionToken = context.PlanSessionToken != null
                ? context.PlanSessionToken.Clone()
                : null;
            PlanAttackContextSnapshot = context.AttackContextSnapshot;
            NextRuntimeStepIndex = context.NextRuntimeStepIndex;
            PlanDispatchIntent = context.PlanDispatchIntent;
            PlanReason = context.PlanReason;
            SemanticContext = context.Result != null ? context.Result.SemanticContext : null;
            hitPartPolicy = context.HitPartPolicy != null ? context.HitPartPolicy.Clone() : null;
            stepIntervalTicks = context.StepIntervalTicks != null
                ? new List<int>(context.StepIntervalTicks)
                : null;
            ApplyPreparedStepToolIndices(context.PreparedStepToolIndices);
            AttackExecutionVisualRuntimeBridge.Publish(context);
        }

        /// <summary>
        /// 在原版 burst 的每一击真正执行前重新确认目标合法。
        /// 原版近战 TryCastShot 对失去射程的情况只记录警告，BDP 虚拟攻击不能接受这种继续命中的结果。
        /// </summary>
        protected override bool TryCastShot()
        {
            ClearLastMeleeBurstShotResult();
            LocalTargetInfo target = CurrentTarget;
            bool targetValid = target.IsValid;
            bool canHitTarget = targetValid && CanHitTarget(target);
            if (!canHitTarget)
            {
                AttackExecutionDiagnostics.LogMeleeBurstShot(
                    CasterPawn,
                    this,
                    target,
                    Find.TickManager != null ? Find.TickManager.TicksGame : -1,
                    targetValid,
                    false,
                    false,
                    "bdp_target_precheck_rejected");
                return false;
            }

            Pawn casterPawn = CasterPawn;
            bool ownBurstCooldown = IsOwnBurstCooldownStance(casterPawn);
            bool dualMeleeHandoffCooldown = !ownBurstCooldown
                && IsPreviousDualMeleeHandoffCooldown(casterPawn);
            if (casterPawn == null
                || !casterPawn.Spawned
                || casterPawn.stances == null
                || (casterPawn.stances.FullBodyBusy
                    && !ownBurstCooldown
                    && !dualMeleeHandoffCooldown))
            {
                AttackExecutionDiagnostics.LogMeleeBurstShot(
                    casterPawn,
                    this,
                    target,
                    Find.TickManager != null ? Find.TickManager.TicksGame : -1,
                    true,
                    true,
                    false,
                    "bdp_caster_precheck_rejected");
                return false;
            }

            Stance originalStance = null;
            Stance temporaryStance = null;
            if (ownBurstCooldown || dualMeleeHandoffCooldown)
            {
                // 原版会把冷却姿态视为不可攻击；这里只临时放行本轮连击间隔，
                // 或同一双持计划中主手到副手的那一次冷却交接。
                originalStance = casterPawn.stances.curStance;
                temporaryStance = new Stance_Mobile();
                casterPawn.stances.curStance = temporaryStance;
            }

            bool hitResult;
            try
            {
                hitResult = base.TryCastShot();
            }
            finally
            {
                if (originalStance != null
                    && ReferenceEquals(casterPawn.stances.curStance, temporaryStance))
                {
                    casterPawn.stances.curStance = originalStance;
                }
            }

            LastMeleeBurstShotHit = hitResult;
            LastMeleeBurstShotCompleted = true;
            AttackExecutionDiagnostics.LogMeleeBurstShot(
                casterPawn,
                this,
                target,
                Find.TickManager != null ? Find.TickManager.TicksGame : -1,
                true,
                true,
                hitResult,
                "vanilla_melee_try_cast_shot");

            // 原版 TryCastNextBurstShot 会把 false 视为“终止整轮 burst”。
            // 对已完成的 BDP 近战 miss/dodge，返回 true 只表示消耗这一击并继续，真实命中结果由上面的状态单独保留。
            return hitResult || LastMeleeBurstShotCompleted;
        }

        /// <summary>
        /// 判断当前身体忙碌是否正是本 Verb 连击之间由原版设置的等待姿态。
        /// 只有这一种忙碌可以被当前 burst 临时放行。
        /// </summary>
        private bool IsOwnBurstCooldownStance(Pawn pawn)
        {
            if (pawn == null
                || pawn.stances == null
                || state != VerbState.Bursting
                || pawn.stances.stunner == null
                || pawn.stances.stunner.Stunned)
            {
                return false;
            }

            Stance_Cooldown cooldownStance = pawn.stances.curStance as Stance_Cooldown;
            return cooldownStance != null && cooldownStance.verb == this;
        }

        /// <summary>
        /// 判断当前完整冷却是否正好位于双持近战主手与副手之间。
        /// 该条件供两个续接入口判断是否可提前交接；普通近战和双持计划收尾均不满足。
        /// </summary>
        internal bool CanSkipDualMeleeHandoffCooldown()
        {
            Pawn pawn = CasterPawn;
            if (pawn == null
                || pawn.stances == null
                || !pawn.stances.FullBodyBusy
                || pawn.stances.stunner == null
                || pawn.stances.stunner.Stunned
                || !IsDualMeleeMainSideWithPendingSub(pawn, null))
            {
                return false;
            }

            Stance_Cooldown cooldownStance = pawn.stances.curStance as Stance_Cooldown;
            return cooldownStance != null && cooldownStance.verb == this;
        }

        /// <summary>
        /// 判断当前宿主是否持有有效的双持主手段，并且计划下一步确实是副手段。
        /// 未提供副手宿主时只检查当前段，供续接入口放行；提供时还校验即将起手的副手宿主。
        /// </summary>
        private bool IsDualMeleeMainSideWithPendingSub(
            Pawn pawn,
            BdpVerb_MeleeAttackDamage nextVerb)
        {
            if (pawn == null
                || pawn != CasterPawn
                || state != VerbState.Idle
                || WarmingUp
                || Bursting
                || !LastMeleeBurstShotCompleted
                || NextRuntimeStepIndex <= 0
                || PlanSessionToken == null
                || !PlanSessionToken.IsValid
                || !PlanSessionToken.BelongsTo(pawn)
                || !AttackExecutionPostLoadRecovery.IsCurrentAttackSessionValid(this)
                || !AttackExecutionSurfaceAccess.TryGetPublishedResult(
                    pawn,
                    PlanSessionToken.ResultId,
                    out var projection,
                    out FormalExpressionResult dualResult)
                || projection.ProjectionVersion != PlanSessionToken.ProjectionVersion
                || dualResult.CompositeKind != CompositeExpressionKind.DualWeapon
                || projection.CompositeReferenceIndex == null
                || !projection.CompositeReferenceIndex.TryGetValue(dualResult.Id, out CompositeExpressionReference reference)
                || reference == null
                || reference.CompositeKind != CompositeExpressionKind.DualWeapon)
            {
                return false;
            }

            string mainSourceResultId = !string.IsNullOrWhiteSpace(reference.MainSourceResultId)
                ? reference.MainSourceResultId
                : reference.SourceResultIds != null && reference.SourceResultIds.Count > 0
                    ? reference.SourceResultIds[0]
                    : null;
            string subSourceResultId = !string.IsNullOrWhiteSpace(reference.SubSourceResultId)
                ? reference.SubSourceResultId
                : reference.SourceResultIds != null && reference.SourceResultIds.Count > 1
                    ? reference.SourceResultIds[1]
                    : null;

            if (string.IsNullOrWhiteSpace(mainSourceResultId)
                || string.IsNullOrWhiteSpace(subSourceResultId)
                || mainSourceResultId == subSourceResultId
                || ResultId != mainSourceResultId)
            {
                return false;
            }

            return nextVerb == null
                || (nextVerb.ResultId == subSourceResultId
                    && nextVerb.PlanSessionToken != null
                    && nextVerb.PlanSessionToken.Equals(PlanSessionToken));
        }

        /// <summary>
        /// 判断当前击是否只需临时跨过上一侧双持近战宿主留下的完整冷却姿态。
        /// </summary>
        private bool IsPreviousDualMeleeHandoffCooldown(Pawn pawn)
        {
            if (pawn == null
                || pawn.stances == null
                || pawn.stances.stunner == null
                || pawn.stances.stunner.Stunned
                || !(pawn.stances.curStance is Stance_Cooldown cooldownStance)
                || !(cooldownStance.verb is BdpVerb_MeleeAttackDamage previousVerb)
                || previousVerb == this)
            {
                return false;
            }

            return previousVerb.IsDualMeleeMainSideWithPendingSub(pawn, this);
        }

        /// <summary>
        /// 清空当前近战宿主挂着的瞬时执行态，避免旧会话状态残留到新结果上。
        /// </summary>
        public override void Reset()
        {
            LogSessionClearedIfNeeded("reset");
            CancelAutomaticContinuation();
            AttackSessionToken previousSessionToken = HostSessionToken != null ? HostSessionToken.Clone() : null;
            base.Reset();
            AttackExecutionVisualRuntimeBridge.Clear(CasterPawn, previousSessionToken);
            AttackInstanceId = null;
            ResultId = null;
            HostSessionToken = null;
            PlanSessionToken = null;
            PlanAttackContextSnapshot = null;
            NextRuntimeStepIndex = -1;
            PlanDispatchIntent = AttackDispatchIntent.ImmediateCast;
            PlanReason = AttackExecutionReason.Manual;
            SemanticContext = null;
            hitPartPolicy = null;
            ClearLastMeleeBurstShotResult();
            stepIntervalTicks = null;
            preparedStepToolIndices = null;
        }

        /// <summary>
        /// 当近战宿主的正式会话真值即将被清空时输出一条诊断日志。
        /// 只在仍持有攻击实例或续接状态时记录，避免普通空壳 reset 刷屏。
        /// </summary>
        private void LogSessionClearedIfNeeded(string reason)
        {
            if (HostSessionToken == null
                && PlanSessionToken == null
                && string.IsNullOrWhiteSpace(AttackInstanceId)
                && string.IsNullOrWhiteSpace(ResultId)
                && NextRuntimeStepIndex < 0)
            {
                return;
            }

            AttackExecutionDiagnostics.LogMeleeVerbSessionCleared(
                CasterPawn,
                this,
                HostSessionToken,
                PlanSessionToken,
                AttackInstanceId,
                ResultId,
                NextRuntimeStepIndex,
                PlanDispatchIntent,
                PlanReason,
                reason);
        }

        /// <summary>
        /// 当前近战会话是否仍保留可供续接的下一段索引。
        /// </summary>
        internal bool HasPendingContinuation()
        {
            return NextRuntimeStepIndex >= 0;
        }

        /// <summary>
        /// 记录当前轮已经预排好的 step-tool 索引序列。
        /// 这组索引只服务当前近战轮次与读档恢复，不直接替代表达层候选集。
        /// </summary>
        internal void ApplyPreparedStepToolIndices(IReadOnlyList<int> preparedStepToolIndices)
        {
            this.preparedStepToolIndices = preparedStepToolIndices != null
                ? new List<int>(preparedStepToolIndices)
                : null;
        }

        internal int ResolveIntervalTicksAfterStep(int stepIndex)
        {
            int stepCount = ResolvePreparedStepCount();
            if (stepIntervalTicks == null || stepCount <= 0 || stepIndex < 0)
            {
                return 0;
            }

            if (stepIndex >= stepCount - 1)
            {
                return 0;
            }

            return stepIntervalTicks[stepIndex];
        }

        /// <summary>
        /// 读取当前轮为指定 step 预排的 Tool 索引。
        /// 若当前没有预排结果，则回退到第 0 把 Tool。
        /// </summary>
        internal int ResolvePreparedStepToolIndex(int stepIndex)
        {
            if (preparedStepToolIndices == null || preparedStepToolIndices.Count == 0 || stepIndex < 0)
            {
                return 0;
            }

            return stepIndex < preparedStepToolIndices.Count
                ? preparedStepToolIndices[stepIndex]
                : preparedStepToolIndices[preparedStepToolIndices.Count - 1];
        }

        /// <summary>
        /// 当前轮已经预排好的 Tool 数量。
        /// 这用于判断是否需要在新一轮开始前重新准备 step-tool 序列。
        /// </summary>
        internal int ResolvePreparedStepToolCount()
        {
            return preparedStepToolIndices != null
                ? preparedStepToolIndices.Count
                : 0;
        }

        internal int ResolvePreparedStepCount()
        {
            return stepIntervalTicks != null && stepIntervalTicks.Count > 0
                ? stepIntervalTicks.Count
                : 1;
        }

        /// <summary>
        /// 判断当前近战 formal host 是否仍需要留在活跃 tick 队列中。
        /// 自动有限计划还须跨过最终冷却推进下一段；手动持续计划仍由原任务推进器负责。
        /// </summary>
        internal bool RequiresFormalHostRuntimeTick()
        {
            return WarmingUp || Bursting || automaticPlanStarted;
        }

        /// <summary>
        /// 复制原版近战伤害落地循环。
        /// 差别只有一处：每个 `DamageInfo` 真正打到目标前，都先压入当前近战表达的攻击语义。
        /// </summary>
        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            DamageWorker.DamageResult result = new DamageWorker.DamageResult();
            foreach (DamageInfo item in BuildDamageInfosToApply(target))
            {
                if (target.ThingDestroyed)
                {
                    break;
                }

                DamageResolution resolution;
                result = DamageResolutionRuntime.ApplyDamage(target.Thing, item, out resolution, SemanticContext);

            }

            return result;
        }

        /// <summary>
        /// 按原版近战规则整理这次命中要依次施加的伤害包。
        /// 这里仍负责“怎么算伤害”，不直接负责“把伤害打进去”。
        /// </summary>
        private IEnumerable<DamageInfo> BuildDamageInfosToApply(LocalTargetInfo target)
        {
            float num = verbProps.AdjustedMeleeDamageAmount(this, CasterPawn);
            float armorPenetration = verbProps.AdjustedArmorPenetration(this, CasterPawn);
            DamageDef damageDef = verbProps.meleeDamageDef;
            BodyPartGroupDef bodyPartGroup = null;
            HediffDef hediffDef = null;
            QualityCategory quality = QualityCategory.Normal;
            num = Rand.Range(num * 0.8f, num * 1.2f);
            if (CasterIsPawn)
            {
                bodyPartGroup = verbProps.AdjustedLinkedBodyPartsGroup(tool);
                if (num >= 1f)
                {
                    if (base.HediffCompSource != null)
                    {
                        hediffDef = base.HediffCompSource.Def;
                    }
                }
                else
                {
                    num = 1f;
                    damageDef = DamageDefOf.Blunt;
                }
            }

            ThingDef sourceDef;
            if (base.EquipmentSource != null)
            {
                sourceDef = base.EquipmentSource.def;
                base.EquipmentSource.TryGetQuality(out quality);
            }
            else
            {
                sourceDef = CasterPawn.def;
            }

            Vector3 direction = (target.Thing.Position - CasterPawn.Position).ToVector3();
            bool instigatorGuilty = !(caster is Pawn pawn) || !pawn.Drafted;
            BodyPartRecord hitPart = null;
            if (hitPartPolicy != null)
            {
                HitPartPolicyResolverAccess.TryResolve(
                    CasterPawn,
                    target.Thing,
                    damageDef,
                    bodyPartGroup,
                    hitPartPolicy,
                    out hitPart);
            }

            DamageInfo damageInfo = new DamageInfo(damageDef, num, armorPenetration, -1f, caster, hitPart, sourceDef, DamageInfo.SourceCategory.ThingOrUnknown, null, instigatorGuilty);
            damageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
            damageInfo.SetWeaponBodyPartGroup(bodyPartGroup);
            damageInfo.SetWeaponHediff(hediffDef);
            damageInfo.SetAngle(direction);
            damageInfo.SetTool(tool);
            damageInfo.SetWeaponQuality(quality);
            yield return damageInfo;

            // 处理工具本身声明的额外近战伤害。
            if (tool != null && tool.extraMeleeDamages != null)
            {
                foreach (ExtraDamage extraMeleeDamage in tool.extraMeleeDamages)
                {
                    if (Rand.Chance(extraMeleeDamage.chance))
                    {
                        num = extraMeleeDamage.amount;
                        num = Rand.Range(num * 0.8f, num * 1.2f);
                        damageInfo = new DamageInfo(extraMeleeDamage.def, num, extraMeleeDamage.AdjustedArmorPenetration(this, CasterPawn), -1f, caster, null, sourceDef);
                        damageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                        damageInfo.SetWeaponBodyPartGroup(bodyPartGroup);
                        damageInfo.SetWeaponHediff(hediffDef);
                        damageInfo.SetAngle(direction);
                        yield return damageInfo;
                    }
                }
            }

            // 处理原版近战里“突袭命中”附带的额外伤害。
            if (!surpriseAttack || ((verbProps.surpriseAttack == null || verbProps.surpriseAttack.extraMeleeDamages.NullOrEmpty()) && (tool == null || tool.surpriseAttack == null || tool.surpriseAttack.extraMeleeDamages.NullOrEmpty())))
            {
                yield break;
            }

            IEnumerable<ExtraDamage> extraDamages = Enumerable.Empty<ExtraDamage>();
            if (verbProps.surpriseAttack != null && verbProps.surpriseAttack.extraMeleeDamages != null)
            {
                extraDamages = extraDamages.Concat(verbProps.surpriseAttack.extraMeleeDamages);
            }

            if (tool != null && tool.surpriseAttack != null && !tool.surpriseAttack.extraMeleeDamages.NullOrEmpty())
            {
                extraDamages = extraDamages.Concat(tool.surpriseAttack.extraMeleeDamages);
            }

            foreach (ExtraDamage item in extraDamages)
            {
                int amount = GenMath.RoundRandom(item.AdjustedDamageAmount(this, CasterPawn));
                float extraArmorPenetration = item.AdjustedArmorPenetration(this, CasterPawn);
                DamageInfo extraDamageInfo = new DamageInfo(item.def, amount, extraArmorPenetration, -1f, caster, null, sourceDef);
                extraDamageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                extraDamageInfo.SetWeaponBodyPartGroup(bodyPartGroup);
                extraDamageInfo.SetWeaponHediff(hediffDef);
                extraDamageInfo.SetAngle(direction);
                yield return extraDamageInfo;
            }
        }
    }
}
