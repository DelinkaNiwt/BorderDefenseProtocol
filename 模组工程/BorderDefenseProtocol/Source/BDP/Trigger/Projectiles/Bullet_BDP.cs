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
    ///   · 模块按需实现管线接口（IBDPPathResolver/IBDPSpeedModifier/...）
    ///   · 每tick按管线顺序分发：PathResolver→SpeedModifier→引擎计算→InterceptModifier→PositionModifier→TickObserver
    ///   · 到达时：ArrivalHandler→ImpactHandler
    ///   · 无管线参与者的阶段零开销（空列表跳过）
    /// </summary>
    public class Bullet_BDP : Bullet
    {
        // ── 模块列表（按Priority升序） ──
        private List<IBDPProjectileModule> modules = new List<IBDPProjectileModule>();

        // ── 管线参与者缓存（SpawnSetup时建立，避免每tick做is检查） ──
        private List<IBDPPathResolver> pathResolvers;
        private List<IBDPSpeedModifier> speedModifiers;
        private List<IBDPInterceptModifier> interceptModifiers;
        private List<IBDPPositionModifier> positionModifiers;
        private List<IBDPTickObserver> tickObservers;
        private List<IBDPArrivalHandler> arrivalHandlers;
        private List<IBDPImpactHandler> impactHandlers;

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
            speedModifiers = new List<IBDPSpeedModifier>();
            interceptModifiers = new List<IBDPInterceptModifier>();
            positionModifiers = new List<IBDPPositionModifier>();
            tickObservers = new List<IBDPTickObserver>();
            arrivalHandlers = new List<IBDPArrivalHandler>();
            impactHandlers = new List<IBDPImpactHandler>();

            for (int i = 0; i < modules.Count; i++)
            {
                var m = modules[i];
                if (m is IBDPPathResolver pr) pathResolvers.Add(pr);
                if (m is IBDPSpeedModifier sm) speedModifiers.Add(sm);
                if (m is IBDPInterceptModifier im) interceptModifiers.Add(im);
                if (m is IBDPPositionModifier pm) positionModifiers.Add(pm);
                if (m is IBDPTickObserver to) tickObservers.Add(to);
                if (m is IBDPArrivalHandler ah) arrivalHandlers.Add(ah);
                if (m is IBDPImpactHandler ih) impactHandlers.Add(ih);
            }

            hasPositionModifiers = positionModifiers.Count > 0;
        }

        // ══════════════════════════════════════════
        //  管线分发：每tick
        // ══════════════════════════════════════════

        /// <summary>
        /// 管线化TickInterval——按阶段顺序分发：
        ///   1. PathResolver → 修改destination
        ///   2. SpeedModifier → 修改速度（暂预留，当前引擎不支持动态速度）
        ///   3. 引擎位置计算（base.TickInterval）
        ///   4. InterceptModifier → 修饰拦截判定（暂预留）
        ///   5. PositionModifier → 修饰显示位置
        ///   6. TickObserver → 通知观察者
        ///   7. 到达检查 → ArrivalHandler → ImpactHandler
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
                if (pathCtx.Modified)
                    destination = pathCtx.Destination;
            }

            // 阶段2：SpeedModifier（预留——当前引擎不支持动态速度修改）
            // 未来可在此处修改ticksToImpact来模拟加速/减速

            // 阶段3：引擎位置计算 + 拦截检查 + 到达判定
            base.TickInterval(delta);

            // 阶段4：InterceptModifier（预留——需要hook CheckForFreeInterceptBetween时启用）

            // 阶段5：PositionModifier——修饰显示位置
            if (hasPositionModifiers)
            {
                var posCtx = new PositionContext(base.DrawPos);
                for (int i = 0; i < positionModifiers.Count; i++)
                    positionModifiers[i].ModifyPosition(this, ref posCtx);
                modifiedDrawPos = posCtx.DrawPosition;
            }
            else
            {
                modifiedDrawPos = base.DrawPos;
            }

            // 阶段6：TickObserver——通知观察者（拖尾/视觉/音效）
            for (int i = 0; i < tickObservers.Count; i++)
                tickObservers[i].OnTick(this);
        }

        // ══════════════════════════════════════════
        //  管线分发：到达 & 命中
        // ══════════════════════════════════════════

        /// <summary>
        /// 到达决策——分发ArrivalHandler管线。
        /// 任一模块设置Continue=true则跳过Impact（如引导飞行重定向）。
        /// </summary>
        protected override void ImpactSomething()
        {
            if (arrivalHandlers.Count > 0)
            {
                var arrCtx = new ArrivalContext(null);
                for (int i = 0; i < arrivalHandlers.Count; i++)
                    arrivalHandlers[i].HandleArrival(this, ref arrCtx);
                if (arrCtx.Continue)
                    return; // 模块已重定向，不执行Impact
            }
            base.ImpactSomething();
        }

        /// <summary>
        /// 命中效果——分发ImpactHandler管线（依次执行，不再first-handler-wins）。
        /// 所有ImpactHandler都会被调用，任一设置Handled=true则跳过base.Impact。
        /// </summary>
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (impactHandlers.Count > 0)
            {
                var impCtx = new ImpactContext(hitThing);
                for (int i = 0; i < impactHandlers.Count; i++)
                    impactHandlers[i].HandleImpact(this, ref impCtx);

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

        // ══════════════════════════════════════════
        //  序列化
        // ══════════════════════════════════════════

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref modules, "bdpModules", LookMode.Deep);
            if (modules == null)
                modules = new List<IBDPProjectileModule>();
        }
    }
}
