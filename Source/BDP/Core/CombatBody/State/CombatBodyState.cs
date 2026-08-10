using System;
using UnityEngine;
using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体外层阶段真值对象。
    ///
    /// 它只回答三类问题：
    /// - 现在处于什么阶段
    /// - 当前是否允许进入下一阶段
    /// - 冷却与崩解这类时间过程还剩多少
    ///
    /// 这里特别强调：
    /// - 冷却倒计时本身允许依赖 RimWorld 时间
    /// - 但“能不能再次激活”不能要求外部 tick 先跑一遍才成立
    ///
    /// 所以它会在查询入口主动收掉已经结束的冷却。
    /// </summary>
    internal sealed class CombatBodyState : IExposable, ICombatBodyPhaseState
    {
        /// <summary>
        /// 崩解阶段默认持续的最小时长。
        /// </summary>
        private const int DefaultCollapseTicks = 90;

        /// <summary>
        /// 当前外层阶段。
        /// </summary>
        private CombatBodyPhase phase = CombatBodyPhase.Inactive;

        /// <summary>
        /// 当前被战斗体正式锁定的 Trion 量。
        /// </summary>
        private float allocatedTrion;

        /// <summary>
        /// 进入 Active 的绝对 tick。
        /// </summary>
        private int activationTick;

        /// <summary>
        /// 冷却结束的绝对 tick。
        /// </summary>
        private int cooldownEndTick;

        /// <summary>
        /// 手动形态切换锁结束的绝对游戏 tick。
        /// </summary>
        private int manualTransformLockEndTick;

        /// <summary>
        /// 崩解表现开始的绝对 tick。
        /// </summary>
        private int collapseStartTick;

        /// <summary>
        /// 崩解原因。
        /// </summary>
        private string collapseReason;

        /// <summary>
        /// 当前战斗体阶段。
        /// </summary>
        public CombatBodyPhase Phase
        {
            get
            {
                ResolveCooldownIfFinished();
                return phase;
            }
        }

        /// <summary>
        /// 当前已正式锁定的 Trion 量。
        /// </summary>
        public float AllocatedTrion
        {
            get { return allocatedTrion; }
        }

        /// <summary>
        /// 进入激活相位时记录的绝对 tick。
        /// </summary>
        public int ActivationTick
        {
            get { return activationTick; }
        }

        /// <summary>
        /// 当前崩解原因。
        /// </summary>
        public string CollapseReason
        {
            get { return collapseReason; }
        }

        /// <summary>
        /// 判断当前是否允许进入激活相位。
        /// </summary>
        public bool CanActivate()
        {
            ResolveCooldownIfFinished();
            return phase == CombatBodyPhase.Inactive && !IsManualTransformLocked();
        }

        /// <summary>
        /// 判断当前是否允许手动退出。
        /// </summary>
        public bool CanManualDeactivate()
        {
            ResolveCooldownIfFinished();
            return phase == CombatBodyPhase.Active && !IsManualTransformLocked();
        }

        /// <summary>
        /// 从当前 tick 起启动或延长一次手动形态切换锁。
        /// </summary>
        public void BeginManualTransformLock(int lockTicks)
        {
            int requestedEndTick = GetCurrentTick() + Mathf.Max(0, lockTicks);
            manualTransformLockEndTick = Mathf.Max(manualTransformLockEndTick, requestedEndTick);
        }

        /// <summary>
        /// 判断当前是否处于崩解无敌阶段。
        /// </summary>
        public bool IsInvulnerable()
        {
            return phase == CombatBodyPhase.Collapsing;
        }

        /// <summary>
        /// 获取剩余冷却 tick。
        /// </summary>
        public int GetCooldownRemaining()
        {
            ResolveCooldownIfFinished();
            if (phase != CombatBodyPhase.Cooldown)
            {
                return 0;
            }

            return Mathf.Max(0, cooldownEndTick - GetCurrentTick());
        }

        /// <summary>
        /// 获取剩余崩解 tick。
        /// </summary>
        public int GetCollapseRemaining()
        {
            if (phase != CombatBodyPhase.Collapsing)
            {
                return 0;
            }

            // 当前先用一个固定的最小崩解时长。
            return Mathf.Max(0, collapseStartTick + DefaultCollapseTicks - GetCurrentTick());
        }

        /// <summary>
        /// 正式进入激活相位并记录占用量。
        /// </summary>
        public void EnterActive(float allocateAmount)
        {
            if (!CanActivate())
            {
                throw new InvalidOperationException("Current combat body phase cannot enter Active.");
            }

            phase = CombatBodyPhase.Active;
            allocatedTrion = Mathf.Max(0f, allocateAmount);
            activationTick = GetCurrentTick();
            collapseReason = null;
            collapseStartTick = 0;
            cooldownEndTick = 0;
        }

        /// <summary>
        /// 从激活相位切入崩解相位。
        /// </summary>
        public void EnterCollapsing(string reason)
        {
            ResolveCooldownIfFinished();
            if (phase != CombatBodyPhase.Active)
            {
                throw new InvalidOperationException("Only Active combat body can enter Collapsing.");
            }

            phase = CombatBodyPhase.Collapsing;
            collapseStartTick = GetCurrentTick();
            collapseReason = reason;
        }

        /// <summary>
        /// 进入冷却相位并重置崩解数据。
        /// </summary>
        public void EnterCooldown(int cooldownTicks)
        {
            ResolveCooldownIfFinished();
            if (phase != CombatBodyPhase.Active && phase != CombatBodyPhase.Collapsing)
            {
                throw new InvalidOperationException("Only Active or Collapsing combat body can enter Cooldown.");
            }

            phase = CombatBodyPhase.Cooldown;
            cooldownEndTick = GetCurrentTick() + Mathf.Max(0, cooldownTicks);
            allocatedTrion = 0f;
            collapseReason = null;
            collapseStartTick = 0;

            // 零冷却要立刻落回 Inactive，不能等时间流动。
            ResolveCooldownIfFinished();
        }

        /// <summary>
        /// 从冷却相位落回未激活相位。
        /// </summary>
        public void EnterInactive()
        {
            ResolveCooldownIfFinished();
            if (phase != CombatBodyPhase.Cooldown)
            {
                throw new InvalidOperationException("Only Cooldown combat body can enter Inactive.");
            }

            phase = CombatBodyPhase.Inactive;
            cooldownEndTick = 0;
        }

        /// <summary>
        /// 存读档最小阶段事实。
        /// </summary>
        public void ExposeData()
        {
            // 保存最小阶段事实。
            Scribe_Values.Look(ref phase, "phase", CombatBodyPhase.Inactive);
            Scribe_Values.Look(ref allocatedTrion, "allocatedTrion", 0f);
            Scribe_Values.Look(ref activationTick, "activationTick", 0);
            Scribe_Values.Look(ref cooldownEndTick, "cooldownEndTick", 0);
            Scribe_Values.Look(ref manualTransformLockEndTick, "manualTransformLockEndTick", 0);
            Scribe_Values.Look(ref collapseStartTick, "collapseStartTick", 0);
            Scribe_Values.Look(ref collapseReason, "collapseReason");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ResolveCooldownIfFinished();
            }
        }

        /// <summary>
        /// 如果冷却已经到点，就立刻把阶段收回 Inactive。
        /// 这样玩家暂停状态下点按钮、查状态时，也能直接看到已经成立的结果。
        /// </summary>
        private void ResolveCooldownIfFinished()
        {
            if (phase != CombatBodyPhase.Cooldown)
            {
                return;
            }

            if (GetCurrentTick() < cooldownEndTick)
            {
                return;
            }

            phase = CombatBodyPhase.Inactive;
            cooldownEndTick = 0;
        }

        /// <summary>
        /// 判断当前是否仍处于手动形态切换锁定窗口。
        /// </summary>
        private bool IsManualTransformLocked()
        {
            if (GetCurrentTick() < manualTransformLockEndTick)
            {
                return true;
            }

            manualTransformLockEndTick = 0;
            return false;
        }

        /// <summary>
        /// 读取当前游戏绝对 tick。
        /// </summary>
        private int GetCurrentTick()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }
    }
}
