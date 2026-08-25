using System;
using System.Collections.Generic;
using BDP.Core.CombatBody.Wounds.Presentation;
using Verse;

namespace BDP.Core.CombatBody.Wounds
{
    /// <summary>
    /// 战斗体伤口运行时接线层。
    /// 它只维护 Active / Collapsing 期间的伤口派生运行时，不持有伤口事实。
    /// </summary>
    internal sealed class CombatBodyWoundRuntime : IExposable
    {
        /// <summary>
        /// 伤口 Trion drain 发布绑定。
        /// </summary>
        private readonly CombatBodyWoundTrionBinding trionBinding = new CombatBodyWoundTrionBinding();

        /// <summary>
        /// <summary>
        /// 本 tick 已过期的伤口 drain id 复用列表。
        /// </summary>
        private readonly List<int> expiredDrainIds = new List<int>();

        /// <summary>
        /// 下次允许低频校准的游戏 tick。
        /// </summary>
        private int nextCalibrationTick;

        /// <summary>
        /// 保存适用阶段内仍在生效的伤口派生运行时。
        /// 这里只存 drain 生命周期，不存原版伤口事实。
        /// </summary>
        public void ExposeData()
        {
            trionBinding.ExposeData();
            Scribe_Values.Look(ref nextCalibrationTick, "nextCalibrationTick", 0);
        }

        /// <summary>
        /// 重建当前伤口运行时适用阶段内的伤口运行时。
        /// 注意：伤口 Trion 流失由明确伤口变化触发；重建只清理旧派生状态，不主动启动旧伤口流失。
        /// </summary>
        internal void RebuildActiveWounds(Pawn pawn)
        {
            if (!CombatBodyWoundPolicy.IsCombatBodyWoundRuntimeApplicable(pawn))
            {
                ClearActiveRuntime(pawn);
                return;
            }

            trionBinding.ClearAll(pawn);
            CombatBodyWoundPresentationRegistry.ClearAll(pawn);
            ScheduleNextCalibration(CombatBodyWoundPolicy.Resolve());
        }

        /// <summary>
        /// 响应伤口新增或变化。
        /// </summary>
        internal void NotifyWoundAddedOrChanged(Pawn pawn, Hediff hediff)
        {
            if (hediff == null || !CombatBodyWoundPolicy.IsSupportedWound(hediff))
            {
                return;
            }

            if (!CombatBodyWoundPolicy.IsCombatBodyWoundRuntimeApplicable(pawn))
            {
                trionBinding.RemoveWoundDrain(pawn, hediff);
                CombatBodyWoundPresentationRegistry.NotifyWoundDrainExpired(pawn, hediff.loadID);
                return;
            }

            CombatBodyWoundPolicyDef policy = CombatBodyWoundPolicy.Resolve();
            int expiryTick = trionBinding.UpdateWoundDrain(pawn, hediff, CurrentGameTick(), ResolveIdleTimeoutTicks(policy));
            if (expiryTick <= 0)
            {
                CombatBodyWoundPresentationRegistry.NotifyWoundDrainExpired(pawn, hediff.loadID);
                return;
            }

            ScheduleNextExpiry(expiryTick);
            CombatBodyWoundPresentationRegistry.NotifyWoundAdded(pawn, hediff);
        }

        /// <summary>
        /// 响应伤口移除。
        /// </summary>
        internal void NotifyWoundRemoved(Pawn pawn, Hediff hediff)
        {
            trionBinding.RemoveWoundDrain(pawn, hediff);
            if (hediff != null)
            {
                CombatBodyWoundPresentationRegistry.NotifyWoundRemoved(pawn, hediff);
            }
        }

        /// <summary>
        /// 清理当前派生出的伤口运行时。
        /// </summary>
        internal void ClearActiveRuntime(Pawn pawn)
        {
            trionBinding.ClearAll(pawn);
            CombatBodyWoundPresentationRegistry.ClearAll(pawn);
            nextCalibrationTick = 0;
        }

