using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
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

        // ── 弹道目标信息 ──
        /// <summary>
        /// 弹道的最终目标。
        /// 普通弹道等同于intendedTarget；引导弹由GuidedModule.ApplyWaypoints()写入真实目标。
        /// </summary>
        public LocalTargetInfo FinalTarget;

        /// <summary>追踪目标（可能与FinalTarget不同——追踪弹可中途切换目标）。</summary>
        public LocalTargetInfo TrackingTarget;

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
        /// 初始化引导飞行——由GuidedModule.ApplyWaypoints调用。
        /// 设置Phase=GuidedLeg，同步FinalTarget/TrackingTarget。
        /// </summary>
        public void InitGuidedFlight(LocalTargetInfo finalTarget)
        {
            SetPhase(FlightPhase.GuidedLeg);
            FinalTarget = finalTarget;
            TrackingTarget = finalTarget;
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

            // Phase为GuidedLeg/Tracking/FinalApproach时origin后退，恢复vanilla沿途拦截
            bool needOriginOffset = Phase == FlightPhase.GuidedLeg
                || Phase == FlightPhase.Tracking
                || Phase == FlightPhase.FinalApproach;

            if (needOriginOffset)
            {
                origin = currentPos - dir * cfg.originOffset;
                origin.y = currentPos.y;
            }
            else
            {
                origin = currentPos;
            }

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

            // 初始化最终目标（引导弹会在ApplyWaypoints时覆盖）
            FinalTarget = intendedTarget;

            // 初始化发射时间戳
            if (!respawningAfterLoad)
                LaunchTick = Find.TickManager.TicksGame;

            // 初始化穿体穿透力
            if (!respawningAfterLoad)
            {
                var chipConfig = equipmentDef?.GetModExtension<WeaponChipConfig>();
                PassthroughPower = chipConfig?.passthroughPower ?? 0f;
            }

            // 读取飞行重定向配置（无配置时用默认值）
            redirectConfig = def.GetModExtension<FlightRedirectConfig>()
                ?? new FlightRedirectConfig();

            // 建立管线参与者缓存
            BuildPipelineCache();

            // 通知所有模块
            for (int i = 0; i < modules.Count; i++)
                modules[i].OnSpawn(this);

            // 初始化显示位置
            modifiedDrawPos = base.DrawPos;
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
                bool isStale = FinalTarget.Thing == null
                    && FinalTarget.Cell.x == 0 && FinalTarget.Cell.z == 0;
                if (isStale && intendedTarget.IsValid)
                {
                    FinalTarget = intendedTarget;
                    TrackingTarget = intendedTarget;
                }
            }

            // 阶段1：LifecycleCheck——遍历lifecyclePolicies
            if (lifecyclePolicies.Count > 0)
            {
                var lcCtx = new LifecycleContext(Phase, lastTickHadIntent);
                for (int i = 0; i < lifecyclePolicies.Count; i++)
                    lifecyclePolicies[i].CheckLifecycle(this, ref lcCtx);

                // 处理Phase转换请求
                if (lcCtx.RequestPhaseChange.HasValue)
                {
                    SetPhase(lcCtx.RequestPhaseChange.Value);
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
            if (flightIntentProviders.Count > 0)
            {
                var fiCtx = new FlightIntentContext(DrawPos, destination, Phase);
                for (int i = 0; i < flightIntentProviders.Count; i++)
                {
                    flightIntentProviders[i].ProvideIntent(this, ref fiCtx);
                    if (fiCtx.Intent.HasValue) break;
                }

                if (fiCtx.Intent.HasValue)
                {
                    var intent = fiCtx.Intent.Value;

                    // 优先处理显式Phase请求
                    if (fiCtx.RequestPhaseChange.HasValue)
                    {
                        SetPhase(fiCtx.RequestPhaseChange.Value);
                    }
                    // 追踪激活时的Phase转换（兼容旧路径）
                    else if (intent.TrackingActivated && Phase != FlightPhase.Tracking)
                    {
                        SetPhase(FlightPhase.Tracking);
                    }

                    ApplyFlightRedirect(intent.TargetPosition, intent.ExactPosition);
                }

                // 记录本tick是否有Intent（供下一tick LifecycleContext使用）
                lastTickHadIntent = fiCtx.Intent.HasValue;
            }

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
                var arrCtx = new ArrivalContextV5(Phase);
                for (int i = 0; i < arrivalPolicies.Count; i++)
                {
                    arrivalPolicies[i].DecideArrival(this, ref arrCtx);
                    if (arrCtx.Continue) break;
                }

                if (arrCtx.Continue)
                {
                    // 处理Phase转换
                    if (arrCtx.RequestPhaseChange.HasValue)
                    {
                        SetPhase(arrCtx.RequestPhaseChange.Value);
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
            if (hitResolvers.Count > 0)
            {
                var hitCtx = new HitContext(Phase, usedTarget);
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

        // ── 速度修正接口（供Patch_Projectile_Launch_FireModeSpeed调用） ──

        /// <summary>
        /// 速度修正：修改destination使StartingTicksToImpact自然变化，
        /// 然后重新初始化ticksToImpact/lifetime。
        /// </summary>
        public void ReinitFlight(float speedMult)
        {
            launchSpeedMult = Mathf.Max(0.01f, speedMult);

            Vector3 dir = (destination - origin).Yto0();
            float dist = dir.magnitude;
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

            Scribe_TargetInfo.Look(ref FinalTarget, "bdpFinalTarget");
            Scribe_TargetInfo.Look(ref TrackingTarget, "bdpTrackingTarget");
            Scribe_Values.Look(ref PassthroughPower, "bdpPassthroughPower", 0f);
            Scribe_Values.Look(ref PassthroughCount, "bdpPassthroughCount", 0);
            Scribe_Values.Look(ref LaunchTick, "bdpLaunchTick", 0);
            Scribe_Values.Look(ref launchSpeedMult, "bdpLaunchSpeedMult", 1f);
            Scribe_Values.Look(ref postLaunchInitDone, "bdpPostLaunchInit", false);
            Scribe_Values.Look(ref arrivalRedirectCount, "bdpArrivalRedirects", 0);
            Scribe_Collections.Look(ref modules, "bdpModules", LookMode.Deep);
            if (modules == null)
                modules = new List<IBDPProjectileModule>();
        }
    }
}
