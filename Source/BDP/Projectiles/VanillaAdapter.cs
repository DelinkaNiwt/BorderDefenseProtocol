using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// Vanilla适配层——集中处理vanilla机制与BDP弹道系统的冲突。
    ///
    /// 设计目标：
    ///   让模块只负责纯粹的弹道逻辑，所有与vanilla的"斗争"都交给适配层。
    ///
    /// 核心职责：
    ///   1. origin偏移策略：根据Phase决定是否后退origin，恢复vanilla拦截
    ///   2. usedTarget同步：在命中前同步usedTarget到实际追踪目标
    ///   3. 拦截距离修正：提供"真实发射点"用于拦截计算
    ///   4. 命中前检查：集中处理ForceGround等特殊情况
    ///
    /// 设计原则：
    ///   · 单一职责：只负责vanilla兼容，不做弹道计算
    ///   · 透明性：模块不需要知道适配层的存在
    ///   · 集中管理：所有hack都在一个地方，便于维护
    ///   · 语义保护：尽量不破坏vanilla字段的原始语义
    ///
    /// 三大根本冲突：
    ///   冲突1：vanilla拦截机制依赖origin距离
    ///     vanilla: InterceptChanceFactorFromDistance(origin, 墙壁)
    ///     问题: 每tick重定向导致origin贴近子弹，拦截概率永远为0
    ///     方案: 后退origin恢复拦截，但保留trueOrigin用于其他计算
    ///
    ///   冲突2：vanilla命中机制usedTarget无视距离
    ///     vanilla: if (usedTarget.HasThing && CanHit(usedTarget.Thing)) Impact(usedTarget.Thing)
    ///     问题: 追踪弹飞过目标后，usedTarget未同步，产生"幽灵命中"
    ///     方案: 在ImpactSomething前同步usedTarget到TrackingTarget
    ///
    ///   冲突3：vanilla生命周期SpawnSetup时目标未就绪
    ///     vanilla: Launch()在SpawnSetup()前调用，destination已设置
    ///     问题: 引导弹的真实目标在ApplyWaypoints()时才确定
    ///     方案: 记录trueOrigin，允许后续重定向
    ///
    /// 版本：v1.0
    /// 作者：Claude Sonnet 4.6
    /// 日期：2026-03-03
    /// </summary>
    public class VanillaAdapter
    {
        // ══════════════════════════════════════════
        //  核心数据
        // ══════════════════════════════════════════

        /// <summary>
        /// 真实发射点——Launch时记录，永不改变。
        /// 用于拦截距离计算，避免origin后退导致的语义混乱。
        /// </summary>
        private Vector3 trueOrigin;

        /// <summary>
        /// 是否已记录真实发射点。
        /// </summary>
        private bool trueOriginRecorded;

        /// <summary>
        /// 上一次同步的追踪目标。
        /// 用于检测目标变化，避免重复同步。
        /// </summary>
        private LocalTargetInfo lastSyncedTarget;

        // ══════════════════════════════════════════
        //  配置参数
        // ══════════════════════════════════════════

        /// <summary>
        /// origin后退距离（格）。
        /// 用于恢复vanilla沿途拦截，默认6格。
        ///
        /// 原理：vanilla的InterceptChanceFactorFromDistance：
        ///   · origin到墙壁 < 5格 → 拦截概率0%
        ///   · origin到墙壁 > 12格 → 拦截概率100%
        ///   · 5-12格之间 → 线性插值
        /// 后退6格可以让大部分墙壁进入有效拦截范围。
        /// </summary>
        private float originOffsetDistance = 6f;

        /// <summary>
        /// 是否启用origin偏移。
        /// 某些Phase（如Direct）不需要偏移。
        /// </summary>
        private bool enableOriginOffset = true;

        /// <summary>
        /// 是否启用usedTarget自动同步。
        /// 追踪弹需要，普通弹不需要。
        /// </summary>
        private bool enableUsedTargetSync = true;

        // ══════════════════════════════════════════
        //  初始化方法
        // ══════════════════════════════════════════

        /// <summary>
        /// 记录真实发射点——在Launch后立即调用（通常在SpawnSetup中）。
        /// </summary>
        public void RecordTrueOrigin(Vector3 origin)
        {
            if (!trueOriginRecorded)
            {
                trueOrigin = origin;
                trueOriginRecorded = true;
            }
        }

        /// <summary>
        /// 配置适配策略——根据弹道类型决定启用哪些适配。
        ///
        /// 参数：
        ///   needsOriginOffset: 是否需要origin偏移（追踪弹/引导弹需要）
        ///   needsUsedTargetSync: 是否需要usedTarget同步（追踪弹需要）
        /// </summary>
        public void ConfigureStrategy(bool needsOriginOffset, bool needsUsedTargetSync)
        {
            enableOriginOffset = needsOriginOffset;
            enableUsedTargetSync = needsUsedTargetSync;
        }

        /// <summary>
        /// 设置origin偏移距离——允许外部自定义偏移量。
        /// </summary>
        public void SetOriginOffsetDistance(float distance)
        {
            originOffsetDistance = Mathf.Max(0f, distance);
        }

        // ══════════════════════════════════════════
        //  核心适配方法
        // ══════════════════════════════════════════

        /// <summary>
        /// 计算适配后的origin——根据Phase和策略决定是否后退。
        ///
        /// 策略：
        ///   · Direct/Free Phase → 使用当前位置（不后退）
        ///   · GuidedLeg/Tracking/FinalApproach → 后退originOffsetDistance
        ///   · 首次重定向 → 不后退（避免视觉起点漂移）
        ///
        /// 原理：
        ///   vanilla拦截机制依赖origin距离，后退origin可以恢复正常拦截。
        ///   但要避免破坏origin的语义，所以保留trueOrigin用于其他计算。
        /// </summary>
        public Vector3 ComputeAdaptedOrigin(
            Vector3 currentPos,
            Vector3 direction,
            FlightPhase phase,
            bool isFirstRedirect)
        {
            // 不启用偏移 → 使用当前位置
            if (!enableOriginOffset)
                return currentPos;

            // 首次重定向 → 使用当前位置（避免视觉起点漂移）
            if (isFirstRedirect)
                return currentPos;

            // 需要偏移的Phase
            bool needsOffset = phase == FlightPhase.GuidedLeg
                || phase == FlightPhase.Tracking
                || phase == FlightPhase.FinalApproach;

            if (!needsOffset)
                return currentPos;

            // 后退origin
            return currentPos - direction * originOffsetDistance;
        }

        /// <summary>
        /// 同步usedTarget到追踪目标——在ImpactSomething前调用。
        ///
        /// 策略：
        ///   · 仅在Tracking/FinalApproach Phase同步
        ///   · 仅在目标有效时同步
        ///   · 避免重复同步（检查lastSyncedTarget）
        ///
        /// 原理：
        ///   vanilla的ImpactSomething优先命中usedTarget，完全不检查距离。
        ///   追踪弹中途切换目标或飞过目标后，必须同步usedTarget，
        ///   否则会产生"幽灵命中"（隔空命中最初目标）。
        ///
        /// 返回：
        ///   true=已同步，false=未同步
        /// </summary>
        public bool TrySyncUsedTarget(
            FlightPhase phase,
            LocalTargetInfo trackingTarget,
            ref LocalTargetInfo usedTarget)
        {
            if (!enableUsedTargetSync)
                return false;

            // 仅在追踪相关Phase同步
            bool isTrackingPhase = phase == FlightPhase.Tracking
                || phase == FlightPhase.FinalApproach;

            if (!isTrackingPhase)
                return false;

            // 目标无效 → 不同步
            if (!IsTargetValid(trackingTarget))
                return false;

            // 目标未变化 → 不同步
            if (trackingTarget == lastSyncedTarget)
                return false;

            // 执行同步
            usedTarget = trackingTarget;
            lastSyncedTarget = trackingTarget;

            if (TrackingDiag.Enabled)
                Log.Message($"[VanillaAdapter] SyncUsedTarget: {trackingTarget}");

            return true;
        }

        /// <summary>
        /// 命中前检查——在ImpactSomething开始时调用。
        ///
        /// 职责：
        ///   1. 同步usedTarget（如果需要）
        ///   2. 检查是否需要强制打地面（TrackingLost/Free Phase）
        ///
        /// 返回：
        ///   ImpactCheckResult结构体，包含ForceGround标志和原因
        /// </summary>
        public ImpactCheckResult CheckBeforeImpact(
            Bullet_BDP host,
            FlightPhase phase,
            LocalTargetInfo trackingTarget,
            ref LocalTargetInfo usedTarget)
        {
            // 1. 同步usedTarget
            TrySyncUsedTarget(phase, trackingTarget, ref usedTarget);

            // 2. 检查是否强制打地面
            bool shouldForceGround = phase == FlightPhase.TrackingLost
                || phase == FlightPhase.Free;

            if (shouldForceGround)
            {
                return new ImpactCheckResult
                {
                    ForceGround = true,
                    Reason = $"Phase={phase}，追踪失效，打地面"
                };
            }

            return new ImpactCheckResult { ForceGround = false };
        }

        // ══════════════════════════════════════════
        //  查询方法
        // ══════════════════════════════════════════

        /// <summary>
        /// 获取用于拦截计算的origin——返回trueOrigin而非当前origin。
        ///
        /// 用途：
        ///   如果未来需要自定义拦截逻辑（Harmony Patch CheckForFreeIntercept），
        ///   可以使用trueOrigin计算正确的拦截距离，而不受origin后退影响。
        ///
        /// 当前：
        ///   vanilla的CheckForFreeIntercept直接读取host.origin，
        ///   我们通过后退origin来hack拦截距离，所以暂时不需要此方法。
        ///   但保留接口，便于未来扩展。
        /// </summary>
        public Vector3 GetInterceptOrigin()
        {
            return trueOriginRecorded ? trueOrigin : Vector3.zero;
        }

        /// <summary>
        /// 是否已记录真实发射点。
        /// </summary>
        public bool HasTrueOrigin => trueOriginRecorded;

        // ══════════════════════════════════════════
        //  辅助方法
        // ══════════════════════════════════════════

        /// <summary>
        /// 目标是否有效（活着、在地图上）。
        /// </summary>
        private static bool IsTargetValid(LocalTargetInfo target)
        {
            if (!target.IsValid) return false;
            if (target.Thing == null) return false;
            if (target.Thing.Destroyed) return false;
            if (!target.Thing.Spawned) return false;
            if (target.Thing is Pawn p && (p.Dead || p.Downed)) return false;
            return true;
        }

        // ══════════════════════════════════════════
        //  序列化
        // ══════════════════════════════════════════

        public void ExposeData()
        {
            Scribe_Values.Look(ref trueOrigin, "vanillaAdapterTrueOrigin");
            Scribe_Values.Look(ref trueOriginRecorded, "vanillaAdapterTrueOriginRecorded", false);
            Scribe_TargetInfo.Look(ref lastSyncedTarget, "vanillaAdapterLastSyncedTarget");
            Scribe_Values.Look(ref originOffsetDistance, "vanillaAdapterOriginOffset", 6f);
            Scribe_Values.Look(ref enableOriginOffset, "vanillaAdapterEnableOriginOffset", true);
            Scribe_Values.Look(ref enableUsedTargetSync, "vanillaAdapterEnableUsedTargetSync", true);
        }
    }

    /// <summary>
    /// 命中前检查结果。
    /// </summary>
    public struct ImpactCheckResult
    {
        /// <summary>
        /// 是否强制打地面（跳过vanilla的usedTarget命中逻辑）。
        /// </summary>
        public bool ForceGround;

        /// <summary>
        /// 原因说明（用于日志）。
        /// </summary>
        public string Reason;
    }
}