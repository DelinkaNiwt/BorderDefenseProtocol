using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using Verse;

namespace BDP.Core.Verbs
{
    /// <summary>
    /// 远程宿主发射游标。
    /// 它只保存当前已绑定发射计划的窗口推进与投射物消费位置。
    /// </summary>
    internal sealed class RangedVerbEmissionCursor
    {
        /// <summary>
        /// 当前已绑定、等待由 Verb 宿主消费的正式发射计划。
        /// </summary>
        private RangedVerbEmissionPlan pendingVerbEmissionPlan;

        /// <summary>
        /// 当前宿主消费的发射窗口引用。
        /// 宿主只推进游标，不复制正式计划对象。
        /// </summary>
        private IReadOnlyList<RangedVerbEmissionWindowPlan> pendingEmissionWindows;

        /// <summary>
        /// 当前正式发射计划已经消费到第几个发射窗口。
        /// </summary>
        private int pendingWindowIndex;

        /// <summary>
        /// 当前窗口已经消费到第几个投射物初始化计划。
        /// </summary>
        private int pendingWindowProjectilePlanIndex;

        /// <summary>
        /// 当前正式发射计划已经成功落地了多少条投射计划。
        /// 这个计数只服务宿主消费摘要日志，不参与业务决策。
        /// </summary>
        private int pendingEmissionConsumedCount;

        /// <summary>
        /// 当前已绑定的发射计划。
        /// </summary>
        internal RangedVerbEmissionPlan PendingVerbEmissionPlan
        {
            get { return pendingVerbEmissionPlan; }
        }

        /// <summary>
        /// 当前已绑定的窗口计划副本。
        /// </summary>
        internal IReadOnlyList<RangedVerbEmissionWindowPlan> PendingEmissionWindows
        {
            get { return pendingEmissionWindows; }
        }

        /// <summary>
        /// 当前窗口索引。
        /// </summary>
        internal int PendingWindowIndex
        {
            get { return pendingWindowIndex; }
        }

        /// <summary>
        /// 当前窗口内投射物索引。
        /// </summary>
        internal int PendingWindowProjectilePlanIndex
        {
            get { return pendingWindowProjectilePlanIndex; }
        }

        /// <summary>
        /// 当前已经消费的投射计划数量。
        /// </summary>
        internal int PendingEmissionConsumedCount
        {
            get { return pendingEmissionConsumedCount; }
        }

        /// <summary>
        /// 序列化游标本身的最小状态。
        /// </summary>
        internal void ExposeData()
        {
            Scribe_Values.Look(ref pendingWindowIndex, "pendingWindowIndex", 0);
            Scribe_Values.Look(ref pendingWindowProjectilePlanIndex, "pendingWindowProjectilePlanIndex", 0);
            Scribe_Values.Look(ref pendingEmissionConsumedCount, "pendingEmissionConsumedCount", 0);
        }

        /// <summary>
        /// 绑定当前动作步正式裁定好的宿主发射计划。
        /// 发射桥只消费正式计划对象本身，不再复制它们。
        /// </summary>
        internal void BindVerbEmissionPlan(RangedVerbEmissionPlan emissionPlan)
        {
            pendingWindowIndex = 0;
            pendingWindowProjectilePlanIndex = 0;
            pendingEmissionConsumedCount = 0;
            if (emissionPlan == null)
            {
                pendingVerbEmissionPlan = null;
                pendingEmissionWindows = null;
                return;
            }

            pendingVerbEmissionPlan = emissionPlan;
            pendingEmissionWindows = emissionPlan.Windows;
        }

        /// <summary>
        /// 当前是否已绑定一份仍可消费的正式宿主发射计划。
        /// </summary>
        internal bool HasPendingEmissionPlan()
        {
            return pendingVerbEmissionPlan != null
                && pendingEmissionWindows != null
                && pendingEmissionWindows.Count > 0
                && pendingWindowIndex >= 0
                && pendingWindowIndex < pendingEmissionWindows.Count;
        }

