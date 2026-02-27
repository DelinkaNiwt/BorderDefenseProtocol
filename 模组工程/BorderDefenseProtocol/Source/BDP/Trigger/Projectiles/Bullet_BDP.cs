using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// BDP统一投射物宿主——继承原版Bullet，通过模块组合实现拖尾/引导/爆炸等功能。
    ///
    /// 架构v4（管线化重构）：
    ///   · 宿主本身是薄壳，不含业务逻辑
    ///   · 功能由IBDPProjectileModule模块提供
    ///   · 模块按需实现管线接口（IBDPPathResolver/IBDPPositionModifier/IBDPTickObserver/...）
    ///   · 每tick按管线顺序分发：PathResolver→引擎计算→PositionModifier→TickObserver
    ///   · 到达时：ArrivalHandler→ImpactHandler
    ///   · 无管线参与者的阶段零开销（空列表跳过）
    /// </summary>
    public class Bullet_BDP : Bullet
    {
        // ── 模块列表（按Priority升序） ──
        private List<IBDPProjectileModule> modules = new List<IBDPProjectileModule>();

        // ── 管线参与者缓存（SpawnSetup时建立，避免每tick做is检查） ──
        private List<IBDPPathResolver> pathResolvers;
        private List<IBDPPositionModifier> positionModifiers;
        private List<IBDPTickObserver> tickObservers;
        private List<IBDPArrivalHandler> arrivalHandlers;
        private List<IBDPImpactHandler> impactHandlers;
        private List<IBDPSpeedModifier> speedModifiers;

        // ── 弹道目标信息 ──
        /// <summary>
        /// 弹道的最终目标。
        /// 普通弹道等同于 intendedTarget；引导弹由 GuidedModule.SetWaypoints() 写入真实目标。
        /// </summary>
        public LocalTargetInfo FinalTarget;

        /// <summary>
        /// 当前是否处于最终飞行段（飞向真实目标）。
        /// 普通弹道始终为 true；引导弹由 GuidedModule 在路径推进时维护。
        /// </summary>
        public bool IsOnFinalSegment = true;

        // ── 追踪状态（由 IBDPPathResolver 模块维护） ──
        /// <summary>是否正在被追踪模块引导（追踪弹激活时写true，丢失目标时写false）。</summary>
        public bool IsTracking;

        /// <summary>追踪目标（可能与FinalTarget不同——追踪弹可中途切换目标）。</summary>
        public LocalTargetInfo TrackingTarget;

        // ── 穿体穿透（由 IBDPImpactHandler 模块维护） ──
        /// <summary>
        /// 穿体穿透剩余力——子弹穿过目标继续飞行的能力。
        /// 区别于原版ArmorPenetration（护甲穿透）。
        /// 初始值由SpawnSetup从芯片配置读取，每次穿透后由ImpactHandler递减。
        /// 默认0=不穿透。
        /// </summary>
        public float PassthroughPower;

        /// <summary>已穿透实体次数（供伤害衰减计算）。</summary>
        public int PassthroughCount;

        // ── 发射上下文 ──
        /// <summary>发射时的游戏tick（用于延迟引爆、存活时限等时间相关效果）。</summary>
        public int LaunchTick;

        // ── PositionModifier输出缓存（供DrawPos/DrawAt使用） ──
        /// <summary>经PositionModifier修饰后的显示位置。每tick更新。</summary>
        private Vector3 modifiedDrawPos;

        /// <summary>是否有PositionModifier参与者（决定是否使用修饰位置）。</summary>
        private bool hasPositionModifiers;

        /// <summary>获取指定类型的模块实例（供Verb层调用）。</summary>
        public T GetModule<T>() where T : class, IBDPProjectileModule
        {
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i] is T typed) return typed;
            }
            return null;
        }

        /// <summary>
        /// 获取实现指定管线接口的模块（供外部查询能力）。
        /// 预留的Provider查询入口。
        /// </summary>
        public T GetCapability<T>() where T : class
        {
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i] is T typed) return typed;
            }
            return null;
        }

        /// <summary>
        /// 重定向飞行——由模块调用，将弹道从当前位置重定向到新目标。
        /// 重置origin/destination/ticksToImpact。
        /// </summary>
        public void RedirectFlight(Vector3 newOrigin, Vector3 newDestination)
        {
            origin = newOrigin;
            destination = newDestination;
            ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
            if (ticksToImpact < 1) ticksToImpact = 1;
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

            // 初始化最终目标（引导弹会在 SetWaypoints 时覆盖）
            FinalTarget = intendedTarget;

            // 初始化发射时间戳
            if (!respawningAfterLoad)
                LaunchTick = Find.TickManager.TicksGame;

            // 初始化穿体穿透力（从芯片配置读取，无配置则默认0=不穿透）
            if (!respawningAfterLoad)
            {
                var chipConfig = equipmentDef?.GetModExtension<WeaponChipConfig>();
                PassthroughPower = chipConfig?.passthroughPower ?? 0f;
            }

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
            pathResolvers = new List<IBDPPathResolver>();
            positionModifiers = new List<IBDPPositionModifier>();
            tickObservers = new List<IBDPTickObserver>();
            arrivalHandlers = new List<IBDPArrivalHandler>();
            impactHandlers = new List<IBDPImpactHandler>();
            speedModifiers = new List<IBDPSpeedModifier>();

            for (int i = 0; i < modules.Count; i++)
            {
                var m = modules[i];
                if (m is IBDPPathResolver pr) pathResolvers.Add(pr);
                if (m is IBDPPositionModifier pm) positionModifiers.Add(pm);
                if (m is IBDPTickObserver to) tickObservers.Add(to);
                if (m is IBDPArrivalHandler ah) arrivalHandlers.Add(ah);
                if (m is IBDPImpactHandler ih) impactHandlers.Add(ih);
                if (m is IBDPSpeedModifier sm) speedModifiers.Add(sm);
            }

            hasPositionModifiers = positionModifiers.Count > 0;
        }

        // ══════════════════════════════════════════
        //  管线分发：每tick
        // ══════════════════════════════════════════

        /// <summary>
        /// 管线化TickInterval——按阶段顺序分发：
        ///   1. PathResolver → 修改destination
        ///   2. 引擎位置计算（base.TickInterval）
        ///   3. PositionModifier → 修饰显示位置
        ///   4. TickObserver → 通知观察者
        ///   5. 到达检查 → ArrivalHandler → ImpactHandler
        ///
        /// 注意：base.TickInterval内部已包含拦截检查和到达判定。
        /// 当前PathResolver在base之前执行（修改destination），
        /// PositionModifier和TickObserver在base之后执行。
        /// ArrivalHandler通过ImpactSomething override分发。
        /// </summary>
        protected override void TickInterval(int delta)
        {
            // 阶段1：PathResolver——修改destination
            if (pathResolvers.Count > 0)
            {
                var pathCtx = new PathContext(origin, destination);
                for (int i = 0; i < pathResolvers.Count; i++)
                    pathResolvers[i].ResolvePath(this, ref pathCtx);
                destination = pathCtx.Destination;
            }

            // 阶段2：引擎位置计算 + 拦截检查 + 到达判定
            base.TickInterval(delta);

            // 阶段3：PositionModifier——修饰显示位置
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

            // 阶段4：TickObserver——通知观察者（拖尾/视觉/音效）
            for (int i = 0; i < tickObservers.Count; i++)
                tickObservers[i].OnTick(this);
        }

        // ══════════════════════════════════════════
        //  管线分发：到达 & 命中
        // ══════════════════════════════════════════

        /// <summary>
        /// 到达决策——分发ArrivalHandler管线。
        /// 任一模块设置Continue=true则短路跳过后续Handler和Impact。
        /// </summary>
        protected override void ImpactSomething()
        {
            if (arrivalHandlers.Count > 0)
            {
                var arrCtx = new ArrivalContext();
                for (int i = 0; i < arrivalHandlers.Count; i++)
                {
                    arrivalHandlers[i].HandleArrival(this, ref arrCtx);
                    if (arrCtx.Continue) break; // 短路：已重定向，不再分发后续Handler
                }
                if (arrCtx.Continue)
                    return; // 模块已重定向，不执行Impact
            }

            base.ImpactSomething();
        }

        /// <summary>
        /// 命中效果——分发ImpactHandler管线。
        /// 任一模块设置Handled=true则短路跳过后续Handler和base.Impact。
        /// </summary>
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (impactHandlers.Count > 0)
            {
                var impCtx = new ImpactContext(hitThing, blockedByShield);
                for (int i = 0; i < impactHandlers.Count; i++)
                {
                    impactHandlers[i].HandleImpact(this, ref impCtx);
                    if (impCtx.Handled) break; // 短路：已处理Impact，不再分发后续Handler
                }

                if (impCtx.Handled)
                    return; // 模块已处理Impact
            }

            // 无模块处理时走原版Impact
            base.Impact(hitThing, blockedByShield);
        }

        // ══════════════════════════════════════════
        //  显示位置（PositionModifier支持）
        // ══════════════════════════════════════════

        /// <summary>
        /// 重写DrawPos——返回经PositionModifier修饰后的显示位置。
        /// 无PositionModifier时等同base.DrawPos。
        /// </summary>
        public override Vector3 DrawPos => hasPositionModifiers ? modifiedDrawPos : base.DrawPos;

        // ── v9.1 FireMode速度修正接口（供 Patch_Projectile_Launch_FireModeSpeed 调用） ──

        /// <summary>
        /// 速度修正：修改 destination 使 StartingTicksToImpact 自然变化，
        /// 然后重新初始化 ticksToImpact / lifetime。
        /// 与 RedirectFlight 模式一致，在 Launch 的 Postfix 中调用。
        /// speedMult > 1 = 加速（destination 缩近），speedMult &lt; 1 = 减速（destination 拉远）。
        /// </summary>
        public void ReinitFlight(float speedMult)
        {
            Vector3 dir  = (destination - origin).Yto0();
            float   dist = dir.magnitude;
            if (dist < 0.001f) return;

            float newDist = dist / speedMult;

            // P0：防止 Speed 极小值时 destination 超出地图边界（越界会被引擎 Destroy()，零命中）
            // 取地图短边的 80% 作为安全上限，保留足够余量
            if (Map != null)
            {
                float mapLimit = Mathf.Min(Map.Size.x, Map.Size.z) * 0.8f;
                newDist = Mathf.Min(newDist, mapLimit);
            }

            // 速度快 → 等效距离短 → StartingTicksToImpact 自然减小 → 更快到达
            destination = origin + dir.normalized * newDist
                          + Vector3.up * destination.y;
            int newTicks  = Mathf.Max(1, Mathf.CeilToInt(StartingTicksToImpact));
            ticksToImpact = newTicks;
            lifetime      = newTicks;
        }

        /// <summary>
        /// 发射时速度管线分发入口（由 Patch_Projectile_Launch_FireModeSpeed 调用）。
        /// 依次调用所有 IBDPSpeedModifier 模块，最终以修改后的倍率执行 ReinitFlight。
        /// 无模块时行为与直接调用 ReinitFlight 完全一致。
        /// </summary>
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
            Scribe_TargetInfo.Look(ref FinalTarget, "bdpFinalTarget");
            Scribe_Values.Look(ref IsOnFinalSegment, "bdpIsOnFinalSegment", true);
            Scribe_Values.Look(ref IsTracking, "bdpIsTracking", false);
            Scribe_TargetInfo.Look(ref TrackingTarget, "bdpTrackingTarget");
            Scribe_Values.Look(ref PassthroughPower, "bdpPassthroughPower", 0f);
            Scribe_Values.Look(ref PassthroughCount, "bdpPassthroughCount", 0);
            Scribe_Values.Look(ref LaunchTick, "bdpLaunchTick", 0);
            Scribe_Collections.Look(ref modules, "bdpModules", LookMode.Deep);
            if (modules == null)
                modules = new List<IBDPProjectileModule>();
        }
    }
}
