using BDP.Core.Projectiles.Visual;
using BDP.Support.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Projectiles.BeamTrail
{
    /// <summary>
    /// 光束拖尾附加件。
    /// 它只消费主模组发布的中性视觉事件，不参与飞行、命中、伤害等主逻辑。
    /// </summary>
    internal sealed class BeamTrailAttachment : IProjectileVisualAttachment
    {
        /// <summary>
        /// 当前附加件冻结使用的外观快照。
        /// </summary>
        private readonly BeamTrailAppearanceSnapshot appearance;

        /// <summary>
        /// 当前附加件是否已经拥有连续锚点。
        /// </summary>
        private bool hasAnchor;

        /// <summary>
        /// 当前附加件维护的上一段连续锚点。
        /// </summary>
        private Vector3 lastAnchor;

        /// <summary>
        /// 用一份外观快照初始化当前拖尾附加件。
        /// </summary>
        /// <param name="appearance">当前附加件冻结使用的外观快照。</param>
        internal BeamTrailAttachment(BeamTrailAppearanceSnapshot appearance)
        {
            this.appearance = appearance;
        }

        /// <summary>
        /// 接收投射物发射事件。
        /// </summary>
        /// <param name="context">当前发射上下文。</param>
        public void OnLaunch(in ProjectileVisualLaunchContext context)
        {
            Vector3 anchor = context.LaunchOrigin;
            if (appearance != null
                && appearance.StartForwardOffset > 0f
                && context.LaunchDirection.sqrMagnitude > 0.0001f)
            {
                anchor += context.LaunchDirection.normalized * appearance.StartForwardOffset;
            }

            BeamTrailMapComponent mapComponent = BeamTrailMapComponent.GetOrCreate(context.Map);
            mapComponent?.ClearLiveSegment(context.ProjectileThingId);
            lastAnchor = NormalizePoint(anchor);
            hasAnchor = true;
            LogIfNeeded(
                "launch",
                context.ProjectileThingId,
                context.AttackInstanceId,
                context.ResultId,
                lastAnchor,
                lastAnchor,
                0);
        }

        /// <summary>
        /// 接收一次真实飞行样本，并直接把这一段样本落成一段拖尾线段。
        /// 这一段先只作为活体头段临时显示；只有下一次样本来到时，上一段才允许沉淀成历史拖尾。
        /// </summary>
        /// <param name="context">当前飞行样本上下文。</param>
        public void OnFlightSample(in ProjectileVisualFlightSampleContext context)
        {
            if (appearance == null || context.Map == null)
            {
                return;
            }

            Vector3 start = hasAnchor ? lastAnchor : NormalizePoint(context.SampleStart);
            Vector3 end = NormalizePoint(context.SampleEnd);
            float distance = (end - start).MagnitudeHorizontal();
            if (distance <= 0.0001f)
            {
                lastAnchor = end;
                hasAnchor = true;
                return;
            }

            BeamTrailMapComponent mapComponent = BeamTrailMapComponent.GetOrCreate(context.Map);
            if (mapComponent == null)
            {
                return;
            }

            mapComponent.PromoteLiveSegment(context.ProjectileThingId);
            mapComponent.SetLiveSegment(context.ProjectileThingId, start, end, appearance);

            lastAnchor = end;
            hasAnchor = true;
            LogIfNeeded(
                "sample",
                context.ProjectileThingId,
                context.AttackInstanceId,
                context.ResultId,
                start,
                end,
                1);
        }

        /// <summary>
        /// 接收读档恢复事件。
        /// </summary>
        /// <param name="context">当前恢复上下文。</param>
        public void OnRestored(in ProjectileVisualRestoreContext context)
        {
            BeamTrailMapComponent mapComponent = BeamTrailMapComponent.GetOrCreate(context.Map);
            mapComponent?.ClearLiveSegment(context.ProjectileThingId);
            lastAnchor = NormalizePoint(context.CurrentPosition);
            hasAnchor = true;
            LogIfNeeded(
                "restore",
                context.ProjectileThingId,
                context.AttackInstanceId,
                context.ResultId,
                lastAnchor,
                lastAnchor,
                0);
        }

        /// <summary>
        /// 接收终止事件。
        /// </summary>
        /// <param name="context">当前终止上下文。</param>
        public void OnTerminate(in ProjectileVisualTerminateContext context)
        {
            BeamTrailMapComponent mapComponent = BeamTrailMapComponent.GetOrCreate(context.Map);
            mapComponent?.ClearLiveSegment(context.ProjectileThingId);
            lastAnchor = NormalizePoint(context.CurrentPosition);
            hasAnchor = false;
            LogIfNeeded(
                "terminate",
                context.ProjectileThingId,
                context.AttackInstanceId,
                context.ResultId,
                lastAnchor,
                lastAnchor,
                0);
        }

        /// <summary>
        /// 统一把坐标压回地图平面。
        /// 拖尾显示高度由线段自身的 `AltitudeOffset（高度偏移）` 决定。
        /// </summary>
        /// <param name="point">待归一的坐标点。</param>
        /// <returns>归一到地图平面后的坐标点。</returns>
        private static Vector3 NormalizePoint(Vector3 point)
        {
            point.y = 0f;
            return point;
        }

        /// <summary>
        /// 在调试开关打开时输出节流日志。
        /// </summary>
        /// <param name="stage">当前日志阶段。</param>
        /// <param name="projectileThingId">当前投射物实体标识。</param>
        /// <param name="attackInstanceId">当前攻击实例标识。</param>
        /// <param name="resultId">当前正式结果标识。</param>
        /// <param name="start">当前记录起点。</param>
        /// <param name="end">当前记录终点。</param>
        /// <param name="segmentCount">当前追加线段数。</param>
        private void LogIfNeeded(
            string stage,
            string projectileThingId,
            string attackInstanceId,
            string resultId,
            Vector3 start,
            Vector3 end,
            int segmentCount)
        {
            if (appearance == null || !appearance.DebugLogging || !Prefs.DevMode)
            {
                return;
            }

            string safeProjectileId = string.IsNullOrWhiteSpace(projectileThingId) ? "<none>" : projectileThingId;
            string safeAttackId = string.IsNullOrWhiteSpace(attackInstanceId) ? "<none>" : attackInstanceId;
            string safeResultId = string.IsNullOrWhiteSpace(resultId) ? "<none>" : resultId;
            BdpDiagnostics.Throttled(
                "beamtrail.attachment." + stage + "." + safeProjectileId,
                "光束拖尾附加件事件。stage=" + stage
                + ", projectile=" + safeProjectileId
                + ", attackId=" + safeAttackId
                + ", resultId=" + safeResultId
                + ", start=" + start
                + ", end=" + end
                + ", segments=" + segmentCount,
                15);
        }
    }
}
