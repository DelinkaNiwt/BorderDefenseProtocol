using System;
using System.Collections.Generic;
using System.Linq;
using BDP.Support.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 切换阶段与绑定拓扑服务。
    /// 这里承接主副手同步、切换上下文构造、到期结算与槽位正式激活/停用。
    /// </summary>
    internal static class TriggerSwitchTransitionService
    {
        /// <summary>
        /// 处理单侧启用请求的阶段推进。
        /// 成员职责：根据当前激活状态决定立即提交、单侧切换或主副手同步切换。
        /// </summary>
        public static bool RequestActivate(
            TriggerSlotState nextSlot,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlotRaw,
            IReadOnlyList<TriggerSlotState> activationBlockers,
            Func<TriggerSlotState, IReadOnlyList<TriggerSlotState>> resolveActivationBlockers,
            Func<TriggerSlotState, bool> isPendingTargetValid,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<Thing, int> resolveChipActivationDelayTicks,
            Func<Thing, int> resolveChipDeactivationDelayTicks,
            int currentTick,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted,
            Action<TriggerSide, int, Thing> notifySlotDeactivated)
        {
            nextSlot = NormalizeDirectControlSlot(nextSlot, getSlot);
            if (nextSlot == null)
            {
                return false;
            }

            TriggerSlotState currentActive = getActiveSlot != null ? getActiveSlot(nextSlot.Side) : null;
            if (currentActive != null
                && NormalizeDirectControlSlot(currentActive, getSlot) == nextSlot)
            {
                return true;
            }

            IReadOnlyList<TriggerSlotState> blockers =
                activationBlockers ?? new List<TriggerSlotState>();
            if (blockers.Count > 0)
            {
                BeginActivationBlockerDeactivations(
                    blockers,
                    nextSlot,
                    getSlot,
                    getActiveSlot,
                    getActiveSlotRaw,
                    getSwitchContext,
                    setSwitchContext,
                    resolveChipActivationDelayTicks,
                    resolveChipDeactivationDelayTicks,
                    currentTick,
                    notifySlotActivationCommitted,
                    notifySlotDeactivated);

                IReadOnlyList<TriggerSlotState> remainingBlockers =
                    resolveActivationBlockers != null
                        ? resolveActivationBlockers(nextSlot)
                        : blockers;
                if (remainingBlockers != null && remainingBlockers.Count > 0)
                {
                    EnsurePendingTargetContext(
                        nextSlot,
                        getSlot,
                        getSwitchContext,
                        setSwitchContext);
                    return true;
                }
            }

            return BeginTargetActivationDelayOrActivate(
                nextSlot,
                currentTick,
                getSlot,
                getSwitchContext,
                setSwitchContext,
                resolveChipActivationDelayTicks,
                notifySlotActivationCommitted);
        }

        /// <summary>
        /// 处理单侧停用请求的阶段推进。
        /// 成员职责：根据当前激活状态决定立即停用或进入停用延迟阶段。
        /// </summary>
        public static bool RequestDeactivate(
            TriggerSide side,
            Func<TriggerSide, TriggerSlotState> getActiveSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlotRaw,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<Thing, int> resolveChipActivationDelayTicks,
            Func<Thing, int> resolveChipDeactivationDelayTicks,
            int currentTick,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted,
            Action<TriggerSide, int, Thing> notifySlotDeactivated)
        {
            TriggerSlotState activeSlot = NormalizeDirectControlSlot(getActiveSlot != null ? getActiveSlot(side) : null, getSlot);
            if (ShouldUseSynchronizedHandTransition(activeSlot, getSlot, getActiveSlotRaw))
            {
                return RequestDeactivateWithSynchronizedHands(
                    activeSlot,
                    getSwitchContext,
                    setSwitchContext,
                    getSlot,
                    getActiveSlotRaw,
                    resolveChipActivationDelayTicks,
                    resolveChipDeactivationDelayTicks,
                    currentTick,
                    notifySlotActivationCommitted,
                    notifySlotDeactivated);
            }

            if (GetActiveSwitchContext(side, currentTick, getSwitchContext) != null)
            {
                BdpDiagnostics.Throttled("trigger.deactivate.reject.switching." + side, "Deactivate rejected: side is still switching.", 30);
                return false;
            }

            TriggerSlotState currentActive = getActiveSlot != null ? getActiveSlot(side) : null;
            if (currentActive == null)
            {
                return false;
            }

            SwitchContext switchContext = BuildDeactivatingContext(
                resolveChipDeactivationDelayTicks != null ? resolveChipDeactivationDelayTicks(currentActive.LoadedChip) : 0,
                currentTick,
                -1,
                currentActive.Index,
                null);
            if (switchContext == null)
            {
                DeactivateSlot(currentActive);
                notifySlotDeactivated?.Invoke(side, currentActive.Index, currentActive.LoadedChip);
                return true;
            }

            setSwitchContext?.Invoke(side, switchContext);
            return true;
        }

        /// <summary>
        /// 判断当前侧是否属于主/副手区。
        /// 成员职责：为成对占槽与同步切换提供最小侧别判定。
        /// </summary>
        public static bool IsHandSide(TriggerSide side)
        {
            return side == TriggerSide.Main || side == TriggerSide.Sub;
        }

        /// <summary>
        /// 读取手部双侧中的对侧。
        /// 成员职责：只返回主副手互换结果，特殊侧保持原值。
        /// </summary>
        public static TriggerSide GetOppositeHandSide(TriggerSide side)
        {
            if (side == TriggerSide.Main)
            {
                return TriggerSide.Sub;
            }

            if (side == TriggerSide.Sub)
            {
                return TriggerSide.Main;
            }

            return side;
        }

        /// <summary>
        /// 读取对侧同索引槽位。
        /// 成员职责：为成对占槽与同步切换提供对侧索引映射。
        /// </summary>
        public static TriggerSlotState GetOppositeIndexedSlot(
            TriggerSide side,
            int index,
            Func<TriggerSide, int, TriggerSlotState> getSlot)
        {
            if (!IsHandSide(side) || getSlot == null)
            {
                return null;
            }

            return getSlot(GetOppositeHandSide(side), index);
        }

        /// <summary>
        /// 把镜像副本槽位归一到绑定主槽位。
        /// 成员职责：统一外部直控入口，只让绑定根槽承接显式操作。
        /// </summary>
        public static TriggerSlotState NormalizeDirectControlSlot(
            TriggerSlotState slot,
            Func<TriggerSide, int, TriggerSlotState> getSlot)
        {
            if (slot == null || !slot.HasBindingPartner || !slot.IsBindingMirror || getSlot == null)
            {
                return slot;
            }

            return getSlot(slot.BindingRootSide, slot.BindingRootIndex);
        }

        /// <summary>
        /// 读取当前槽位绑定的对侧槽位。
        /// 成员职责：只解析绑定关系，不做任何状态修改。
        /// </summary>
        public static TriggerSlotState GetBindingPartnerSlot(
            TriggerSlotState slot,
            Func<TriggerSide, int, TriggerSlotState> getSlot)
        {
            if (slot == null || !slot.HasBindingPartner || slot.BindingPartnerIndex < 0 || getSlot == null)
            {
                return null;
            }

            return getSlot(slot.BindingPartnerSide, slot.BindingPartnerIndex);
        }

        /// <summary>
        /// 判断当前操作是否应走主副手同步切换。
        /// 成员职责：识别成对占槽目标或遗留的成对激活态。
        /// </summary>
        public static bool ShouldUseSynchronizedHandTransition(
            TriggerSlotState selectedSlot,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlotRaw)
        {
            TriggerSlotState normalizedSelected = NormalizeDirectControlSlot(selectedSlot, getSlot);
            if (normalizedSelected != null && IsPairedOccupancySlot(normalizedSelected))
            {
                return true;
            }

            TriggerSlotState normalizedMain = NormalizeDirectControlSlot(getActiveSlotRaw != null ? getActiveSlotRaw(TriggerSide.Main) : null, getSlot);
            TriggerSlotState normalizedSub = NormalizeDirectControlSlot(getActiveSlotRaw != null ? getActiveSlotRaw(TriggerSide.Sub) : null, getSlot);
            return IsPairedOccupancySlot(normalizedMain) || IsPairedOccupancySlot(normalizedSub);
        }

        /// <summary>
        /// 构造停用延迟上下文。
        /// 成员职责：用停用延迟生成 Deactivating 阶段真值。
        /// </summary>
        public static SwitchContext BuildDeactivatingContext(
            int delayTicks,
            int currentTick,
            int targetSlotIndex,
            int deactivatingSlotIndex,
            string targetChipThingId)
        {
            int delayDuration = Mathf.Max(0, delayTicks);
            if (delayDuration <= 0)
            {
                return null;
            }

            return new SwitchContext
            {
                phase = SwitchPhase.Deactivating,
                phaseEndTick = currentTick + delayDuration,
                targetSlotIndex = targetSlotIndex,
                targetChipThingId = targetChipThingId,
                deactivatingSlotIndex = deactivatingSlotIndex,
                activationDelayDuration = 0,
                deactivationDelayDuration = delayDuration
            };
        }

        /// <summary>
        /// 构造启用延迟上下文。
        /// 成员职责：用启用延迟生成 Activating 阶段真值。
        /// </summary>
        public static SwitchContext BuildActivatingContext(
            int delayTicks,
            int currentTick,
            int targetSlotIndex,
            string targetChipThingId)
        {
            int delayDuration = Mathf.Max(0, delayTicks);
            if (delayDuration <= 0)
            {
                return null;
            }

            return new SwitchContext
            {
                phase = SwitchPhase.Activating,
                phaseEndTick = currentTick + delayDuration,
                targetSlotIndex = targetSlotIndex,
                targetChipThingId = targetChipThingId,
                deactivatingSlotIndex = -1,
                activationDelayDuration = delayDuration,
                deactivationDelayDuration = 0
            };
        }

        /// <summary>
        /// 构造等待冲突解除上下文。
        /// 等待阶段没有可预先计算的结束刻，只保存目标槽位与物品身份。
        /// </summary>
        public static SwitchContext BuildWaitingForConflictsContext(
            int targetSlotIndex,
            string targetChipThingId)
        {
            return new SwitchContext
            {
                phase = SwitchPhase.WaitingForConflicts,
                phaseEndTick = 0,
                targetSlotIndex = targetSlotIndex,
                targetChipThingId = targetChipThingId,
                deactivatingSlotIndex = -1,
                activationDelayDuration = 0,
                deactivationDelayDuration = 0
            };
        }

        /// <summary>
        /// 读取当前仍处于有效展示期的切换上下文。
        /// 成员职责：屏蔽已过期但尚未结算的上下文。
        /// </summary>
        public static SwitchContext GetActiveSwitchContext(
            TriggerSide side,
            int currentTick,
            Func<TriggerSide, SwitchContext> getSwitchContext)
        {
            SwitchContext switchContext = getSwitchContext != null ? getSwitchContext(side) : null;
            return IsPresentationPhaseActive(switchContext, currentTick) ? switchContext : null;
        }

        /// <summary>
        /// 结算所有已到期的切换上下文。
        /// 成员职责：由 owner 主动触发懒结算，不依赖持续 Tick 推进。
        /// </summary>
        public static void ResolveDueSwitchTransitions(
            int currentTick,
            Func<Thing, int> resolveChipActivationDelayTicks,
            Func<Thing, int> resolveChipDeactivationDelayTicks,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlotRaw,
            Func<TriggerSlotState, IReadOnlyList<TriggerSlotState>> resolveActivationBlockers,
            Func<TriggerSlotState, bool> isPendingTargetValid,
            Action<TriggerSlotState> deactivateBoundSlotImmediate,
            Action<int, int> activateSynchronizedTargets,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted,
            Action<TriggerSide, int, Thing> notifySlotDeactivated)
        {
            ResolveDueSwitchTransition(TriggerSide.Main, currentTick, resolveChipActivationDelayTicks, resolveChipDeactivationDelayTicks, getSwitchContext, setSwitchContext, getSlot, getActiveSlotRaw, resolveActivationBlockers, isPendingTargetValid, deactivateBoundSlotImmediate, activateSynchronizedTargets, notifySlotActivationCommitted, notifySlotDeactivated);
            ResolveDueSwitchTransition(TriggerSide.Sub, currentTick, resolveChipActivationDelayTicks, resolveChipDeactivationDelayTicks, getSwitchContext, setSwitchContext, getSlot, getActiveSlotRaw, resolveActivationBlockers, isPendingTargetValid, deactivateBoundSlotImmediate, activateSynchronizedTargets, notifySlotActivationCommitted, notifySlotDeactivated);
            ResolveDueSwitchTransition(TriggerSide.Special, currentTick, resolveChipActivationDelayTicks, resolveChipDeactivationDelayTicks, getSwitchContext, setSwitchContext, getSlot, getActiveSlotRaw, resolveActivationBlockers, isPendingTargetValid, deactivateBoundSlotImmediate, activateSynchronizedTargets, notifySlotActivationCommitted, notifySlotDeactivated);
        }

        /// <summary>
        /// 立刻关闭一组绑定槽位。
        /// 成员职责：同步清空绑定双侧的展示上下文，并由根槽提交一次逻辑芯片停用事件。
        /// </summary>
        public static void DeactivateBoundSlotImmediate(
            TriggerSlotState rootSlot,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Action<TriggerSide, int, Thing> notifySlotDeactivated)
        {
            rootSlot = NormalizeDirectControlSlot(rootSlot, getSlot);
            if (rootSlot == null)
            {
                return;
            }

            TriggerSlotState mirrorSlot = GetBindingPartnerSlot(rootSlot, getSlot);
            if (mirrorSlot != null)
            {
                setSwitchContext?.Invoke(rootSlot.Side, null);
                setSwitchContext?.Invoke(mirrorSlot.Side, null);
            }

            bool wasActive = rootSlot.IsActive || (mirrorSlot != null && mirrorSlot.IsActive);
            if (rootSlot.IsActive)
            {
                DeactivateSlot(rootSlot);
            }

            if (mirrorSlot != null && mirrorSlot.IsActive)
            {
                DeactivateSlot(mirrorSlot);
            }

            if (wasActive)
            {
                notifySlotDeactivated?.Invoke(rootSlot.Side, rootSlot.Index, rootSlot.LoadedChip);
            }
        }

        /// <summary>
        /// 判断两枚逻辑芯片是否占用同一启用控制范围。
        /// 成对主副槽会占据整个手部范围，普通单槽只占据自身侧别。
        /// </summary>
        public static bool SharesActivationControlScope(
            TriggerSlotState left,
            TriggerSlotState right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            bool leftPaired = IsPairedOccupancySlot(left);
            bool rightPaired = IsPairedOccupancySlot(right);
            if ((leftPaired || rightPaired)
                && IsHandSide(left.Side)
                && IsHandSide(right.Side))
            {
                return true;
            }

            // 特殊槽各槽位独立控制，不因同侧就共享激活控制范围。
            if (left.Side == TriggerSide.Special && right.Side == TriggerSide.Special)
            {
                return false;
            }

            return left.Side == right.Side;
        }

        /// <summary>
        /// 判断新请求是否就是当前已经等待或开启中的同一枚目标芯片。
        /// </summary>
        public static bool IsSamePendingTarget(
            TriggerSlotState targetSlot,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot)
        {
            targetSlot = NormalizeDirectControlSlot(targetSlot, getSlot);
            string targetThingId = targetSlot != null && targetSlot.LoadedChip != null
                ? targetSlot.LoadedChip.ThingID
                : null;
            if (string.IsNullOrEmpty(targetThingId))
            {
                return false;
            }

            foreach (TriggerSide side in AllSides())
            {
                SwitchContext context = getSwitchContext != null
                    ? getSwitchContext(side)
                    : null;
                if (context != null
                    && context.phase != SwitchPhase.Idle
                    && context.targetChipThingId == targetThingId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 取消与新目标同侧或互相冲突的旧待启用目标。
        /// 已经开始的旧芯片关闭过程会保留原结束刻，但不再携带旧目标。
        /// </summary>
        public static void CancelConflictingPendingTargets(
            TriggerSlotState newTarget,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSlotState, TriggerSlotState, bool> hasActivationExclusionConflict)
        {
            newTarget = NormalizeDirectControlSlot(newTarget, getSlot);
            if (newTarget == null)
            {
                return;
            }

            HashSet<string> handledTargetIds = new HashSet<string>();
            foreach (TriggerSide side in AllSides())
            {
                SwitchContext context = getSwitchContext != null
                    ? getSwitchContext(side)
                    : null;
                string pendingId = context != null ? context.targetChipThingId : null;
                if (string.IsNullOrEmpty(pendingId)
                    || pendingId == newTarget.LoadedChip?.ThingID
                    || !handledTargetIds.Add(pendingId))
                {
                    continue;
                }

                TriggerSlotState pendingTarget = ResolvePendingTarget(
                    side,
                    context,
                    getSwitchContext,
                    getSlot);
                bool shouldCancel = pendingTarget == null
                    || SharesActivationControlScope(newTarget, pendingTarget)
                    || (hasActivationExclusionConflict != null
                        && hasActivationExclusionConflict(newTarget, pendingTarget));
                if (!shouldCancel)
                {
                    continue;
                }

                foreach (TriggerSide affectedSide in AllSides())
                {
                    SwitchContext affectedContext = getSwitchContext != null
                        ? getSwitchContext(affectedSide)
                        : null;
                    if (affectedContext != null
                        && affectedContext.targetChipThingId == pendingId)
                    {
                        setSwitchContext?.Invoke(
                            affectedSide,
                            PreserveDeactivatingWithoutTarget(affectedContext));
                    }
                }
            }
        }

        /// <summary>
        /// 保留正在进行的关闭计时，同时删除它原本携带的待启用目标。
        /// </summary>
        private static SwitchContext PreserveDeactivatingWithoutTarget(
            SwitchContext context)
        {
            if (context == null
                || context.phase != SwitchPhase.Deactivating
                || context.deactivatingSlotIndex < 0)
            {
                return null;
            }

            return new SwitchContext
            {
                phase = context.phase,
                phaseEndTick = context.phaseEndTick,
                targetSlotIndex = -1,
                targetChipThingId = null,
                deactivatingSlotIndex = context.deactivatingSlotIndex,
                activationDelayDuration = context.activationDelayDuration,
                deactivationDelayDuration = context.deactivationDelayDuration
            };
        }

        /// <summary>
        /// 让初次扫描到的全部阻挡者在同一游戏刻进入各自正常关闭程序。
        /// </summary>
        private static void BeginActivationBlockerDeactivations(
            IReadOnlyList<TriggerSlotState> blockers,
            TriggerSlotState targetSlot,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlotRaw,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<Thing, int> resolveChipActivationDelayTicks,
            Func<Thing, int> resolveChipDeactivationDelayTicks,
            int currentTick,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted,
            Action<TriggerSide, int, Thing> notifySlotDeactivated)
        {
            foreach (TriggerSlotState blocker in blockers ?? new List<TriggerSlotState>())
            {
                TriggerSlotState normalizedBlocker =
                    NormalizeDirectControlSlot(blocker, getSlot);
                if (normalizedBlocker == null
                    || IsBlockerAlreadyWinding(
                        normalizedBlocker,
                        getSwitchContext,
                        getSlot))
                {
                    continue;
                }

                RequestDeactivate(
                    normalizedBlocker.Side,
                    getActiveSlot,
                    getActiveSlotRaw,
                    getSlot,
                    getSwitchContext,
                    setSwitchContext,
                    resolveChipActivationDelayTicks,
                    resolveChipDeactivationDelayTicks,
                    currentTick,
                    notifySlotActivationCommitted,
                    notifySlotDeactivated);
            }

            AttachTargetToWindingContexts(
                targetSlot,
                getSlot,
                getSwitchContext,
                setSwitchContext);
        }

        /// <summary>
        /// 确保目标在自身控制范围内拥有可存档的等待或关闭上下文。
        /// </summary>
        private static void EnsurePendingTargetContext(
            TriggerSlotState targetSlot,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext)
        {
            targetSlot = NormalizeDirectControlSlot(targetSlot, getSlot);
            if (targetSlot == null || targetSlot.LoadedChip == null)
            {
                return;
            }

            string targetThingId = targetSlot.LoadedChip.ThingID;
            foreach (TriggerSide side in GetTargetControlSides(targetSlot))
            {
                SwitchContext existing = getSwitchContext != null
                    ? getSwitchContext(side)
                    : null;
                if (existing != null
                    && existing.phase == SwitchPhase.Deactivating
                    && existing.targetChipThingId == targetThingId)
                {
                    continue;
                }

                setSwitchContext?.Invoke(
                    side,
                    BuildWaitingForConflictsContext(
                        GetTargetSlotIndexForSide(targetSlot, side, getSlot),
                        targetThingId));
            }
        }

        /// <summary>
        /// 把新目标附着到同一控制范围内已经开始的关闭上下文上。
        /// </summary>
        private static void AttachTargetToWindingContexts(
            TriggerSlotState targetSlot,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext)
        {
            targetSlot = NormalizeDirectControlSlot(targetSlot, getSlot);
            if (targetSlot == null || targetSlot.LoadedChip == null)
            {
                return;
            }

            foreach (TriggerSide side in AllSides())
            {
                SwitchContext context = getSwitchContext != null
                    ? getSwitchContext(side)
                    : null;
                if (context == null
                    || context.phase != SwitchPhase.Deactivating
                    || context.deactivatingSlotIndex < 0)
                {
                    continue;
                }

                TriggerSlotState blocker = NormalizeDirectControlSlot(
                    getSlot != null
                        ? getSlot(side, context.deactivatingSlotIndex)
                        : null,
                    getSlot);
                if (!SharesActivationControlScope(targetSlot, blocker))
                {
                    continue;
                }

                context.targetSlotIndex =
                    GetTargetSlotIndexForSide(targetSlot, side, getSlot);
                context.targetChipThingId = targetSlot.LoadedChip.ThingID;
                setSwitchContext?.Invoke(side, context);
            }
        }

        /// <summary>
        /// 判断指定逻辑阻挡者是否已经处于关闭阶段。
        /// </summary>
        private static bool IsBlockerAlreadyWinding(
            TriggerSlotState blocker,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot)
        {
            foreach (TriggerSide side in AllSides())
            {
                SwitchContext context = getSwitchContext != null
                    ? getSwitchContext(side)
                    : null;
                if (context == null
                    || context.phase != SwitchPhase.Deactivating
                    || context.deactivatingSlotIndex < 0)
                {
                    continue;
                }

                TriggerSlotState windingSlot = NormalizeDirectControlSlot(
                    getSlot != null
                        ? getSlot(side, context.deactivatingSlotIndex)
                        : null,
                    getSlot);
                if (windingSlot == blocker)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 请求关闭一组双侧同步激活。
        /// 成员职责：把主副双侧同时送入停用延迟，或在零延迟时立即关闭。
        /// </summary>
        private static bool RequestDeactivateWithSynchronizedHands(
            TriggerSlotState activeRootSlot,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlotRaw,
            Func<Thing, int> resolveChipActivationDelayTicks,
            Func<Thing, int> resolveChipDeactivationDelayTicks,
            int currentTick,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted,
            Action<TriggerSide, int, Thing> notifySlotDeactivated)
        {
            activeRootSlot = NormalizeDirectControlSlot(activeRootSlot, getSlot);
            if (activeRootSlot == null)
            {
                return false;
            }

            if (GetActiveSwitchContext(TriggerSide.Main, currentTick, getSwitchContext) != null
                || GetActiveSwitchContext(TriggerSide.Sub, currentTick, getSwitchContext) != null)
            {
                BdpDiagnostics.Throttled("trigger.deactivate.reject.sync_switch." + activeRootSlot.Side + "." + activeRootSlot.Index, "槽位关闭被拒绝：主副侧仍处于同步切换中", 30);
                return false;
            }

            TriggerSlotState activeMainRaw = getActiveSlotRaw != null ? getActiveSlotRaw(TriggerSide.Main) : null;
            TriggerSlotState activeSubRaw = getActiveSlotRaw != null ? getActiveSlotRaw(TriggerSide.Sub) : null;
            int oldMainIndex = activeMainRaw != null ? activeMainRaw.Index : -1;
            int oldSubIndex = activeSubRaw != null ? activeSubRaw.Index : -1;
            if (oldMainIndex < 0 && oldSubIndex < 0)
            {
                return false;
            }

            int synchronizedDeactivationDelayTicks = ResolveSynchronizedDeactivationDelayTicks(activeMainRaw, activeSubRaw, resolveChipDeactivationDelayTicks);
            SwitchContext mainContext = BuildDeactivatingContext(synchronizedDeactivationDelayTicks, currentTick, -1, oldMainIndex, null);
            SwitchContext subContext = BuildDeactivatingContext(synchronizedDeactivationDelayTicks, currentTick, -1, oldSubIndex, null);
            if (mainContext == null || subContext == null)
            {
                DeactivateBoundSlotImmediate(activeRootSlot, getSlot, setSwitchContext, notifySlotDeactivated);
                return true;
            }

            setSwitchContext?.Invoke(TriggerSide.Main, mainContext);
            setSwitchContext?.Invoke(TriggerSide.Sub, subContext);
            return true;
        }

        /// <summary>
        /// 解析一组同步切换共享的停用延迟。
        /// 成员职责：优先读取当前真实停用芯片的显式配置；若只有一侧存在旧激活，也沿用这一侧时长作为同步屏障。
        /// </summary>
        private static int ResolveSynchronizedDeactivationDelayTicks(
            TriggerSlotState activeMainRaw,
            TriggerSlotState activeSubRaw,
            Func<Thing, int> resolveChipDeactivationDelayTicks)
        {
            if (resolveChipDeactivationDelayTicks == null)
            {
                return 0;
            }

            if (activeMainRaw != null && activeMainRaw.LoadedChip != null)
            {
                return Mathf.Max(0, resolveChipDeactivationDelayTicks(activeMainRaw.LoadedChip));
            }

            if (activeSubRaw != null && activeSubRaw.LoadedChip != null)
            {
                return Mathf.Max(0, resolveChipDeactivationDelayTicks(activeSubRaw.LoadedChip));
            }

            return 0;
        }

        /// <summary>
        /// 解析一组同步切换共享的启用延迟。
        /// 成员职责：优先读取真实目标芯片的显式配置；若只有一侧存在目标，也沿用这一侧时长作为同步屏障。
        /// </summary>
        private static int ResolveSynchronizedActivationDelayTicks(
            int targetMainIndex,
            int targetSubIndex,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<Thing, int> resolveChipActivationDelayTicks)
        {
            if (resolveChipActivationDelayTicks == null || getSlot == null)
            {
                return 0;
            }

            TriggerSlotState mainTarget = targetMainIndex >= 0 ? getSlot(TriggerSide.Main, targetMainIndex) : null;
            if (mainTarget != null && mainTarget.LoadedChip != null)
            {
                return Mathf.Max(0, resolveChipActivationDelayTicks(mainTarget.LoadedChip));
            }

            TriggerSlotState subTarget = targetSubIndex >= 0 ? getSlot(TriggerSide.Sub, targetSubIndex) : null;
            if (subTarget != null && subTarget.LoadedChip != null)
            {
                return Mathf.Max(0, resolveChipActivationDelayTicks(subTarget.LoadedChip));
            }

            return 0;
        }

        /// <summary>
        /// 开始目标自己的正常开启阶段；零延迟时直接提交正式启用。
        /// </summary>
        private static bool BeginTargetActivationDelayOrActivate(
            TriggerSlotState targetSlot,
            int currentTick,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<Thing, int> resolveChipActivationDelayTicks,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted)
        {
            targetSlot = NormalizeDirectControlSlot(targetSlot, getSlot);
            if (targetSlot == null || targetSlot.LoadedChip == null || targetSlot.IsDisabled)
            {
                return false;
            }

            string targetThingId = targetSlot.LoadedChip.ThingID;
            ClearPendingTargetContexts(
                targetThingId,
                getSwitchContext,
                setSwitchContext);

            if (IsPairedOccupancySlot(targetSlot))
            {
                int targetMainIndex =
                    GetTargetSlotIndexForSide(targetSlot, TriggerSide.Main, getSlot);
                int targetSubIndex =
                    GetTargetSlotIndexForSide(targetSlot, TriggerSide.Sub, getSlot);
                int activationDelayTicks = ResolveSynchronizedActivationDelayTicks(
                    targetMainIndex,
                    targetSubIndex,
                    getSlot,
                    resolveChipActivationDelayTicks);
                SwitchContext mainContext = BuildActivatingContext(
                    activationDelayTicks,
                    currentTick,
                    targetMainIndex,
                    targetThingId);
                SwitchContext subContext = BuildActivatingContext(
                    activationDelayTicks,
                    currentTick,
                    targetSubIndex,
                    targetThingId);
                if (mainContext == null || subContext == null)
                {
                    ActivateSynchronizedTargets(
                        targetMainIndex,
                        targetSubIndex,
                        getSlot,
                        notifySlotActivationCommitted);
                    return true;
                }

                setSwitchContext?.Invoke(TriggerSide.Main, mainContext);
                setSwitchContext?.Invoke(TriggerSide.Sub, subContext);
                return true;
            }

            SwitchContext context = BuildActivatingContext(
                resolveChipActivationDelayTicks != null
                    ? resolveChipActivationDelayTicks(targetSlot.LoadedChip)
                    : 0,
                currentTick,
                targetSlot.Index,
                targetThingId);
            if (context == null)
            {
                if (!ActivateSlot(targetSlot))
                {
                    return false;
                }

                notifySlotActivationCommitted?.Invoke(
                    targetSlot.Side,
                    targetSlot.Index,
                    targetSlot.LoadedChip);
                return true;
            }

            setSwitchContext?.Invoke(targetSlot.Side, context);
            return true;
        }

        /// <summary>
        /// 判断上下文保存的目标身份与当前槽位真值是否仍然一致且可继续。
        /// </summary>
        private static bool IsPendingTargetValid(
            SwitchContext context,
            TriggerSlotState targetSlot,
            Func<TriggerSlotState, bool> isPendingTargetValid)
        {
            return context != null
                && targetSlot != null
                && targetSlot.LoadedChip != null
                && !string.IsNullOrEmpty(context.targetChipThingId)
                && targetSlot.LoadedChip.ThingID == context.targetChipThingId
                && (isPendingTargetValid == null || isPendingTargetValid(targetSlot));
        }

        /// <summary>
        /// 清理一个失效逻辑目标涉及的全部上下文，同时保留已经开始的关闭计时。
        /// </summary>
        private static void ClearInvalidPendingTarget(
            string targetChipThingId,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext)
        {
            if (string.IsNullOrEmpty(targetChipThingId))
            {
                return;
            }

            foreach (TriggerSide side in AllSides())
            {
                SwitchContext context = getSwitchContext != null
                    ? getSwitchContext(side)
                    : null;
                if (context != null
                    && context.targetChipThingId == targetChipThingId)
                {
                    setSwitchContext?.Invoke(
                        side,
                        PreserveDeactivatingWithoutTarget(context));
                }
            }
        }

        /// <summary>
        /// 清除同一逻辑目标的旧等待或开启上下文。
        /// </summary>
        private static void ClearPendingTargetContexts(
            string targetChipThingId,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext)
        {
            if (string.IsNullOrEmpty(targetChipThingId))
            {
                return;
            }

            foreach (TriggerSide side in AllSides())
            {
                SwitchContext context = getSwitchContext != null
                    ? getSwitchContext(side)
                    : null;
                if (context != null
                    && context.targetChipThingId == targetChipThingId)
                {
                    setSwitchContext?.Invoke(side, null);
                }
            }
        }

        /// <summary>
        /// 从单侧上下文或同一逻辑目标的配对上下文中解析真实目标槽位。
        /// </summary>
        private static TriggerSlotState ResolvePendingTarget(
            TriggerSide contextSide,
            SwitchContext context,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot)
        {
            if (context == null || string.IsNullOrEmpty(context.targetChipThingId))
            {
                return null;
            }

            if (context.targetSlotIndex >= 0)
            {
                TriggerSlotState directTarget = NormalizeDirectControlSlot(
                    getSlot != null
                        ? getSlot(contextSide, context.targetSlotIndex)
                        : null,
                    getSlot);
                if (directTarget != null
                    && directTarget.LoadedChip != null
                    && directTarget.LoadedChip.ThingID == context.targetChipThingId)
                {
                    return directTarget;
                }
            }

            foreach (TriggerSide side in AllSides())
            {
                SwitchContext sibling = getSwitchContext != null
                    ? getSwitchContext(side)
                    : null;
                if (sibling == null
                    || sibling.targetChipThingId != context.targetChipThingId
                    || sibling.targetSlotIndex < 0)
                {
                    continue;
                }

                TriggerSlotState siblingTarget = NormalizeDirectControlSlot(
                    getSlot != null ? getSlot(side, sibling.targetSlotIndex) : null,
                    getSlot);
                if (siblingTarget != null
                    && siblingTarget.LoadedChip != null
                    && siblingTarget.LoadedChip.ThingID == context.targetChipThingId)
                {
                    return siblingTarget;
                }
            }

            return null;
        }

        /// <summary>
        /// 读取目标占据的控制侧集合。
        /// </summary>
        private static IEnumerable<TriggerSide> GetTargetControlSides(
            TriggerSlotState targetSlot)
        {
            if (IsPairedOccupancySlot(targetSlot))
            {
                yield return TriggerSide.Main;
                yield return TriggerSide.Sub;
                yield break;
            }

            if (targetSlot != null)
            {
                yield return targetSlot.Side;
            }
        }

        /// <summary>
        /// 解析目标在指定控制侧对应的槽位索引。
        /// </summary>
        private static int GetTargetSlotIndexForSide(
            TriggerSlotState targetSlot,
            TriggerSide side,
            Func<TriggerSide, int, TriggerSlotState> getSlot)
        {
            targetSlot = NormalizeDirectControlSlot(targetSlot, getSlot);
            if (targetSlot == null)
            {
                return -1;
            }

            if (targetSlot.Side == side)
            {
                return targetSlot.Index;
            }

            TriggerSlotState partner = GetBindingPartnerSlot(targetSlot, getSlot);
            return partner != null && partner.Side == side ? partner.Index : -1;
        }

        /// <summary>
        /// 固定枚举三种正式侧别。
        /// </summary>
        private static IEnumerable<TriggerSide> AllSides()
        {
            yield return TriggerSide.Main;
            yield return TriggerSide.Sub;
            yield return TriggerSide.Special;
        }

        /// <summary>
        /// 判断一个展示上下文是否仍处于有效期。
        /// 成员职责：封装 phaseEndTick 比较规则。
        /// </summary>
        private static bool IsPresentationPhaseActive(SwitchContext context, int currentTick)
        {
            return context != null
                && (context.phase == SwitchPhase.WaitingForConflicts
                    || currentTick < context.phaseEndTick);
        }

        /// <summary>
        /// 判断槽位是否属于成对占槽结构。
        /// 成员职责：识别主副槽中的绑定根槽或镜像槽。
        /// </summary>
        private static bool IsPairedOccupancySlot(TriggerSlotState slot)
        {
            return slot != null
                && slot.HasBindingPartner
                && IsHandSide(slot.Side)
                && IsHandSide(slot.BindingPartnerSide);
        }

        /// <summary>
        /// 结算某一侧已到期的切换上下文。
        /// 成员职责：只在上下文过期后推进到下一阶段或清空。
        /// </summary>
        private static void ResolveDueSwitchTransition(
            TriggerSide side,
            int currentTick,
            Func<Thing, int> resolveChipActivationDelayTicks,
            Func<Thing, int> resolveChipDeactivationDelayTicks,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlotRaw,
            Func<TriggerSlotState, IReadOnlyList<TriggerSlotState>> resolveActivationBlockers,
            Func<TriggerSlotState, bool> isPendingTargetValid,
            Action<TriggerSlotState> deactivateBoundSlotImmediate,
            Action<int, int> activateSynchronizedTargets,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted,
            Action<TriggerSide, int, Thing> notifySlotDeactivated)
        {
            SwitchContext switchContext = getSwitchContext != null ? getSwitchContext(side) : null;
            if (switchContext != null
                && !string.IsNullOrEmpty(switchContext.targetChipThingId))
            {
                TriggerSlotState pendingTarget = ResolvePendingTarget(
                    side,
                    switchContext,
                    getSwitchContext,
                    getSlot);
                if (!IsPendingTargetValid(
                    switchContext,
                    pendingTarget,
                    isPendingTargetValid))
                {
                    ClearInvalidPendingTarget(
                        switchContext.targetChipThingId,
                        getSwitchContext,
                        setSwitchContext);
                    return;
                }
            }

            if (switchContext != null
                && switchContext.phase == SwitchPhase.WaitingForConflicts)
            {
                ResolveWaitingForConflicts(
                    side,
                    switchContext,
                    currentTick,
                    resolveChipActivationDelayTicks,
                    resolveChipDeactivationDelayTicks,
                    getSwitchContext,
                    setSwitchContext,
                    getSlot,
                    getActiveSlotRaw,
                    resolveActivationBlockers,
                    isPendingTargetValid,
                    notifySlotActivationCommitted,
                    notifySlotDeactivated);
                return;
            }

            if (!IsPresentationPhaseActive(switchContext, currentTick))
            {
                if (switchContext != null && switchContext.phaseEndTick > 0 && currentTick >= switchContext.phaseEndTick)
                {
                    FinalizeSwitchPhase(side, switchContext, currentTick, resolveChipActivationDelayTicks, resolveChipDeactivationDelayTicks, getSwitchContext, setSwitchContext, getSlot, getActiveSlotRaw, deactivateBoundSlotImmediate, activateSynchronizedTargets, notifySlotActivationCommitted, notifySlotDeactivated);
                }
            }
        }

        /// <summary>
        /// 复查等待目标的当前真值；有新阻挡者就启动关闭，无阻挡者才进入正常开启。
        /// </summary>
        private static void ResolveWaitingForConflicts(
            TriggerSide side,
            SwitchContext switchContext,
            int currentTick,
            Func<Thing, int> resolveChipActivationDelayTicks,
            Func<Thing, int> resolveChipDeactivationDelayTicks,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlotRaw,
            Func<TriggerSlotState, IReadOnlyList<TriggerSlotState>> resolveActivationBlockers,
            Func<TriggerSlotState, bool> isPendingTargetValid,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted,
            Action<TriggerSide, int, Thing> notifySlotDeactivated)
        {
            TriggerSlotState targetSlot = ResolvePendingTarget(
                side,
                switchContext,
                getSwitchContext,
                getSlot);
            if (!IsPendingTargetValid(
                switchContext,
                targetSlot,
                isPendingTargetValid))
            {
                ClearInvalidPendingTarget(
                    switchContext != null
                        ? switchContext.targetChipThingId
                        : null,
                    getSwitchContext,
                    setSwitchContext);
                return;
            }

            IReadOnlyList<TriggerSlotState> blockers =
                resolveActivationBlockers != null
                    ? resolveActivationBlockers(targetSlot)
                    : new List<TriggerSlotState>();
            if (blockers != null && blockers.Count > 0)
            {
                BeginActivationBlockerDeactivations(
                    blockers,
                    targetSlot,
                    getSlot,
                    getActiveSlotRaw,
                    getActiveSlotRaw,
                    getSwitchContext,
                    setSwitchContext,
                    resolveChipActivationDelayTicks,
                    resolveChipDeactivationDelayTicks,
                    currentTick,
                    notifySlotActivationCommitted,
                    notifySlotDeactivated);

                blockers = resolveActivationBlockers != null
                    ? resolveActivationBlockers(targetSlot)
                    : blockers;
                if (blockers != null && blockers.Count > 0)
                {
                    EnsurePendingTargetContext(
                        targetSlot,
                        getSlot,
                        getSwitchContext,
                        setSwitchContext);
                    return;
                }
            }

            BeginTargetActivationDelayOrActivate(
                targetSlot,
                currentTick,
                getSlot,
                getSwitchContext,
                setSwitchContext,
                resolveChipActivationDelayTicks,
                notifySlotActivationCommitted);
        }

        /// <summary>
        /// 推进一个已到期的切换阶段。
        /// 成员职责：根据 phase 分发到停用延迟收尾、启用延迟收尾或直接清空。
        /// </summary>
        private static void FinalizeSwitchPhase(
            TriggerSide side,
            SwitchContext switchContext,
            int currentTick,
            Func<Thing, int> resolveChipActivationDelayTicks,
            Func<Thing, int> resolveChipDeactivationDelayTicks,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlotRaw,
            Action<TriggerSlotState> deactivateBoundSlotImmediate,
            Action<int, int> activateSynchronizedTargets,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted,
            Action<TriggerSide, int, Thing> notifySlotDeactivated)
        {
            if (switchContext == null)
            {
                return;
            }

            if (switchContext.phase == SwitchPhase.Deactivating)
            {
                FinalizeDeactivating(side, switchContext, currentTick, resolveChipActivationDelayTicks, resolveChipDeactivationDelayTicks, getSwitchContext, setSwitchContext, getSlot, getActiveSlotRaw, deactivateBoundSlotImmediate, activateSynchronizedTargets, notifySlotActivationCommitted, notifySlotDeactivated);
                return;
            }

            if (switchContext.phase == SwitchPhase.Activating)
            {
                FinalizeActivating(side, switchContext, getSwitchContext, setSwitchContext, getSlot, getActiveSlotRaw, activateSynchronizedTargets, notifySlotActivationCommitted);
                return;
            }

            setSwitchContext?.Invoke(side, null);
        }

        /// <summary>
        /// 结束停用延迟。
        /// 成员职责：先稳定切换上下文，再按目标槽位决定正式停用、进入启用延迟或直接提交激活。
        /// </summary>
        private static void FinalizeDeactivating(
            TriggerSide side,
            SwitchContext switchContext,
            int currentTick,
            Func<Thing, int> resolveChipActivationDelayTicks,
            Func<Thing, int> resolveChipDeactivationDelayTicks,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlotRaw,
            Action<TriggerSlotState> deactivateBoundSlotImmediate,
            Action<int, int> activateSynchronizedTargets,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted,
            Action<TriggerSide, int, Thing> notifySlotDeactivated)
        {
            TriggerSlotState oldSlot = NormalizeDirectControlSlot(getSlot != null ? getSlot(side, switchContext.deactivatingSlotIndex) : null, getSlot);
            if (ShouldUseSynchronizedHandTransition(oldSlot, getSlot, getActiveSlotRaw))
            {
                FinalizeSynchronizedDeactivating(currentTick, resolveChipActivationDelayTicks, getSwitchContext, setSwitchContext, getSlot, getActiveSlotRaw, deactivateBoundSlotImmediate, activateSynchronizedTargets, notifySlotActivationCommitted, notifySlotDeactivated);
                return;
            }

            Thing oldChip = oldSlot != null ? oldSlot.LoadedChip : null;
            if (switchContext.targetSlotIndex < 0)
            {
                setSwitchContext?.Invoke(side, null);

                if (oldSlot != null && oldSlot.IsActive)
                {
                    DeactivateSlot(oldSlot);
                    notifySlotDeactivated?.Invoke(side, oldSlot.Index, oldChip);
                }

                return;
            }

            setSwitchContext?.Invoke(
                side,
                BuildWaitingForConflictsContext(
                    switchContext.targetSlotIndex,
                    switchContext.targetChipThingId));

            if (oldSlot != null && oldSlot.IsActive)
            {
                DeactivateSlot(oldSlot);
                notifySlotDeactivated?.Invoke(side, oldSlot.Index, oldChip);
            }

            return;
        }

        /// <summary>
        /// 结束启用延迟。
        /// 成员职责：先清理当前侧切换上下文，再把目标槽位正式提交为激活态，或切到主副同步提交。
        /// </summary>
        private static void FinalizeActivating(
            TriggerSide side,
            SwitchContext switchContext,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlotRaw,
            Action<int, int> activateSynchronizedTargets,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted)
        {
            TriggerSlotState targetSlot = NormalizeDirectControlSlot(getSlot != null ? getSlot(side, switchContext.targetSlotIndex) : null, getSlot);
            if (ShouldUseSynchronizedHandTransition(targetSlot, getSlot, getActiveSlotRaw))
            {
                FinalizeSynchronizedActivating(
                    side,
                    switchContext,
                    getSwitchContext,
                    setSwitchContext,
                    getSlot,
                    activateSynchronizedTargets,
                    notifySlotActivationCommitted);
                return;
            }

            if (!IsPendingTargetValid(switchContext, targetSlot, null))
            {
                ClearInvalidPendingTarget(
                    switchContext.targetChipThingId,
                    getSwitchContext,
                    setSwitchContext);
                return;
            }

            if (targetSlot != null && ActivateSlot(targetSlot))
            {
                setSwitchContext?.Invoke(side, null);
                notifySlotActivationCommitted?.Invoke(side, targetSlot.Index, targetSlot.LoadedChip);
                return;
            }

            setSwitchContext?.Invoke(side, null);
        }

        /// <summary>
        /// 结束一组双侧同步停用延迟。
        /// 成员职责：统一关闭旧双侧，再决定进入同步启用延迟或直接同步激活。
        /// </summary>
        private static void FinalizeSynchronizedDeactivating(
            int currentTick,
            Func<Thing, int> resolveChipActivationDelayTicks,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlotRaw,
            Action<TriggerSlotState> deactivateBoundSlotImmediate,
            Action<int, int> activateSynchronizedTargets,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted,
            Action<TriggerSide, int, Thing> notifySlotDeactivated)
        {
            SwitchContext mainContext = getSwitchContext != null ? getSwitchContext(TriggerSide.Main) : null;
            SwitchContext subContext = getSwitchContext != null ? getSwitchContext(TriggerSide.Sub) : null;
            int targetMainIndex = mainContext != null ? mainContext.targetSlotIndex : -1;
            int targetSubIndex = subContext != null ? subContext.targetSlotIndex : -1;
            string targetThingId = mainContext != null && !string.IsNullOrEmpty(mainContext.targetChipThingId)
                ? mainContext.targetChipThingId
                : subContext != null ? subContext.targetChipThingId : null;

            TriggerSlotState activeMain = NormalizeDirectControlSlot(getActiveSlotRaw != null ? getActiveSlotRaw(TriggerSide.Main) : null, getSlot);
            TriggerSlotState activeSub = NormalizeDirectControlSlot(getActiveSlotRaw != null ? getActiveSlotRaw(TriggerSide.Sub) : null, getSlot);
            if (activeMain != null)
            {
                if (deactivateBoundSlotImmediate != null)
                {
                    deactivateBoundSlotImmediate(activeMain);
                }
                else
                {
                    DeactivateBoundSlotImmediate(activeMain, getSlot, setSwitchContext, notifySlotDeactivated);
                }
            }
            else if (activeSub != null)
            {
                if (deactivateBoundSlotImmediate != null)
                {
                    deactivateBoundSlotImmediate(activeSub);
                }
                else
                {
                    DeactivateBoundSlotImmediate(activeSub, getSlot, setSwitchContext, notifySlotDeactivated);
                }
            }

            if (targetMainIndex < 0 && targetSubIndex < 0)
            {
                setSwitchContext?.Invoke(TriggerSide.Main, null);
                setSwitchContext?.Invoke(TriggerSide.Sub, null);
                return;
            }

            setSwitchContext?.Invoke(
                TriggerSide.Main,
                BuildWaitingForConflictsContext(targetMainIndex, targetThingId));
            setSwitchContext?.Invoke(
                TriggerSide.Sub,
                BuildWaitingForConflictsContext(targetSubIndex, targetThingId));
        }

        /// <summary>
        /// 结束一组双侧同步启用延迟。
        /// 成员职责：清空双侧上下文并把目标双侧一起提交为激活态。
        /// </summary>
        private static void FinalizeSynchronizedActivating(
            TriggerSide side,
            SwitchContext currentContext,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Action<int, int> activateSynchronizedTargets,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted)
        {
            TriggerSlotState targetSlot = ResolvePendingTarget(
                side,
                currentContext,
                getSwitchContext,
                getSlot);
            if (!IsPendingTargetValid(currentContext, targetSlot, null))
            {
                ClearInvalidPendingTarget(
                    currentContext != null
                        ? currentContext.targetChipThingId
                        : null,
                    getSwitchContext,
                    setSwitchContext);
                return;
            }

            SwitchContext mainContext = getSwitchContext != null ? getSwitchContext(TriggerSide.Main) : null;
            SwitchContext subContext = getSwitchContext != null ? getSwitchContext(TriggerSide.Sub) : null;
            int targetMainIndex = mainContext != null ? mainContext.targetSlotIndex : currentContext.targetSlotIndex;
            int targetSubIndex = subContext != null ? subContext.targetSlotIndex : currentContext.targetSlotIndex;
            setSwitchContext?.Invoke(TriggerSide.Main, null);
            setSwitchContext?.Invoke(TriggerSide.Sub, null);
            if (activateSynchronizedTargets != null)
            {
                activateSynchronizedTargets(targetMainIndex, targetSubIndex);
                return;
            }

            // owner 没有注入测试替身时，必须回到正式同步激活路径，
            // 否则非零启用延迟到期后只会清空上下文而不会真正激活芯片。
            ActivateSynchronizedTargets(
                targetMainIndex,
                targetSubIndex,
                getSlot,
                notifySlotActivationCommitted);
        }

        /// <summary>
        /// 把同步切换目标正式提交为激活态。
        /// 成员职责：优先走绑定根槽一次性提交，否则分别激活双侧。
        /// </summary>
        private static void ActivateSynchronizedTargets(
            int targetMainIndex,
            int targetSubIndex,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted)
        {
            TriggerSlotState mainTarget = getSlot != null ? getSlot(TriggerSide.Main, targetMainIndex) : null;
            TriggerSlotState subTarget = getSlot != null ? getSlot(TriggerSide.Sub, targetSubIndex) : null;

            if (mainTarget != null && subTarget != null && mainTarget.LoadedChip == subTarget.LoadedChip)
            {
                // 成对芯片只能走原子提交；失败后不得退回两个普通单槽逐个激活。
                ActivateBoundSlotImmediate(mainTarget, getSlot, notifySlotActivationCommitted);
                return;
            }

            if (mainTarget != null && ActivateSlot(mainTarget))
            {
                notifySlotActivationCommitted?.Invoke(mainTarget.Side, mainTarget.Index, mainTarget.LoadedChip);
            }

            if (subTarget != null && ActivateSlot(subTarget))
            {
                notifySlotActivationCommitted?.Invoke(subTarget.Side, subTarget.Index, subTarget.LoadedChip);
            }
        }

        /// <summary>
        /// 立刻激活一组绑定槽位。
        /// 成员职责：以绑定根槽为主入口，同时激活镜像槽，但只提交一次逻辑芯片事件。
        /// </summary>
        private static bool ActivateBoundSlotImmediate(
            TriggerSlotState rootSlot,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Action<TriggerSide, int, Thing> notifySlotActivationCommitted)
        {
            rootSlot = NormalizeDirectControlSlot(rootSlot, getSlot);
            TriggerSlotState mirrorSlot = GetBindingPartnerSlot(rootSlot, getSlot);
            if (rootSlot == null || mirrorSlot == null)
            {
                return false;
            }

            if (rootSlot.LoadedChip == null
                || mirrorSlot.LoadedChip != rootSlot.LoadedChip
                || rootSlot.IsDisabled
                || mirrorSlot.IsDisabled)
            {
                DeactivateSlot(rootSlot);
                DeactivateSlot(mirrorSlot);
                return false;
            }

            bool rootActivated = ActivateSlot(rootSlot);
            bool mirrorActivated = ActivateSlot(mirrorSlot);
            if (!rootActivated || !mirrorActivated)
            {
                // 成对启用必须具备原子性；任一侧失败都回到双侧关闭。
                DeactivateSlot(rootSlot);
                DeactivateSlot(mirrorSlot);
                return false;
            }

            notifySlotActivationCommitted?.Invoke(rootSlot.Side, rootSlot.Index, rootSlot.LoadedChip);
            return true;
        }

        /// <summary>
        /// 让指定槽位正式进入激活状态。
        /// 成员职责：封装 TriggerSlotState.SetActive(true) 的前置校验。
        /// </summary>
        private static bool ActivateSlot(ITriggerSlotState slot)
        {
            TriggerSlotState state = slot as TriggerSlotState;
            if (state == null || state.LoadedChip == null || state.IsDisabled)
            {
                return false;
            }

            state.SetActive(true);
            return true;
        }

        /// <summary>
        /// 让指定槽位退出激活状态。
        /// 成员职责：封装 TriggerSlotState.SetActive(false)。
        /// </summary>
        private static void DeactivateSlot(ITriggerSlotState slot)
        {
            TriggerSlotState state = slot as TriggerSlotState;
            if (state == null)
            {
                return;
            }

            state.SetActive(false);
        }
    }
}
