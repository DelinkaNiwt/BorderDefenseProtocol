using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 浮空实际状态、动画锚点和生命周期收口控制器。
    /// </summary>
    public static class FloatingFlightStateController
    {
        /// <summary>保护控制器内部增删 Hediff 时不重复响应回调。</summary>
        private static int hediffNotificationSuppressionDepth;

        /// <summary>Hediff 是否正在由控制器内部修改。</summary>
        internal static bool SuppressHediffNotifications
        {
            get { return hediffNotificationSuppressionDepth > 0; }
        }

        /// <summary>按当前能力、偏好、移动和屋顶环境收口一个 Pawn。</summary>
        public static void Reconcile(Pawn pawn, FloatingFlightRecord record)
        {
            if (pawn == null || record == null)
            {
                return;
            }

            if (!pawn.Spawned || pawn.Dead || pawn.Destroyed || pawn.Downed)
            {
                ForceGroundedImmediately(pawn, record);
                return;
            }

            EnsureSelfPermissionRegistration(pawn);

            FloatingFlightMode mode = FloatingFlightModeResolver.ResolveEffectiveMode(pawn);
            if (record.GroundedForRoofSegment && mode != FloatingFlightMode.MoveOnly)
            {
                record.GroundedForRoofSegment = false;
            }

            if (record.GroundedForRoofSegment
                && (pawn.pather == null || !pawn.pather.Moving)
                && !pawn.Position.Roofed(pawn.Map))
            {
                record.GroundedForRoofSegment = false;
            }

            bool shouldFly = !record.GroundedForRoofSegment && ShouldActuallyFly(pawn, mode);
            bool actualFlying = FloatingFlightUtility.IsActuallyFlying(pawn);

            if (record.PendingSafeLanding)
            {
                if (shouldFly)
                {
                    record.PendingSafeLanding = false;
                }
                else if (FloatingFlightLandingUtility.IsGroundLegal(pawn, pawn.Position))
                {
                    RequestActualFlight(pawn, record, false);
                }
                else
                {
                    return;
                }
            }

            if (shouldFly == actualFlying)
            {
                return;
            }

            RequestActualFlight(pawn, record, shouldFly);
        }

        /// <summary>
        /// 在原版征召属性切换进入原版工作清理前，立即同步实际浮空状态。
        /// 玩家 Pawn 取消征召时必须先收地，避免原版工作寻找阶段继续把它当作浮空 Pawn。
        /// </summary>
        internal static void SynchronizeForDraftChange(Pawn pawn, bool drafted)
        {
            if (pawn == null
                || pawn.health == null
                || Scribe.mode != LoadSaveMode.Inactive)
            {
                return;
            }

            bool actualFlying = FloatingFlightUtility.IsActuallyFlying(pawn);
            bool hasCapability = FloatingFlightCapabilityResolver.HasCapability(pawn);
            if (!hasCapability && !actualFlying)
            {
                return;
            }

            // 非玩家 Pawn 的模式由非玩家扩展提供器决定，不把 Drafted 重新解释为关闭。
            if (!pawn.IsPlayerControlled)
            {
                if (!actualFlying && !hasCapability)
                {
                    return;
                }

                FloatingFlightRecord nonPlayerRecord =
                    FloatingFlightLedgerService.GetOrCreateRecord(pawn);
                Reconcile(pawn, nonPlayerRecord);
                return;
            }

            if (!drafted && !actualFlying)
            {
                return;
            }

            FloatingFlightRecord record = FloatingFlightLedgerService.GetOrCreateRecord(pawn);
            if (record == null)
            {
                return;
            }

            record.GroundedForRoofSegment = false;
            FloatingFlightMode mode = drafted
                ? record.PreferredMode
                : FloatingFlightMode.Off;
            bool shouldFly = ShouldActuallyFly(pawn, mode);
            RequestActualFlight(pawn, record, shouldFly, !drafted);
        }

        /// <summary>请求添加或移除实际飞行 Hediff。</summary>
        public static void RequestActualFlight(
            Pawn pawn,
            FloatingFlightRecord record,
            bool shouldFly)
        {
            RequestActualFlight(pawn, record, shouldFly, false);
        }

        /// <summary>
        /// 按目标状态切换实际 Hediff；强制收地用于取消征召等必须立即完成的边界。
        /// </summary>
        private static void RequestActualFlight(
            Pawn pawn,
            FloatingFlightRecord record,
            bool shouldFly,
            bool forceGrounding)
        {
            if (pawn == null || record == null || pawn.health == null)
            {
                return;
            }

            bool actualFlying = FloatingFlightUtility.IsActuallyFlying(pawn);
            if (actualFlying == shouldFly)
            {
                if (shouldFly)
                {
                    record.PendingSafeLanding = false;
                }

                return;
            }

            if (!shouldFly
                && pawn.Spawned
                && !FloatingFlightLandingUtility.IsGroundLegal(pawn, pawn.Position))
            {
                if (!forceGrounding)
                {
                    record.PendingSafeLanding = true;
                    return;
                }

                // 取消征召必须在原版工作清理前收回合法地面，不能留下永久等待的 Hediff。
                if (FloatingFlightLandingUtility.TryFindNearestLegalCell(
                    pawn,
                    out IntVec3 legalCell))
                {
                    pawn.Position = legalCell;
                }
            }

            BeginTransition(record, pawn, actualFlying, shouldFly);
            if (shouldFly)
            {
                record.PendingSafeLanding = false;
                AddFlightHediff(pawn);
            }
            else
            {
                record.PendingSafeLanding = false;
                RemoveAllFlightHediffs(pawn);
            }
        }

        /// <summary>计算当前地图绘制使用的线性表现高度因子。</summary>
        public static float GetPresentationFactor(
            Pawn pawn,
            FloatingFlightRecord record,
            int tick)
        {
            if (record == null)
            {
                return 0f;
            }

            bool targetFlying = FloatingFlightUtility.IsActuallyFlying(pawn);
            return InterpolateFactor(record, targetFlying, tick, ResolveSettings());
        }

        /// <summary>Hediff 外部添加通知，建立从地面到空中的过渡锚点。</summary>
        public static void NotifyHediffAdded(Pawn pawn)
        {
            if (pawn == null || SuppressHediffNotifications || Scribe.mode != LoadSaveMode.Inactive)
            {
                return;
            }

            FloatingFlightRecord record = FloatingFlightLedgerService.GetOrCreateRecord(pawn);
            if (record == null)
            {
                return;
            }

            record.GroundedForRoofSegment = false;

            if (CountFlightHediffs(pawn) > 1)
            {
                RemoveDuplicateFlightHediffs(pawn);
                return;
            }

            BeginTransition(record, pawn, false, true);
            record.PendingSafeLanding = false;
        }

        /// <summary>Hediff 外部移除通知，建立从当前高度到地面的过渡锚点。</summary>
        public static void NotifyHediffRemoved(Pawn pawn)
        {
            if (pawn == null || SuppressHediffNotifications || Scribe.mode != LoadSaveMode.Inactive)
            {
                return;
            }

            if (FloatingFlightUtility.IsActuallyFlying(pawn))
            {
                RemoveDuplicateFlightHediffs(pawn);
                return;
            }

            FloatingFlightRecord record = FloatingFlightLedgerService.GetOrCreateRecord(pawn);
            if (record == null)
            {
                return;
            }

            BeginTransition(record, pawn, true, false);
            record.PendingSafeLanding = false;
        }

        /// <summary>倒地、死亡、离图时立即清理飞行状态和视觉高度。</summary>
        public static void ForceGroundedImmediately(Pawn pawn, FloatingFlightRecord record)
        {
            if (record == null)
            {
                return;
            }

            if (pawn != null && pawn.health != null)
            {
                // 安全搬回地面只属于仍在 BDP 实际飞行的人物，历史账本本身不是搬动许可。
                if (FloatingFlightUtility.IsActuallyFlying(pawn)
                    && pawn.Spawned
                    && !FloatingFlightLandingUtility.IsGroundLegal(pawn, pawn.Position)
                    && FloatingFlightLandingUtility.TryFindNearestLegalCell(pawn, out IntVec3 legalCell))
                {
                    pawn.Position = legalCell;
                }

                RemoveAllFlightHediffs(pawn);
            }

            record.TransitionStartFactor = 0f;
            record.TransitionStartTick = CurrentTick;
            record.PendingSafeLanding = false;
            record.GroundedForRoofSegment = false;
        }

        /// <summary>由 GameComponent 在 Pawn 记录变化时调用的当前时间入口。</summary>
        public static int CurrentTick
        {
            get
            {
                try
                {
                    return Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>判断指定模式和实时环境是否应持有飞行 Hediff。</summary>
        private static bool ShouldActuallyFly(Pawn pawn, FloatingFlightMode mode)
        {
            if (pawn == null || pawn.Map == null || pawn.Position.Roofed(pawn.Map))
            {
                return false;
            }

            if (mode == FloatingFlightMode.Always)
            {
                return true;
            }

            return mode == FloatingFlightMode.MoveOnly && pawn.pather != null && pawn.pather.Moving;
        }

        /// <summary>
        /// 确保读档恢复或跨地图后的浮空 Hediff 已重新进入当前地图许可索引。
        /// </summary>
        private static void EnsureSelfPermissionRegistration(Pawn pawn)
        {
            Hediff_FloatingFlight hediff = pawn?.health?.hediffSet?.GetFirstHediffOfDef(
                FloatingFlightDefOf.BDP_Hediff_FloatingFlight) as Hediff_FloatingFlight;
            hediff?.EnsureSelfPermissionRegistration();
        }

        /// <summary>按旧目标和新目标写入一次可反转的过渡锚点。</summary>
        private static void BeginTransition(
            FloatingFlightRecord record,
            Pawn pawn,
            bool previousTargetFlying,
            bool nextTargetFlying)
        {
            FloatingFlightSettingsExtension settings = ResolveSettings();
            record.TransitionStartFactor = InterpolateFactor(
                record,
                previousTargetFlying,
                CurrentTick,
                settings);
            record.TransitionStartTick = CurrentTick;
        }

        /// <summary>按照记录锚点和目标状态计算线性插值。</summary>
        private static float InterpolateFactor(
            FloatingFlightRecord record,
            bool targetFlying,
            int tick,
            FloatingFlightSettingsExtension settings)
        {
            float target = targetFlying ? 1f : 0f;
            int durationTicks = Mathf.Max(1, settings?.transitionTicks ?? 30);
            float progress = Mathf.Clamp01(
                (tick - record.TransitionStartTick) / (float)durationTicks);
            return Mathf.Clamp01(Mathf.Lerp(record.TransitionStartFactor, target, progress));
        }

        /// <summary>读取浮空 Hediff 的正式配置扩展。</summary>
        private static FloatingFlightSettingsExtension ResolveSettings()
        {
            return FloatingFlightDefOf.BDP_Hediff_FloatingFlight?.GetModExtension<FloatingFlightSettingsExtension>()
                ?? new FloatingFlightSettingsExtension();
        }

        /// <summary>添加唯一一份浮空 Hediff。</summary>
        private static void AddFlightHediff(Pawn pawn)
        {
            if (FloatingFlightUtility.IsActuallyFlying(pawn))
            {
                RemoveDuplicateFlightHediffs(pawn);
                return;
            }

            using (SuppressHediffNotificationsScope())
            {
                pawn.health.AddHediff(FloatingFlightDefOf.BDP_Hediff_FloatingFlight);
            }
        }

        /// <summary>移除指定 Pawn 的全部浮空 Hediff。</summary>
        private static void RemoveAllFlightHediffs(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return;
            }

            List<Hediff> matches = new List<Hediff>();
            for (int index = 0; index < pawn.health.hediffSet.hediffs.Count; index++)
            {
                Hediff hediff = pawn.health.hediffSet.hediffs[index];
                if (hediff != null && hediff.def == FloatingFlightDefOf.BDP_Hediff_FloatingFlight)
                {
                    matches.Add(hediff);
                }
            }

            using (SuppressHediffNotificationsScope())
            {
                for (int index = 0; index < matches.Count; index++)
                {
                    pawn.health.RemoveHediff(matches[index]);
                }
            }
        }

        /// <summary>保留第一份浮空 Hediff，移除重复来源造成的重复状态。</summary>
        private static void RemoveDuplicateFlightHediffs(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return;
            }

            bool keptOne = false;
            List<Hediff> duplicates = new List<Hediff>();
            for (int index = 0; index < pawn.health.hediffSet.hediffs.Count; index++)
            {
                Hediff hediff = pawn.health.hediffSet.hediffs[index];
                if (hediff == null || hediff.def != FloatingFlightDefOf.BDP_Hediff_FloatingFlight)
                {
                    continue;
                }

                if (!keptOne)
                {
                    keptOne = true;
                }
                else
                {
                    duplicates.Add(hediff);
                }
            }

            using (SuppressHediffNotificationsScope())
            {
                for (int index = 0; index < duplicates.Count; index++)
                {
                    pawn.health.RemoveHediff(duplicates[index]);
                }
            }
        }

        /// <summary>统计指定 Pawn 当前的浮空 Hediff 数量。</summary>
        private static int CountFlightHediffs(Pawn pawn)
        {
            int count = 0;
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return count;
            }

            for (int index = 0; index < pawn.health.hediffSet.hediffs.Count; index++)
            {
                if (pawn.health.hediffSet.hediffs[index]?.def == FloatingFlightDefOf.BDP_Hediff_FloatingFlight)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>建立一个作用域，保证内部增删 Hediff 的回调不会递归。</summary>
        private static IDisposable SuppressHediffNotificationsScope()
        {
            hediffNotificationSuppressionDepth++;
            return new HediffNotificationSuppressionScope();
        }

        /// <summary>内部 Hediff 回调抑制作用域。</summary>
        private sealed class HediffNotificationSuppressionScope : IDisposable
        {
            /// <summary>释放作用域并恢复回调处理。</summary>
            public void Dispose()
            {
                hediffNotificationSuppressionDepth = Mathf.Max(
                    0,
                    hediffNotificationSuppressionDepth - 1);
            }
        }
    }
}
