using UnityEngine;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 战斗体状态聚合器。
    /// 统一管理战斗体的所有状态,避免状态分散。
    ///
    /// 设计目的:
    /// - 消除状态分散问题(原来分散在5个地方)
    /// - 提供状态转换验证(非法转换抛异常)
    /// - 支持外层FSM(Inactive→Active→Cooldown)
    /// - 为后续内层FSM预留扩展空间
    /// </summary>
    public class CombatBodyState : IExposable
    {
        // ═══════════════════════════════════════════
        //  外层FSM状态定义
        // ═══════════════════════════════════════════

        /// <summary>
        /// 外层FSM状态枚举。
        /// Inactive: 未激活,可以激活战斗体
        /// Active: 已激活,战斗体运行中
        /// Collapsing: 延时破裂中,无敌且禁止操作
        /// Cooldown: 冷却中,不可激活战斗体
        /// </summary>
        public enum OuterState
        {
            Inactive,   // 未激活
            Active,     // 激活中
            Collapsing, // 延时破裂中
            Cooldown    // 冷却中
        }

        // ═══════════════════════════════════════════
        //  状态字段
        // ═══════════════════════════════════════════

        private OuterState outerState = OuterState.Inactive;
        private float allocatedTrion;      // 已占用的Trion量
        private int activationTick;        // 激活时的tick
        private int cooldownEndTick;       // 冷却结束的tick
        private int collapseStartTick;     // 破裂开始的tick
        private string collapseReason;     // 破裂原因(用于日志和UI)

        // ═══════════════════════════════════════════
        //  属性
        // ═══════════════════════════════════════════

        /// <summary>当前外层状态</summary>
        public OuterState CurrentState => outerState;

        /// <summary>是否处于激活状态</summary>
        public bool IsActive => outerState == OuterState.Active;

        /// <summary>是否处于延时破裂状态</summary>
        public bool IsCollapsing => outerState == OuterState.Collapsing;

        /// <summary>是否处于冷却状态</summary>
        public bool IsInCooldown => outerState == OuterState.Cooldown;

        /// <summary>已占用的Trion量</summary>
        public float AllocatedTrion => allocatedTrion;

        /// <summary>激活时的tick</summary>
        public int ActivationTick => activationTick;

        /// <summary>破裂原因(仅在Collapsing状态有效)</summary>
        public string CollapseReason => collapseReason;

        // ═══════════════════════════════════════════
        //  状态查询方法
        // ═══════════════════════════════════════════

        /// <summary>
        /// 检查是否可以激活战斗体。
        /// 条件: 当前状态为Inactive,或者Cooldown已结束。
        /// </summary>
        public bool CanActivate()
        {
            return outerState == OuterState.Inactive ||
                   (outerState == OuterState.Cooldown && Find.TickManager.TicksGame >= cooldownEndTick);
        }

        /// <summary>
        /// 获取剩余冷却时间(ticks)。
        /// 如果不在冷却状态,返回0。
        /// </summary>
        public int GetCooldownRemaining()
        {
            if (outerState != OuterState.Cooldown) return 0;
            return Mathf.Max(0, cooldownEndTick - Find.TickManager.TicksGame);
        }

        /// <summary>
        /// 检查是否可以手动解除战斗体。
        /// Collapsing状态下禁止手动解除。
        /// </summary>
        public bool CanManualDeactivate()
        {
            return outerState == OuterState.Active;
        }

        /// <summary>
        /// 检查是否处于无敌状态。
        /// Collapsing状态下无敌。
        /// </summary>
        public bool IsInvulnerable()
        {
            return outerState == OuterState.Collapsing;
        }

        /// <summary>
        /// 获取破裂倒计时剩余时间(ticks)。
        /// 如果不在Collapsing状态,返回0。
        /// </summary>
        public int GetCollapseRemaining()
        {
            if (outerState != OuterState.Collapsing) return 0;
            // 破裂延时固定为90 ticks (1.5秒)
            int collapseEndTick = collapseStartTick + 90;
            return Mathf.Max(0, collapseEndTick - Find.TickManager.TicksGame);
        }

        // ═══════════════════════════════════════════
        //  状态转换方法
        // ═══════════════════════════════════════════

        /// <summary>
        /// 转换到Active状态。
        /// 前置条件: 当前状态为Inactive或Cooldown已结束。
        /// </summary>
        /// <param name="allocate">占用的Trion量</param>
        public void TransitionToActive(float allocate)
        {
            if (!CanActivate())
                throw new System.InvalidOperationException($"Cannot activate from {outerState}");

            outerState = OuterState.Active;
            allocatedTrion = allocate;
            activationTick = Find.TickManager.TicksGame;
        }

        /// <summary>
        /// 转换到Collapsing状态。
        /// 前置条件: 当前状态为Active。
        /// </summary>
        /// <param name="reason">破裂原因(用于日志和UI)</param>
        public void TransitionToCollapsing(string reason)
        {
            if (outerState != OuterState.Active)
                throw new System.InvalidOperationException($"Cannot collapse from {outerState}");

            outerState = OuterState.Collapsing;
            collapseStartTick = Find.TickManager.TicksGame;
            collapseReason = reason;
        }

        /// <summary>
        /// 转换到Cooldown状态(从Collapsing)。
        /// 前置条件: 当前状态为Collapsing。
        /// </summary>
        /// <param name="cooldownTicks">冷却时长(ticks)</param>
        public void TransitionToCooldownFromCollapsing(int cooldownTicks)
        {
            if (outerState != OuterState.Collapsing)
                throw new System.InvalidOperationException($"Cannot cooldown from {outerState} (expected Collapsing)");

            outerState = OuterState.Cooldown;
            cooldownEndTick = Find.TickManager.TicksGame + cooldownTicks;
            allocatedTrion = 0f;
            collapseReason = null; // 清理破裂原因
        }

        /// <summary>
        /// 转换到Cooldown状态。
        /// 前置条件: 当前状态为Active。
        /// </summary>
        /// <param name="cooldownTicks">冷却时长(ticks)</param>
        public void TransitionToCooldown(int cooldownTicks)
        {
            if (outerState != OuterState.Active)
                throw new System.InvalidOperationException($"Cannot cooldown from {outerState}");

            outerState = OuterState.Cooldown;
            cooldownEndTick = Find.TickManager.TicksGame + cooldownTicks;
            allocatedTrion = 0f;
        }

        /// <summary>
        /// 转换到Inactive状态。
        /// 前置条件: 当前状态为Cooldown。
        /// </summary>
        public void TransitionToInactive()
        {
            if (outerState != OuterState.Cooldown)
                throw new System.InvalidOperationException($"Cannot deactivate from {outerState}");

            outerState = OuterState.Inactive;
            cooldownEndTick = 0;
        }

        // ═══════════════════════════════════════════
        //  序列化
        // ═══════════════════════════════════════════

        public void ExposeData()
        {
            Scribe_Values.Look(ref outerState, "outerState", OuterState.Inactive);
            Scribe_Values.Look(ref allocatedTrion, "allocatedTrion", 0f);
            Scribe_Values.Look(ref activationTick, "activationTick", 0);
            Scribe_Values.Look(ref cooldownEndTick, "cooldownEndTick", 0);
            Scribe_Values.Look(ref collapseStartTick, "collapseStartTick", 0);
            Scribe_Values.Look(ref collapseReason, "collapseReason", null);
        }
    }
}
