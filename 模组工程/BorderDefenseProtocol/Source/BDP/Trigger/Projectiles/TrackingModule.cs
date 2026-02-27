using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 追踪模块——实时追踪目标的弹道控制。
    /// Priority=15（路径修改，在GuidedModule之后执行）。
    ///
    /// 管线接口：
    ///   IBDPPathResolver  — 每tick修改destination（核心追踪逻辑）
    ///   IBDPTickObserver  — 计数飞行时间，处理超时
    ///   IBDPArrivalHandler — 到达时判断是否需要继续追踪
    ///
    /// 与GuidedModule共存：仅在IsOnFinalSegment=true时激活追踪。
    /// 单独使用时：trackingDelay结束后激活。
    /// </summary>
    public class TrackingModule : IBDPProjectileModule,
        IBDPPathResolver, IBDPTickObserver, IBDPArrivalHandler
    {
        /// <summary>追踪配置引用。</summary>
        private readonly BDPTrackingConfig config;

        /// <summary>当前飞行角度（度，0=东偏北方向，与Atan2(x,z)一致）。</summary>
        private float currentAngle;

        /// <summary>当前角速度（度/tick，仅Smooth模式使用）。</summary>
        private float angularVelocity;

        /// <summary>飞行已持续tick数（从发射开始计）。</summary>
        private int flyingTicks;

        /// <summary>是否已初始化角度（首次ResolvePath时从弹道方向初始化）。</summary>
        private bool angleInitialized;

        /// <summary>上次搜索目标的tick。</summary>
        private int lastSearchTick;

        public int Priority => 15;

        /// <summary>无参构造——Scribe反序列化需要。</summary>
        public TrackingModule() { config = null; }

        public TrackingModule(BDPTrackingConfig config)
        {
            this.config = config;
        }

        public void OnSpawn(Bullet_BDP host)
        {
            // 初始追踪目标 = 弹道的最终目标（intendedTarget）
            host.TrackingTarget = host.FinalTarget;
        }

        // ══════════════════════════════════════════
        //  IBDPPathResolver — 每tick修改destination
        // ══════════════════════════════════════════

        public void ResolvePath(Bullet_BDP host, ref PathContext ctx)
        {
            var cfg = GetConfig(host);
            if (cfg == null) return;

            // 条件1：必须在最终飞行段（与GuidedModule协作）
            if (!host.IsOnFinalSegment) return;

            // 条件2：追踪延迟
            if (flyingTicks < cfg.trackingDelay) return;

            // 条件3：需要有效目标
            if (!IsTargetValid(host.TrackingTarget))
            {
                host.IsTracking = false;
                return;
            }

            // 初始化角度（首次激活时从当前飞行方向读取）
            if (!angleInitialized)
            {
                Vector3 dir = (ctx.Destination - ctx.Origin).Yto0();
                if (dir.sqrMagnitude > 0.001f)
                    currentAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                angleInitialized = true;
            }

            // 计算朝向目标的期望角度
            Vector3 targetPos = host.TrackingTarget.Thing != null
                ? host.TrackingTarget.Thing.DrawPos
                : host.TrackingTarget.Cell.ToVector3Shifted();
            Vector3 toTarget = (targetPos - ctx.Origin).Yto0();
            if (toTarget.sqrMagnitude < 0.001f) return;

            float desiredAngle = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;

            // 脱锁检查：角度超限
            float angleDiff = Mathf.DeltaAngle(currentAngle, desiredAngle);
            if (Mathf.Abs(angleDiff) > cfg.maxLockAngle)
            {
                // 脱锁，尝试重搜索
                host.IsTracking = false;
                TrySearchNewTarget(host, ctx.Origin.ToIntVec3(), cfg);
                if (!host.IsTracking) return; // 未找到新目标，保持直飞

                // 找到新目标，重新计算
                targetPos = host.TrackingTarget.Thing.DrawPos;
                toTarget = (targetPos - ctx.Origin).Yto0();
                desiredAngle = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                angleDiff = Mathf.DeltaAngle(currentAngle, desiredAngle);
            }

            // 转向计算
            if (cfg.turnMode == TrackingTurnMode.Simple)
                UpdateAngleSimple(angleDiff, cfg);
            else
                UpdateAngleSmooth(angleDiff, cfg);

            // 应用新方向到destination
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 newDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            float dist = (ctx.Destination - ctx.Origin).Yto0().magnitude;
            if (dist < 0.1f) dist = 1f;
            ctx.Destination = ctx.Origin + newDir * dist
                              + Vector3.up * ctx.Destination.y;

            host.IsTracking = true;
        }

        /// <summary>Simple模式：角速度限幅。</summary>
        private void UpdateAngleSimple(float angleDiff, BDPTrackingConfig cfg)
        {
            float turn = Mathf.Clamp(angleDiff, -cfg.maxTurnRate, cfg.maxTurnRate);
            currentAngle += turn;
        }

        /// <summary>Smooth模式：角加速度 + 阻尼。</summary>
        private void UpdateAngleSmooth(float angleDiff, BDPTrackingConfig cfg)
        {
            // 角加速度朝向目标
            float accel = Mathf.Clamp(angleDiff, -cfg.angularAccel, cfg.angularAccel);
            angularVelocity += accel;
            // 限幅角速度
            angularVelocity = Mathf.Clamp(angularVelocity,
                -cfg.maxTurnRate, cfg.maxTurnRate);
            // 阻尼衰减
            angularVelocity *= cfg.damping;
            currentAngle += angularVelocity;
        }

        // ══════════════════════════════════════════
        //  IBDPTickObserver — 飞行计时 + 超时自毁
        // ══════════════════════════════════════════

        public void OnTick(Bullet_BDP host)
        {
            flyingTicks++;

            var cfg = GetConfig(host);
            if (cfg == null) return;

            // 超时自毁
            if (flyingTicks >= cfg.maxFlyingTicks)
            {
                host.IsTracking = false;
                if (host.Spawned)
                    host.Destroy();
                return;
            }

            // 目标丢失时定期重搜索
            if (!host.IsTracking && host.IsOnFinalSegment
                && flyingTicks >= cfg.trackingDelay)
            {
                TrySearchNewTarget(host, host.Position, cfg);
            }
        }

        // ══════════════════════════════════════════
        //  IBDPArrivalHandler — 到达时继续追踪
        // ══════════════════════════════════════════

        public void HandleArrival(Bullet_BDP host, ref ArrivalContext ctx)
        {
            // 如果正在追踪且目标仍有效，重定向继续飞行
            if (!host.IsTracking) return;
            if (!IsTargetValid(host.TrackingTarget)) return;

            Vector3 targetPos = host.TrackingTarget.Thing != null
                ? host.TrackingTarget.Thing.DrawPos
                : host.TrackingTarget.Cell.ToVector3Shifted();
            float distToTarget = (targetPos - host.DrawPos).Yto0().magnitude;

            // 目标足够近（<1格），让原版Impact处理命中
            if (distToTarget < 1f) return;

            // 目标还远，重定向继续追踪
            host.RedirectFlight(host.DrawPos, targetPos);
            ctx.Continue = true;
        }

        // ══════════════════════════════════════════
        //  辅助方法
        // ══════════════════════════════════════════

        /// <summary>获取配置（优先用构造时传入的，回退到def上的）。</summary>
        private BDPTrackingConfig GetConfig(Bullet_BDP host)
        {
            return config ?? host.def.GetModExtension<BDPTrackingConfig>();
        }

        /// <summary>目标是否有效（活着、在地图上）。</summary>
        private static bool IsTargetValid(LocalTargetInfo target)
        {
            if (!target.IsValid) return false;
            if (target.Thing == null) return false; // Cell目标不追踪
            if (target.Thing.Destroyed) return false;
            if (!target.Thing.Spawned) return false;
            if (target.Thing is Pawn p && (p.Dead || p.Downed)) return false;
            return true;
        }

        /// <summary>尝试搜索新目标。</summary>
        private void TrySearchNewTarget(Bullet_BDP host, IntVec3 position,
            BDPTrackingConfig cfg)
        {
            // 搜索间隔限制
            int currentTick = Find.TickManager.TicksGame;
            if (currentTick - lastSearchTick < cfg.searchInterval) return;
            lastSearchTick = currentTick;

            var newTarget = TargetSearcher.FindNearestEnemy(
                host.Map, position, cfg.searchRadius, host.Launcher);
            if (newTarget.IsValid)
            {
                host.TrackingTarget = newTarget;
                host.IsTracking = true;
                angleInitialized = false; // 重新初始化角度朝向新目标
            }
        }

        // ══════════════════════════════════════════
        //  序列化
        // ══════════════════════════════════════════

        public void ExposeData()
        {
            Scribe_Values.Look(ref currentAngle, "trackingAngle", 0f);
            Scribe_Values.Look(ref angularVelocity, "trackingAngVel", 0f);
            Scribe_Values.Look(ref flyingTicks, "trackingFlyingTicks", 0);
            Scribe_Values.Look(ref angleInitialized, "trackingAngleInit", false);
            Scribe_Values.Look(ref lastSearchTick, "trackingLastSearch", 0);
        }
    }
}
