using System;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody部分类 - 切换状态机模块
    ///
    /// 职责：
    /// - 状态查询（IsSwitching, IsSideSwitching, GetSideSwitchProgress, GetSideSwitchPhase）
    /// - 状态转换（TryResolveSideSwitch, SetSideCtx）
    /// - 懒求值结算（WindingDown/WarmingUp阶段自动推进）
    /// </summary>
    public partial class CompTriggerBody
    {
        /// <summary>
        /// 任一侧是否处于切换中（向后兼容属性）。
        /// 懒求值：访问时自动结算到期的阶段。
        /// </summary>
        public bool IsSwitching
        {
            get
            {
                TryResolveSideSwitch(ref leftSwitchCtx, SlotSide.LeftHand);
                TryResolveSideSwitch(ref rightSwitchCtx, SlotSide.RightHand);
                return leftSwitchCtx != null || rightSwitchCtx != null;
            }
        }

        /// <summary>指定侧是否在切换中。</summary>
        public bool IsSideSwitching(SlotSide side)
        {
            if (side == SlotSide.LeftHand)
            {
                TryResolveSideSwitch(ref leftSwitchCtx, SlotSide.LeftHand);
                return leftSwitchCtx != null;
            }
            if (side == SlotSide.RightHand)
            {
                TryResolveSideSwitch(ref rightSwitchCtx, SlotSide.RightHand);
                return rightSwitchCtx != null;
            }
            return false;
        }

        /// <summary>指定侧的切换进度（0=刚开始，1=完成）。</summary>
        public float GetSideSwitchProgress(SlotSide side)
        {
            var ctx = side == SlotSide.LeftHand ? leftSwitchCtx : rightSwitchCtx;
            if (ctx == null) return 1f;

            int now = Find.TickManager.TicksGame;
            int remaining = ctx.phaseEndTick - now;

            if (ctx.phase == SwitchPhase.WindingDown)
            {
                if (ctx.winddownDuration <= 0) return 1f;
                return 1f - Mathf.Clamp01((float)remaining / ctx.winddownDuration);
            }
            else // WarmingUp
            {
                if (ctx.warmupDuration <= 0) return 1f;
                return 1f - Mathf.Clamp01((float)remaining / ctx.warmupDuration);
            }
        }

        /// <summary>指定侧当前切换阶段（供UI区分WindingDown/WarmingUp颜色）。</summary>
        public SwitchPhase GetSideSwitchPhase(SlotSide side)
        {
            var ctx = side == SlotSide.LeftHand ? leftSwitchCtx : rightSwitchCtx;
            return ctx?.phase ?? SwitchPhase.Idle;
        }

        /// <summary>总体切换进度（向后兼容，取两侧中较低的进度）。</summary>
        public float SwitchProgress
        {
            get
            {
                float left = leftSwitchCtx != null ? GetSideSwitchProgress(SlotSide.LeftHand) : 1f;
                float right = rightSwitchCtx != null ? GetSideSwitchProgress(SlotSide.RightHand) : 1f;
                return Mathf.Min(left, right);
            }
        }

        /// <summary>
        /// 懒求值：检查指定侧的切换阶段是否到期，到期则结算。
        /// WindingDown到期 → 关闭旧芯片 → targetSlotIndex≥0时进入WarmingUp，否则回到Idle。
        /// WarmingUp到期 → 激活新芯片 → 回到Idle（ctx=null）。
        /// </summary>
        private void TryResolveSideSwitch(ref SwitchContext ctx, SlotSide side)
        {
            if (ctx == null) return;
            int now = Find.TickManager.TicksGame;
            if (now < ctx.phaseEndTick) return; // 未到期

            if (ctx.phase == SwitchPhase.WindingDown)
            {
                // 后摇到期 → 关闭旧芯片
                var oldSlot = GetSlot(side, ctx.windingDownSlotIndex);
                if (oldSlot != null) DeactivateSlot(oldSlot);

                // 纯关闭（无目标芯片）：后摇到期直接回到Idle
                if (ctx.targetSlotIndex < 0)
                {
                    ctx = null;
                    return;
                }

                // 切换：后摇到期 → 进入新芯片前摇
                var newSlot = GetSlot(side, ctx.targetSlotIndex);
                var newChipComp = newSlot?.loadedChip?.TryGetComp<TriggerChipComp>();
                int warmup = newChipComp?.Props.activationWarmup ?? 0;
                int cooldown = System.Math.Max(Props.switchCooldownTicks, warmup);

                ctx.phase = SwitchPhase.WarmingUp;
                ctx.phaseEndTick = now + cooldown;
                ctx.warmupDuration = cooldown;
                ctx.windingDownSlotIndex = -1;

                // 如果cooldown为0，立即结算WarmingUp
                if (cooldown <= 0)
                {
                    if (CanActivateChip(side, ctx.targetSlotIndex))
                        DoActivate(GetSlot(side, ctx.targetSlotIndex));
                    ctx = null;
                }
            }
            else if (ctx.phase == SwitchPhase.WarmingUp)
            {
                // 前摇到期 → 激活新芯片
                if (CanActivateChip(side, ctx.targetSlotIndex))
                    DoActivate(GetSlot(side, ctx.targetSlotIndex));
                ctx = null; // 回到Idle
            }
        }
    }
}