        /// <summary>
        /// 读档后按当前相位恢复伤口运行时。
        /// </summary>
        internal void RestoreAfterLoad(Pawn pawn)
        {
            if (!CombatBodyWoundPolicy.IsCombatBodyWoundRuntimeApplicable(pawn))
            {
                ClearActiveRuntime(pawn);
                return;
            }

            CombatBodyWoundPolicyDef policy = CombatBodyWoundPolicy.Resolve();
            int nextExpiryTick = trionBinding.RestoreAfterLoad(pawn, CurrentGameTick());
            CombatBodyWoundPresentationRegistry.RebuildFromActiveDrains(
                pawn,
                trionBinding.GetActiveHediffLoadIds());
            nextCalibrationTick = 0;
            if (nextExpiryTick > 0)
            {
                ScheduleNextExpiry(nextExpiryTick);
                return;
            }

            ScheduleNextCalibration(policy);
        }

        /// <summary>
        /// 推进低频伤口流失过期检查。
        /// 只注销长时间没有继续变化的派生 drain，不重新注册旧伤口。
        /// </summary>
        internal void Tick(Pawn pawn)
        {
            CombatBodyWoundPolicyDef policy = CombatBodyWoundPolicy.Resolve();
            if (!HasEnabledDrain(policy))
            {
                return;
            }

            if (!CombatBodyWoundPolicy.IsCombatBodyWoundRuntimeApplicable(pawn))
            {
                return;
            }

            int ticksGame = CurrentGameTick();
            if (ticksGame >= nextCalibrationTick)
            {
                expiredDrainIds.Clear();
                int nextExpiryTick = trionBinding.ExpireIdleDrains(pawn, ticksGame, expiredDrainIds);
                for (int index = 0; index < expiredDrainIds.Count; index++)
                {
                    CombatBodyWoundPresentationRegistry.NotifyWoundDrainExpired(pawn, expiredDrainIds[index]);
                }

                expiredDrainIds.Clear();
                if (nextExpiryTick > 0)
                {
                    ScheduleNextExpiry(nextExpiryTick);
                }
                else
                {
                    ScheduleNextCalibration(policy);
                }
            }

            CombatBodyWoundPresentationRegistry.Tick(pawn);
        }

        /// <summary>
        /// 安排下一次低频校准 tick。
        /// </summary>
        private void ScheduleNextCalibration(CombatBodyWoundPolicyDef policy)
        {
            int interval = Math.Max(1, policy != null ? policy.calibrationIntervalTicks : 600);
            nextCalibrationTick = CurrentGameTick() + interval;
        }

        /// <summary>
        /// 把下一次低频检查压到最早的伤口流失到期点。
        /// </summary>
        private void ScheduleNextExpiry(int expiryTick)
        {
            if (expiryTick <= 0)
            {
                return;
            }

            if (nextCalibrationTick <= 0 || expiryTick < nextCalibrationTick)
            {
                nextCalibrationTick = expiryTick;
            }
        }

        /// <summary>
        /// 读取当前游戏 tick。
        /// 在非游戏上下文下返回 0，便于测试环境安全构建。
        /// </summary>
        private static int CurrentGameTick()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }

        /// <summary>
        /// 解析伤口流失空闲超时 tick。
        /// </summary>
        private static int ResolveIdleTimeoutTicks(CombatBodyWoundPolicyDef policy)
        {
            return Math.Max(1, policy != null ? policy.trionDrainIdleTimeoutTicks : 600);
        }

        /// <summary>
        /// 判断策略是否启用了至少一种正数伤口流失口径。
        /// </summary>
        private static bool HasEnabledDrain(CombatBodyWoundPolicyDef policy)
        {
            if (policy == null || !policy.trionDrainEnabled)
            {
                return false;
            }

            if (policy.trionDrainMetric == CombatBodyWoundTrionDrainMetric.Severity)
            {
                return policy.trionDrainPerSeverityPerSecond > 0f;
            }

            return policy.trionDrainPerRawBleedRatePerSecond > 0f;
        }
    }
}