        /// <summary>
        /// 尝试读取当前要消费的发射窗口。
        /// </summary>
        internal bool TryGetCurrentWindow(out RangedVerbEmissionWindowPlan window)
        {
            window = null;
            if (pendingEmissionWindows == null
                || pendingWindowIndex < 0
                || pendingWindowIndex >= pendingEmissionWindows.Count)
            {
                return false;
            }

            window = pendingEmissionWindows[pendingWindowIndex];
            if (window == null || window.ProjectilePlans == null || window.ProjectilePlans.Count == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 绑定当前窗口中的下一条待发射投射计划。
        /// </summary>
        internal bool TryBindNextWindowPlan(out ProjectileInitPlan plan)
        {
            plan = null;
            if (!TryGetCurrentWindow(out RangedVerbEmissionWindowPlan window)
                || pendingWindowProjectilePlanIndex < 0
                || pendingWindowProjectilePlanIndex >= window.ProjectilePlans.Count)
            {
                return false;
            }

            plan = window.ProjectilePlans[pendingWindowProjectilePlanIndex++];
            return plan != null;
        }

        /// <summary>
        /// 当前窗口消费完成后推进到下一个窗口。
        /// </summary>
        internal void AdvanceAfterCurrentWindow(int emittedCount)
        {
            pendingEmissionConsumedCount += emittedCount;
            pendingWindowIndex++;
            pendingWindowProjectilePlanIndex = 0;
        }

        /// <summary>
        /// 读取当前已绑定计划中还剩多少个宿主发射窗口。
        /// </summary>
        internal int ResolveRemainingWindowCount()
        {
            if (pendingEmissionWindows == null || pendingWindowIndex >= pendingEmissionWindows.Count)
            {
                return 0;
            }

            return pendingEmissionWindows.Count - pendingWindowIndex;
        }

        /// <summary>
        /// 读取当前已绑定计划中还剩多少条投射计划。
        /// 这只用于日志和 battle log，不驱动 burst 节奏。
        /// </summary>
        internal int ResolveRemainingProjectileCount()
        {
            if (pendingEmissionWindows == null || pendingWindowIndex >= pendingEmissionWindows.Count)
            {
                return 0;
            }

            int remaining = 0;
            for (int i = pendingWindowIndex; i < pendingEmissionWindows.Count; i++)
            {
                RangedVerbEmissionWindowPlan window = pendingEmissionWindows[i];
                if (window?.ProjectilePlans == null)
                {
                    continue;
                }

                remaining += i == pendingWindowIndex
                    ? window.ProjectilePlans.Count - pendingWindowProjectilePlanIndex
                    : window.ProjectilePlans.Count;
            }

            return remaining;
        }

        /// <summary>
        /// 把刚重建出来的发射计划快进到已持久化的 burst 游标。
        /// </summary>
        internal bool TryRestorePreparedEmissionCursor(
            int savedWindowIndex,
            int savedWindowProjectilePlanIndex,
            int savedEmissionConsumedCount)
        {
            if (!HasPendingEmissionPlan())
            {
                return false;
            }

            if (savedWindowIndex <= 0
                && savedWindowProjectilePlanIndex <= 0
                && savedEmissionConsumedCount <= 0)
            {
                return true;
            }

            if (pendingEmissionWindows == null
                || savedWindowIndex < 0
                || savedWindowIndex >= pendingEmissionWindows.Count)
            {
                return false;
            }

            RangedVerbEmissionWindowPlan window = pendingEmissionWindows[savedWindowIndex];
            if (window?.ProjectilePlans == null
                || savedWindowProjectilePlanIndex < 0
                || savedWindowProjectilePlanIndex > window.ProjectilePlans.Count)
            {
                return false;
            }

            pendingWindowIndex = savedWindowIndex;
            pendingWindowProjectilePlanIndex = savedWindowProjectilePlanIndex;
            pendingEmissionConsumedCount = savedEmissionConsumedCount;
            return true;
        }

        /// <summary>
        /// 清空当前正式宿主发射计划绑定。
        /// </summary>
        internal void ClearPendingEmissionPlan()
        {
            pendingVerbEmissionPlan = null;
            pendingEmissionWindows = null;
            pendingWindowIndex = 0;
            pendingWindowProjectilePlanIndex = 0;
            pendingEmissionConsumedCount = 0;
        }

        /// <summary>
        /// 判断当前持久化下来的 burst 游标是否仍然处于最小合法区间。
        /// </summary>
        internal bool HasValidLoadedBurstCursor()
        {
            return pendingWindowIndex >= 0
                && pendingWindowProjectilePlanIndex >= 0
                && pendingEmissionConsumedCount >= 0;
        }

        /// <summary>
        /// 读取当前待消费窗口中下一条真正会被发出去的目标。
        /// </summary>
        internal bool TryGetFirstPendingLaunchTarget(out LocalTargetInfo target)
        {
            target = LocalTargetInfo.Invalid;
            if (pendingEmissionWindows == null
                || pendingWindowIndex < 0
                || pendingWindowIndex >= pendingEmissionWindows.Count)
            {
                return false;
            }

            for (int windowIndex = pendingWindowIndex; windowIndex < pendingEmissionWindows.Count; windowIndex++)
            {
                RangedVerbEmissionWindowPlan window = pendingEmissionWindows[windowIndex];
                if (window?.ProjectilePlans == null || window.ProjectilePlans.Count == 0)
                {
                    continue;
                }

                int startPlanIndex = windowIndex == pendingWindowIndex
                    ? pendingWindowProjectilePlanIndex
                    : 0;
                for (int planIndex = startPlanIndex; planIndex < window.ProjectilePlans.Count; planIndex++)
                {
                    ProjectileInitPlan plan = window.ProjectilePlans[planIndex];
                    if (plan == null || !plan.LaunchTarget.IsValid)
                    {
                        continue;
                    }

                    target = plan.LaunchTarget;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
    }
}
