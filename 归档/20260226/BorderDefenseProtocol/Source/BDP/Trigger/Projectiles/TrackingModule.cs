using System;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 追踪模块——每tick将弹道destination转向目标当前位置，受最大转向角限制。
    /// Priority=5（路径修改，高优先级，在GuidedModule之前执行）。
    ///
    /// 管线接口：IBDPPathResolver（每tick修改destination实现追踪）。
    ///
    /// 核心算法：
    ///   1. 计算当前飞行方向与目标方向的夹角
    ///   2. 将夹角Clamp到maxTurnAnglePerTick
    ///   3. 用Quaternion旋转当前方向得到新destination
    /// </summary>
    public class TrackingModule : IBDPProjectileModule, IBDPPathResolver
    {
        /// <summary>追踪配置引用。</summary>
        private BDPTrackingConfig config;

        /// <summary>当前追踪目标。</summary>
        private LocalTargetInfo trackingTarget;

        /// <summary>目标最后已知位置（目标丢失时飞向此处）。</summary>
        private Vector3 lastKnownPos;

        /// <summary>已经过的tick数（用于延迟和超时判断）。</summary>
        private int elapsedTicks;

        /// <summary>追踪是否激活。</summary>
        private bool trackingActive = true;

        public int Priority => 5;

        /// <summary>无参构造——Scribe反序列化需要。</summary>
        public TrackingModule() { }

        public TrackingModule(BDPTrackingConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// SpawnSetup时调用——从宿主的intendedTarget初始化追踪目标。
        /// </summary>
        public void OnSpawn(Bullet_BDP host)
        {
            trackingTarget = host.intendedTarget;
            elapsedTicks = 0;
            trackingActive = true;

            // 记录初始目标位置作为lastKnownPos
            if (trackingTarget.HasThing && trackingTarget.Thing.Spawned)
                lastKnownPos = trackingTarget.Thing.DrawPos;
            else if (trackingTarget.IsValid)
                lastKnownPos = trackingTarget.CenterVector3;
        }

        /// <summary>
        /// IBDPPathResolver实现——核心转向算法。
        /// 每tick计算当前飞行方向与目标方向的夹角，Clamp后旋转destination。
        /// </summary>
        public void ResolvePath(Bullet_BDP host, ref PathContext ctx)
        {
            if (!trackingActive) return;

            elapsedTicks++;

            // 延迟检查：未到点火时间则跳过
            var cfg = config ?? host.def.GetModExtension<BDPTrackingConfig>();
            if (cfg == null) return;
            if (elapsedTicks <= cfg.trackingDelayTicks) return;

            // 超时检查：超过最大追踪tick数则停止
            int trackingElapsed = elapsedTicks - cfg.trackingDelayTicks;
            if (cfg.maxTrackingTicks > 0 && trackingElapsed > cfg.maxTrackingTicks)
            {
                trackingActive = false;
                return;
            }

            // 获取目标位置
            if (!TryGetTargetPosition(host, cfg, out Vector3 targetPos))
                return;

            // ── 核心转向算法 ──
            Vector3 currentPos = host.DrawPos;
            Vector3 currentDir = (ctx.Destination - currentPos).normalized;
            Vector3 desiredDir = (targetPos - currentPos).normalized;

            // 避免零向量（目标与当前位置重合）
            if (currentDir.sqrMagnitude < 0.001f || desiredDir.sqrMagnitude < 0.001f)
                return;

            // 计算夹角并Clamp到最大转向角
            float angle = Vector3.SignedAngle(currentDir, desiredDir, Vector3.up);
            float clampedAngle = Mathf.Clamp(angle, -cfg.maxTurnAnglePerTick, cfg.maxTurnAnglePerTick);

            // 用Quaternion旋转当前方向
            Quaternion rotation = Quaternion.AngleAxis(clampedAngle, Vector3.up);
            Vector3 newDir = rotation * currentDir;

            // 保持原始飞行距离，更新destination
            float remainDist = (ctx.Destination - currentPos).magnitude;
            ctx.Destination = currentPos + newDir * remainDist;
            ctx.Modified = true;
        }

        /// <summary>
        /// 尝试获取目标位置。目标有效则返回其DrawPos；
        /// 目标丢失则根据配置飞向lastKnownPos或搜索新目标。
        /// </summary>
        private bool TryGetTargetPosition(Bullet_BDP host, BDPTrackingConfig cfg, out Vector3 pos)
        {
            // 目标仍然有效且存活
            if (trackingTarget.HasThing && trackingTarget.Thing.Spawned)
            {
                pos = trackingTarget.Thing.DrawPos;
                lastKnownPos = pos; // 持续更新最后已知位置
                return true;
            }

            // 目标丢失——尝试搜索新目标
            if (cfg.retargetOnLost && host.Map != null)
            {
                Thing newTarget = TryFindNewTarget(host, cfg);
                if (newTarget != null)
                {
                    trackingTarget = new LocalTargetInfo(newTarget);
                    pos = newTarget.DrawPos;
                    lastKnownPos = pos;
                    return true;
                }
            }

            // 无法重新锁定——飞向最后已知位置，然后停止追踪
            pos = lastKnownPos;
            trackingActive = false;
            return true; // 仍然返回true，让弹道飞向lastKnownPos
        }

        /// <summary>
        /// 在指定半径内搜索符合过滤条件的最近目标。
        /// 使用GenRadial.RadialDistinctThingsAround高效搜索。
        /// </summary>
        private Thing TryFindNewTarget(Bullet_BDP host, BDPTrackingConfig cfg)
        {
            IntVec3 center = host.Position;
            Map map = host.Map;
            float bestDistSq = float.MaxValue;
            Thing bestTarget = null;

            foreach (Thing t in GenRadial.RadialDistinctThingsAround(center, map, cfg.retargetRadius, true))
            {
                // 跳过自身和发射者
                if (t == host || t == host.Launcher) continue;
                if (!t.Spawned) continue;
                if (!MatchesFilter(t, cfg.targetFilter)) continue;

                // 不追踪友方（与发射者同阵营）
                if (t.Faction != null && t.Faction == host.Launcher?.Faction) continue;

                float distSq = (t.DrawPos - host.DrawPos).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestTarget = t;
                }
            }

            return bestTarget;
        }

        /// <summary>检查目标是否匹配过滤器类型。</summary>
        private static bool MatchesFilter(Thing t, TrackingTargetFilter filter)
        {
            switch (filter)
            {
                case TrackingTargetFilter.Pawn:
                    return t is Pawn;
                case TrackingTargetFilter.Projectile:
                    return t is Projectile;
                case TrackingTargetFilter.Building:
                    return t is Building;
                case TrackingTargetFilter.PawnOrProjectile:
                    return t is Pawn || t is Projectile;
                case TrackingTargetFilter.Any:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 序列化——存档/读档支持。
        /// config从def重新获取，只需序列化运行时状态。
        /// </summary>
        public void ExposeData()
        {
            Scribe_TargetInfo.Look(ref trackingTarget, "trackingTarget");
            Scribe_Values.Look(ref lastKnownPos, "lastKnownPos");
            Scribe_Values.Look(ref elapsedTicks, "elapsedTicks");
            Scribe_Values.Look(ref trackingActive, "trackingActive", true);
        }
    }
}
