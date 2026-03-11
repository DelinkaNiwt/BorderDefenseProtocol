using System.Linq;
using Verse;
using BDP.Core;

namespace BDP.Combat
{
    /// <summary>
    /// 战斗体延时破裂Hediff。
    ///
    /// 设计思路:
    /// - 使用Hediff的Tick()方法管理倒计时，避免全局轮询
    /// - 倒计时结束时自动触发破裂流程
    /// - 自动清理临时Hediff
    ///
    /// 职责:
    /// - 记录破裂开始时间和原因
    /// - 管理90 ticks (1.5秒)倒计时
    /// - 倒计时结束时执行破裂逻辑
    /// - 显示剩余倒计时
    /// </summary>
    public class Hediff_CombatBodyCollapsing : HediffWithComps
    {
        // ═══════════════════════════════════════════
        //  字段
        // ═══════════════════════════════════════════

        /// <summary>破裂执行时间点(tick)</summary>
        private int collapseAtTick;

        /// <summary>破裂原因(用于日志)</summary>
        private string collapseReason;

        // ═══════════════════════════════════════════
        //  生命周期
        // ═══════════════════════════════════════════

        /// <summary>
        /// Hediff添加时初始化。
        /// 从Runtime读取破裂原因，设置倒计时。
        /// </summary>
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            // 设置破裂执行时间: 当前时间 + 90 ticks (1.5秒)
            collapseAtTick = Find.TickManager.TicksGame + 90;

            // 从Runtime读取破裂原因
            var runtime = CombatBodyRuntime.Of(pawn);
            collapseReason = runtime?.State.CollapseReason ?? "未知原因";

            Log.Message($"[BDP] 战斗体进入延时破裂: {pawn.LabelShort} (原因: {collapseReason}, 倒计时: 90 ticks)");
        }

        /// <summary>
        /// 每tick检查倒计时。
        /// 倒计时结束时执行破裂流程。
        /// </summary>
        public override void Tick()
        {
            base.Tick();

            // 检查倒计时是否结束
            if (Find.TickManager.TicksGame >= collapseAtTick)
            {
                ExecuteCollapse();
            }
        }

        // ═══════════════════════════════════════════
        //  破裂执行
        // ═══════════════════════════════════════════

        /// <summary>
        /// 执行破裂流程。
        /// 1. 打断当前动作
        /// 2. 移除所有临时Hediff
        /// 3. 调用Runtime.Deactivate()解除战斗体
        /// </summary>
        private void ExecuteCollapse()
        {
            Log.Warning($"[BDP] 战斗体破裂执行: {pawn.LabelShort} (原因: {collapseReason})");

            // 打断当前动作（避免破裂完成后继续破裂前的动作）
            CombatBodyQuery.InterruptCurrentAction(pawn, "破裂流程结束");

            // 移除自己
            pawn.health.RemoveHediff(this);

            // 移除所有部位待失效标记
            var pendingHediffs = pawn.health.hediffSet.hediffs
                .Where(h => h.def == BDP_DefOf.BDP_CombatBodyPartPending)
                .ToList();

            foreach (var hediff in pendingHediffs)
            {
                pawn.health.RemoveHediff(hediff);
            }

            // 执行解除流程
            var runtime = CombatBodyRuntime.Of(pawn);
            if (runtime != null)
            {
                runtime.Deactivate(isEmergency: true);
            }
            else
            {
                Log.Error($"[BDP] 破裂执行失败: {pawn.LabelShort} 没有CombatBodyRuntime");
            }
        }

        // ═══════════════════════════════════════════
        //  UI显示
        // ═══════════════════════════════════════════

        /// <summary>
        /// 显示剩余倒计时。
        /// 格式: "破裂倒计时: X ticks"
        /// </summary>
        public override string LabelInBrackets
        {
            get
            {
                int remaining = GetCollapseRemaining();
                return $"破裂倒计时: {remaining} ticks";
            }
        }

        /// <summary>
        /// 获取剩余倒计时(ticks)。
        /// </summary>
        private int GetCollapseRemaining()
        {
            return System.Math.Max(0, collapseAtTick - Find.TickManager.TicksGame);
        }

        // ═══════════════════════════════════════════
        //  序列化
        // ═══════════════════════════════════════════

        /// <summary>
        /// 序列化倒计时和破裂原因。
        /// 支持存档/读档。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref collapseAtTick, "collapseAtTick", 0);
            Scribe_Values.Look(ref collapseReason, "collapseReason", "未知原因");
        }

        // ═══════════════════════════════════════════
        //  辅助方法
        // ═══════════════════════════════════════════

    }
}
