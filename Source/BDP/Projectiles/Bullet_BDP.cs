using System.Collections.Generic;
using BDP.Projectiles.Pipeline;
using BDP.Projectiles.Config;
using BDP.Projectiles.Modules;
using BDP.Trigger;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Projectiles
{
    /// <summary>
    /// BDP统一投射物宿主——v5管线架构。
    ///
    /// 核心原则：模块只产出意图，宿主统一执行。
    ///   · Phase状态机是模块间唯一协作媒介（模块只读）
    ///   · origin/destination/ticksToImpact 只在 ApplyFlightRedirect 中写入
    ///   · Destroy()/Impact() 只在宿主内部调用
    ///
    /// 管线调度（8阶段）：
    ///   TickInterval:
    ///     0. PostLaunchInit
    ///     1. LifecycleCheck — IBDPLifecyclePolicy
    ///     2. FlightIntent — IBDPFlightIntentProvider
    ///     3. base.TickInterval() — vanilla引擎
    ///     4. PositionModifier — IBDPPositionModifier（保留）
    ///     5. VisualObserve — IBDPVisualObserver
    ///   ImpactSomething:
    ///     6. ArrivalPolicy — IBDPArrivalPolicy
    ///     7. HitResolve — IBDPHitResolver
    ///     8. Impact — IBDPImpactHandler（保留）
    /// </summary>
    public class Bullet_BDP : Bullet
    {
        // ── 模块列表（按Priority升序） ──
        private List<IBDPProjectileModule> modules = new List<IBDPProjectileModule>();

        // ── v5管线参与者缓存（SpawnSetup时建立） ──
        private List<IBDPLifecyclePolicy> lifecyclePolicies;
        private List<IBDPFlightIntentProvider> flightIntentProviders;
        private List<IBDPPositionModifier> positionModifiers;
        private List<IBDPVisualObserver> visualObservers;
        private List<IBDPArrivalPolicy> arrivalPolicies;
        private List<IBDPHitResolver> hitResolvers;
        private List<IBDPImpactHandler> impactHandlers;
        private List<IBDPSpeedModifier> speedModifiers;

        // ── Phase转换观察者缓存 ──
        private List<IBDPPhaseTransitionObserver> phaseObservers;

        // ── 飞行阶段状态机（模块间唯一协作媒介） ──
        /// <summary>
        /// 当前飞行阶段。模块只读，宿主统一管理转换。
        /// </summary>
        public FlightPhase Phase { get; private set; } = FlightPhase.Direct;

        // ── 飞行重定向配置 ──
        /// <summary>从def.modExtensions读取，无配置时用默认值。</summary>
        private FlightRedirectConfig redirectConfig;

        // ── 三层目标模型 ──
        /// <summary>瞄准目标——发射时锁定，不变。</summary>
        public LocalTargetInfo AimTarget { get; private set; }

        /// <summary>锁定目标——通常=AimTarget，仅"重定向"机制可改。</summary>
        public LocalTargetInfo LockedTarget { get; private set; }

        /// <summary>当前目标——此刻飞向谁。引导段=锚点坐标，引导结束/纯追踪=目标实体。</summary>
        public LocalTargetInfo CurrentTarget { get; private set; }

        /// <summary>设置锁定目标（追踪模块切换目标时调用）。</summary>
        public void SetLockedTarget(LocalTargetInfo target) { LockedTarget = target; }

        /// <summary>设置当前目标（引导模块设置锚点/回归目标时调用）。</summary>
        public void SetCurrentTarget(LocalTargetInfo target) { CurrentTarget = target; }

        // ── 穿体穿透（由IBDPImpactHandler模块维护） ──
        /// <summary>穿体穿透剩余力——子弹穿过目标继续飞行的能力。默认0=不穿透。</summary>
        public float PassthroughPower;

        /// <summary>已穿透实体次数（供伤害衰减计算）。</summary>
        public int PassthroughCount;

        // ── Launch后延迟初始化标记 ──
        private bool postLaunchInitDone;

        /// <summary>上一tick是否有模块产出了飞行意图（供LifecycleContext注入）。</summary>
        private bool lastTickHadIntent;

        /// <summary>累计到达重定向次数（诊断 + 防无限循环）。</summary>
        private int arrivalRedirectCount;
        /// <summary>是否已消耗首段重定向（首段不做origin后退）。</summary>
        private bool firstRedirectConsumed;

        // ── Vanilla适配层 ──
        /// <summary>Vanilla兼容适配层——集中处理vanilla机制冲突。</summary>
        private VanillaAdapter vanillaAdapter = new VanillaAdapter();

        // ── 发射上下文 ──
        /// <summary>发射时的游戏tick。</summary>
        public int LaunchTick;

        // ── PositionModifier输出缓存 ──
        private Vector3 modifiedDrawPos;
        private bool hasPositionModifiers;
        /// <summary>发射速度倍率（FireMode注入，1=原速）。</summary>
        private float launchSpeedMult = 1f;

        // ══════════════════════════════════════════
        //  公共API
        // ══════════════════════════════════════════

        /// <summary>获取指定类型的模块实例（供Verb层调用）。</summary>
        public T GetModule<T>() where T : class, IBDPProjectileModule
        {
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i] is T typed) return typed;
            }
            return null;
        }

        /// <summary>获取实现指定管线接口的模块（供外部查询能力）。</summary>
        public T GetCapability<T>() where T : class
        {
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i] is T typed) return typed;
            }
            return null;
        }

        /// <summary>发射速度倍率（只读）。</summary>
        public float LaunchSpeedMult => launchSpeedMult;

        /// <summary>有效每tick速度（基础速度 * 发射速度倍率）。</summary>
        public float EffectiveSpeedTilesPerTick
        {
            get
            {
                float baseSpeed = def?.projectile?.SpeedTilesPerTick ?? 0f;
                float mult = launchSpeedMult > 0.001f ? launchSpeedMult : 1f;
                return baseSpeed * mult;
            }
        }

        /// <summary>
        /// 注入引导路径——外部调用入口，宿主内部委托给GuidedModule。
        /// 外部调用者无需知道GuidedModule的存在。
        /// </summary>
        public bool TryInitGuidedFlight(
            List<IntVec3> anchors, LocalTargetInfo finalTarget, float anchorSpread)
        {
            var module = GetModule<GuidedModule>();
            if (module == null) return false;
            module.ApplyWaypoints(this, anchors, finalTarget, anchorSpread);
            return true;
        }

        /// <summary>
        /// 从 ShotSession 注入射击管线数据到弹道。
        /// 新的数据注入入口，用于射击管线集成。
        /// 替代 VerbFlightState.AttachManualFlight/AttachAutoRouteFlight。
        /// </summary>
        /// <param name="aimResult">瞄准结果（包含锚点路径、散布等）</param>
        /// <param name="fireResult">射击结果（包含速度倍率等）</param>
        /// <param name="routeResult">自动绕行路由结果（可选）</param>
        public void InjectShotData(
            BDP.Trigger.ShotPipeline.AimResult aimResult,
            BDP.Trigger.ShotPipeline.FireResult fireResult,
            ObstacleRouteResult? routeResult)
        {
            // 1. 注入手动锚点路径（优先级最高）
            if (aimResult?.HasGuidedPath == true)
            {
                TryInitGuidedFlight(
                    aimResult.AnchorPath,
                    aimResult.FinalTarget,
                    aimResult.AnchorSpread);
                return;
            }

            // 2. 注入自动绕行路径（无手动锚点时）
            if (routeResult.HasValue && routeResult.Value.IsValid)
            {
                var route = routeResult.Value;
                List<IntVec3> anchors;

                // 两侧都可绕行时交替分配（简化版，不维护计数器）
                if (route.LeftAnchors != null && route.RightAnchors != null)
                {
                    // 使用弹道ID的奇偶性决定左右侧
                    anchors = (thingIDNumber % 2 == 0)
                        ? route.LeftAnchors
                        : route.RightAnchors;
                }
                else
                {
                    anchors = route.LeftAnchors ?? route.RightAnchors;
                }

                if (anchors != null && anchors.Count > 0)
                {
                    TryInitGuidedFlight(
                        anchors,
                        aimResult?.FinalTarget ?? default,
                        fireResult?.SpreadRadius ?? 0f);
                }
            }

            // 3. 无引导路径时，弹道保持直射（无需额外操作）
        }

        /// <summary>
        /// 初始化引导飞行——由GuidedModule.ApplyWaypoints调用。
        /// 设置三层目标，CurrentTarget由GuidedModule首tick ProvideIntent设置。
        /// </summary>
        public void InitGuidedFlight(LocalTargetInfo finalTarget)
        {
            AimTarget = finalTarget;
            LockedTarget = finalTarget;
            // CurrentTarget由GuidedModule首tick ProvideIntent设置为首锚点
        }

        /// <summary>
        /// 统一Phase转换——所有Phase变更必须通过此方法。
        /// 通知所有IBDPPhaseTransitionObserver。
        /// </summary>
        private void SetPhase(FlightPhase newPhase)
        {
            if (Phase == newPhase) return;
            var old = Phase;
            Phase = newPhase;
            if (TrackingDiag.Enabled)
                Log.Message($"[BDP-Phase] {old}→{Phase}");
            if (phaseObservers != null)
            {
                for (int i = 0; i < phaseObservers.Count; i++)
                    phaseObservers[i].OnPhaseChanged(this, old, newPhase);
            }
        }

        /// <summary>
        /// Phase自动推导——根据三层目标和Intent状态推导Phase。
        /// 每tick末尾、ApplyFlightRedirect后调用。
        /// </summary>
        private void DeriveAndSetPhase(bool hadIntent, bool trackingActivated)
        {
            FlightPhase derived;
            if (hadIntent && trackingActivated)
                derived = FlightPhase.Tracking;
            else if (CurrentTarget.Thing != null && CurrentTarget.Thing == LockedTarget.Thing)
                derived = FlightPhase.Direct;  // C==B，直飞或追踪前
            else if (CurrentTarget.Cell.IsValid && LockedTarget.Cell.IsValid && CurrentTarget.Cell == LockedTarget.Cell && CurrentTarget.Thing == null && LockedTarget.Thing == null)
                derived = FlightPhase.Direct;  // 两者都指向同一Cell且无Thing
            else if (hadIntent)
                derived = FlightPhase.Guided;  // C≠B，有引导Intent
            else
                derived = FlightPhase.Free;

            if (Phase != derived)
            {
                var old = Phase;
                Phase = derived;
                if (TrackingDiag.Enabled)
                    Log.Message($"[BDP-Phase-Auto] {old}→{derived} hadIntent={hadIntent} trackingActivated={trackingActivated}");
                if (phaseObservers != null)
                {
                    for (int i = 0; i < phaseObservers.Count; i++)
                        phaseObservers[i].OnPhaseChanged(this, old, derived);
                }
            }
        }

        // ══════════════════════════════════════════
        //  飞行重定向（唯一写入origin/destination/ticksToImpact的方法）
        // ══════════════════════════════════════════

        /// <summary>
        /// 统一飞行参数重算——所有飞行方向变更必须通过此方法。
        /// 红线约束：origin/destination/ticksToImpact只在此方法中写入。
        ///
        /// 策略：
        ///   GuidedLeg/Tracking/FinalApproach → origin后退config.originOffset（恢复vanilla沿途拦截）
        ///   Tracking远距离 → 固定tick，精确速度
        ///   其他 → 精确距离计算
        /// </summary>
        private void ApplyFlightRedirect(Vector3 newDestination, bool exactPosition = false)
        {
            // 首段重定向只执行一次：用于避免首发视觉起点漂移。
            bool isFirstRedirect = !firstRedirectConsumed;
            firstRedirectConsumed = true;

            var cfg = redirectConfig;
            Vector3 currentPos = DrawPos;
            Vector3 toDest = (newDestination - currentPos).Yto0();
            float dist = toDest.magnitude;

            if (dist < 0.001f)
            {
                // 退化：目标几乎重合，直接设置最小tick
                destination = newDestination;
                ticksToImpact = 1;
                return;
            }

            Vector3 dir = toDest.normalized;
            float speedPerTick = EffectiveSpeedTilesPerTick;
            if (speedPerTick <= 0.0001f)
                speedPerTick = def.projectile.SpeedTilesPerTick;

            // ★ 使用适配层统一计算origin（替换原有的分散origin后退逻辑）
            origin = vanillaAdapter.ComputeAdaptedOrigin(currentPos, dir, Phase, isFirstRedirect);
            origin.y = currentPos.y;

            // 距离策略（GuidedLeg不走far-distance，始终精确飞到锚点）
            // exactPosition=true时跳过远距离策略（贝塞尔等精确位置模式）
            bool isFarDistance = !exactPosition
                && Phase == FlightPhase.Tracking
                && dist > speedPerTick * cfg.farDistanceSpeedMult;

            if (isFarDistance)
            {
                // 远距离：固定tick，精确速度
                int ticks = cfg.farDistanceFixedTicks;
                destination = currentPos + dir * (ticks * speedPerTick);
                destination.y = currentPos.y;
                ticksToImpact = ticks;
            }
            else
            {
                // 近距离：精确距离计算
                ticksToImpact = Mathf.CeilToInt(dist / speedPerTick);
                if (ticksToImpact < 1) ticksToImpact = 1;

                // 近距离snap：用整tick距离避免CeilToInt余数累积
                float finalApproachThreshold = speedPerTick * 1.5f;
                float resolvedDist = dist <= finalApproachThreshold
                    ? dist
                    : ticksToImpact * speedPerTick;

                destination = currentPos + dir * resolvedDist;
                destination.y = currentPos.y;
            }
        }

        // ══════════════════════════════════════════
        //  生命周期
        // ══════════════════════════════════════════

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            // 首次生成时通过工厂创建模块（读档时模块由ExposeData恢复）
            if (!respawningAfterLoad)
            {
                modules = BDPModuleFactory.CreateModules(def);
                modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }

            // 初始化三层目标（引导弹会在ApplyWaypoints时覆盖）
            if (!respawningAfterLoad)
            {
                AimTarget = intendedTarget;
                LockedTarget = intendedTarget;
                CurrentTarget = intendedTarget;
            }

            // 初始化发射时间戳
            if (!respawningAfterLoad)
                LaunchTick = Find.TickManager.TicksGame;

            // 初始化穿体穿透力
            if (!respawningAfterLoad)
            {
                var chipConfig = equipmentDef?.GetModExtension<VerbChipConfig>();
                PassthroughPower = chipConfig?.ranged?.passthroughPower ?? 0f;
            }

            // 读取飞行重定向配置（无配置时用默认值）
            redirectConfig = def.GetModExtension<FlightRedirectConfig>()
                ?? new FlightRedirectConfig();

            // 建立管线参与者缓存
            BuildPipelineCache();

            // 初始化显示位置（必须在OnSpawn之前，避免模块读到默认值）
            modifiedDrawPos = base.DrawPos;

            // 通知所有模块
            for (int i = 0; i < modules.Count; i++)
                modules[i].OnSpawn(this);

            // 配置适配层策略（根据弹道类型决定启用哪些适配）
            bool hasTracking = GetModule<TrackingModule>() != null;
            bool hasGuided = GetModule<GuidedModule>() != null;
            vanillaAdapter.ConfigureStrategy(
                needsOriginOffset: hasTracking || hasGuided,
                needsUsedTargetSync: hasTracking
            );

            // 记录真实发射点（在Launch后，origin已由vanilla设置）
            if (!respawningAfterLoad)
                vanillaAdapter.RecordTrueOrigin(origin);
        }

        /// <summary>扫描模块列表，按管线接口类型分组缓存。</summary>
        private void BuildPipelineCache()
        {
            lifecyclePolicies = new List<IBDPLifecyclePolicy>();
            flightIntentProviders = new List<IBDPFlightIntentProvider>();
            positionModifiers = new List<IBDPPositionModifier>();
            visualObservers = new List<IBDPVisualObserver>();
            arrivalPolicies = new List<IBDPArrivalPolicy>();
            hitResolvers = new List<IBDPHitResolver>();
            impactHandlers = new List<IBDPImpactHandler>();
            speedModifiers = new List<IBDPSpeedModifier>();
            phaseObservers = new List<IBDPPhaseTransitionObserver>();

            for (int i = 0; i < modules.Count; i++)
            {
                var m = modules[i];
                if (m is IBDPLifecyclePolicy lp) lifecyclePolicies.Add(lp);
                if (m is IBDPFlightIntentProvider fp) flightIntentProviders.Add(fp);
                if (m is IBDPPositionModifier pm) positionModifiers.Add(pm);
                if (m is IBDPVisualObserver vo) visualObservers.Add(vo);
                if (m is IBDPArrivalPolicy ap) arrivalPolicies.Add(ap);
                if (m is IBDPHitResolver hr) hitResolvers.Add(hr);
                if (m is IBDPImpactHandler ih) impactHandlers.Add(ih);
                if (m is IBDPSpeedModifier sm) speedModifiers.Add(sm);
                if (m is IBDPPhaseTransitionObserver po) phaseObservers.Add(po);
            }

            hasPositionModifiers = positionModifiers.Count > 0;
        }

        // ══════════════════════════════════════════
        //  管线调度：每tick（阶段0-5）
        // ══════════════════════════════════════════

        /// <summary>
        /// v5管线TickInterval——8阶段调度。
        ///   0. PostLaunchInit
        ///   1. LifecycleCheck
        ///   2. FlightIntent
        ///   3. base.TickInterval()
        ///   4. PositionModifier
        ///   5. VisualObserve
        /// </summary>
        protected override void TickInterval(int delta)
        {
            // 阶段0：Launch后延迟初始化
            if (!postLaunchInitDone)
            {
                postLaunchInitDone = true;
                bool isStale = AimTarget.Thing == null
                    && AimTarget.Cell.x == 0 && AimTarget.Cell.z == 0;
                if (isStale && intendedTarget.IsValid)
                {
                    AimTarget = intendedTarget;
                    LockedTarget = intendedTarget;
                    CurrentTarget = intendedTarget;
                }

                // 视觉模块初始化：在首次base.TickInterval（子弹移动）之前执行。
                // 此时DrawPos ≈ 发射原点，是Trail等视觉模块记录起始位置的正确时机。
                for (int i = 0; i < visualObservers.Count; i++)
                    visualObservers[i].OnVisualInit(this);
            }

            // 阶段1：LifecycleCheck——遍历lifecyclePolicies
            if (lifecyclePolicies.Count > 0)
            {
                var lcCtx = new LifecycleContext(lastTickHadIntent, AimTarget, LockedTarget, CurrentTarget);
                for (int i = 0; i < lifecyclePolicies.Count; i++)
                    lifecyclePolicies[i].CheckLifecycle(this, ref lcCtx);

                // 处理LockedTarget变更请求
                if (lcCtx.NewLockedTarget.HasValue)
                {
                    LockedTarget = lcCtx.NewLockedTarget.Value;
                }

                // 处理销毁请求
                if (lcCtx.RequestDestroy)
                {
                    if (TrackingDiag.Enabled)
                        Log.Message($"[BDP-Bullet] Destroy reason={lcCtx.DestroyReason} Phase={Phase} pos={Position}");
                    if (Spawned) Destroy();
                    return;
                }
            }

            // 阶段2：FlightIntent——取第一个非null Intent执行ApplyFlightRedirect
            bool hadIntent = false;
            bool trackingActivated = false;
            if (flightIntentProviders.Count > 0)
            {
                var fiCtx = new FlightIntentContext(DrawPos, destination, AimTarget, LockedTarget, CurrentTarget);
                for (int i = 0; i < flightIntentProviders.Count; i++)
                {
                    flightIntentProviders[i].ProvideIntent(this, ref fiCtx);
                    if (fiCtx.Intent.HasValue) break;
                }

                hadIntent = fiCtx.Intent.HasValue;
                if (hadIntent)
                {
                    var intent = fiCtx.Intent.Value;
                    trackingActivated = intent.TrackingActivated;
                    ApplyFlightRedirect(intent.TargetPosition, intent.ExactPosition);
                }

                // 处理CurrentTarget变更请求
                if (fiCtx.NewCurrentTarget.HasValue)
                    CurrentTarget = fiCtx.NewCurrentTarget.Value;
                if (fiCtx.NewLockedTarget.HasValue)
                    LockedTarget = fiCtx.NewLockedTarget.Value;

                // 记录本tick是否有Intent（供下一tick LifecycleContext使用）
                lastTickHadIntent = hadIntent;
            }

            // Phase自动推导
            DeriveAndSetPhase(hadIntent, trackingActivated);

            // 阶段3：vanilla引擎位置计算 + 拦截检查 + 到达判定
            base.TickInterval(delta);
            if (!Spawned)
                return;

            // 阶段4：PositionModifier——修饰显示位置
            if (hasPositionModifiers)
            {
                float progress = StartingTicksToImpact > 0f
                    ? 1f - (float)ticksToImpact / StartingTicksToImpact
                    : 1f;
                var posCtx = new PositionContext(base.DrawPos, progress);
                for (int i = 0; i < positionModifiers.Count; i++)
                    positionModifiers[i].ModifyPosition(this, ref posCtx);
                modifiedDrawPos = posCtx.DrawPosition;
            }
            else
            {
                modifiedDrawPos = base.DrawPos;
            }

            // 阶段5：VisualObserve——通知视觉观察者
            for (int i = 0; i < visualObservers.Count; i++)
                visualObservers[i].Observe(this);
        }

        // ══════════════════════════════════════════
        //  管线调度：到达 & 命中（阶段6-8）
        // ══════════════════════════════════════════

        /// <summary>
        /// v5到达决策——分发ArrivalPolicy + HitResolver管线。
        /// </summary>
        protected override void ImpactSomething()
        {
            // 阶段6：ArrivalPolicy——决定继续飞还是命中
            if (arrivalPolicies.Count > 0)
            {
                var arrCtx = new ArrivalContext(AimTarget, LockedTarget, CurrentTarget);
                for (int i = 0; i < arrivalPolicies.Count; i++)
                {
                    arrivalPolicies[i].DecideArrival(this, ref arrCtx);
                    if (arrCtx.Continue) break;
                }

                if (arrCtx.Continue)
                {
                    // 处理CurrentTarget变更请求
                    if (arrCtx.NewCurrentTarget.HasValue)
                    {
                        CurrentTarget = arrCtx.NewCurrentTarget.Value;
                    }

                    arrivalRedirectCount++;
                    if (TrackingDiag.Enabled)
                        Log.Message($"[BDP-Bullet] Redirect#{arrivalRedirectCount} Phase={Phase} nextDest={arrCtx.NextDestination:F2}");
                    // 安全阀：防止无限重定向
                    if (arrivalRedirectCount > 200)
                    {
                        Log.Warning($"[BDP-Bullet] 重定向超限销毁 def={def.defName} pos={Position} Phase={Phase}");
                        if (Spawned) Destroy();
                        return;
                    }

                    ApplyFlightRedirect(arrCtx.NextDestination);
                    return;
                }
            }

            // 阶段7：HitResolve——修正命中判定
            // ★ 优先使用适配层统一处理（usedTarget同步 + ForceGround检查）
            var impactCheck = vanillaAdapter.CheckBeforeImpact(this, LockedTarget, ref usedTarget, lastTickHadIntent);
            if (impactCheck.ForceGround)
            {
                if (TrackingDiag.Enabled)
                    Log.Message($"[VanillaAdapter] ForceGround: {impactCheck.Reason}");
                Impact(null);
                return;
            }

            // 其余HitResolver（非TrackingModule）仍可参与
            if (hitResolvers.Count > 0)
            {
                var hitCtx = new HitContext(usedTarget, LockedTarget);
                for (int i = 0; i < hitResolvers.Count; i++)
                    hitResolvers[i].ResolveHit(this, ref hitCtx);

                if (hitCtx.ForceGround)
                {
                    Impact(null);
                    return;
                }

                if (hitCtx.OverrideTarget.IsValid)
                    usedTarget = hitCtx.OverrideTarget;
            }

            // 阶段8：vanilla命中判定 → Impact
            base.ImpactSomething();
        }

        /// <summary>
        /// 命中效果——分发IBDPImpactHandler管线。
        /// </summary>
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (TrackingDiag.Enabled)
            {
                string hitName = hitThing?.def?.defName ?? "Ground";
                Log.Message($"[BDP-Impact] {def.defName}→{hitName} Phase={Phase} pos={Position} shield={blockedByShield}");
            }

            if (impactHandlers.Count > 0)
            {
                var impCtx = new ImpactContext(hitThing, blockedByShield);
                for (int i = 0; i < impactHandlers.Count; i++)
                {
                    impactHandlers[i].HandleImpact(this, ref impCtx);
                    if (impCtx.Handled) break;
                }
                if (impCtx.Handled) return;
            }

            base.Impact(hitThing, blockedByShield);
        }

        // ══════════════════════════════════════════
        //  显示位置（PositionModifier支持）
        // ══════════════════════════════════════════

        public override Vector3 DrawPos => hasPositionModifiers ? modifiedDrawPos : base.DrawPos;

        /// <summary>
        /// 当前飞行方向（X-Z平面单位向量，忽略Y轴高度差）。
        /// 供视觉模块（TrailModule等）计算枪口偏移方向，不用于弹道计算。
        /// </summary>
        public Vector3 FlightDirection => (destination - origin).Yto0().normalized;

        // ── 速度修正接口（供Patch_Projectile_Launch_FireModeSpeed调用） ──

        /// <summary>
        /// 速度修正：修改destination使StartingTicksToImpact自然变化，
        /// 然后重新初始化ticksToImpact/lifetime。
        /// </summary>
        public void ReinitFlight(float speedMult)
        {
            launchSpeedMult = Mathf.Max(0.01f, speedMult);

            // 必须用 3D 距离（不 Yto0），与 StartingTicksToImpact（vanilla 计算同源）保持一致。
            // 若用 2D 距离，而 StartingTicksToImpact 用 3D，两者不一致会导致
            // DistanceCoveredFraction > 0，子弹视觉位置在发射瞬间就已前移约2格。
            float dist = (destination - origin).magnitude;
            if (dist < 0.001f)
            {
                ticksToImpact = 1;
                lifetime = 1;
                return;
            }

            float speedPerTick = EffectiveSpeedTilesPerTick;
            if (speedPerTick <= 0.0001f)
                speedPerTick = def.projectile.SpeedTilesPerTick;

            // 防止destination超出地图边界
            int newTicks = Mathf.Max(1, Mathf.CeilToInt(dist / speedPerTick));
            ticksToImpact = newTicks;
            lifetime = newTicks;
        }

        /// <summary>发射时速度管线分发入口。</summary>
        public void DispatchSpeedModifiers(float speedMult)
        {
            if (speedModifiers.Count > 0)
            {
                var ctx = new SpeedContext(speedMult);
                for (int i = 0; i < speedModifiers.Count; i++)
                    speedModifiers[i].ModifySpeed(this, ref ctx);
                speedMult = ctx.SpeedMult;
            }
            ReinitFlight(speedMult);
        }

        // ══════════════════════════════════════════
        //  序列化
        // ══════════════════════════════════════════

        public override void ExposeData()
        {
            base.ExposeData();

            // v5 Phase状态机
            var phase = Phase;
            Scribe_Values.Look(ref phase, "bdpPhase", FlightPhase.Direct);
            Phase = phase;

            // 三层目标模型
            var aim = AimTarget;
            Scribe_TargetInfo.Look(ref aim, "bdpAimTarget");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                AimTarget = aim;

            var locked = LockedTarget;
            Scribe_TargetInfo.Look(ref locked, "bdpLockedTarget");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                LockedTarget = locked;

            var current = CurrentTarget;
            Scribe_TargetInfo.Look(ref current, "bdpCurrentTarget");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                CurrentTarget = current;

            Scribe_Values.Look(ref PassthroughPower, "bdpPassthroughPower", 0f);
            Scribe_Values.Look(ref PassthroughCount, "bdpPassthroughCount", 0);
            Scribe_Values.Look(ref LaunchTick, "bdpLaunchTick", 0);
            Scribe_Values.Look(ref launchSpeedMult, "bdpLaunchSpeedMult", 1f);
            Scribe_Values.Look(ref postLaunchInitDone, "bdpPostLaunchInit", false);
            Scribe_Values.Look(ref arrivalRedirectCount, "bdpArrivalRedirects", 0);
            Scribe_Values.Look(ref firstRedirectConsumed, "bdpFirstRedirectConsumed", false);
            Scribe_Collections.Look(ref modules, "bdpModules", LookMode.Deep);
            if (modules == null)
                modules = new List<IBDPProjectileModule>();
        }
    }
}
