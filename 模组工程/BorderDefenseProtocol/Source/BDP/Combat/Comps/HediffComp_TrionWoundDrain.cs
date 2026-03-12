using BDP.Core;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// HediffComp配置: Trion伤口流失
    ///
    /// 功能: 基于原版伤口严重度的持续Trion消耗
    /// 配置参数:
    /// - drainPerSeverityPerDay: 每点伤口严重度每天消耗的Trion量
    /// </summary>
    public class HediffCompProperties_TrionWoundDrain : HediffCompProperties
    {
        /// <summary>
        /// 每点伤口严重度每天消耗的Trion量
        /// 示例: 总伤口严重度10 → 每天消耗50 Trion (10 × 5.0)
        /// </summary>
        public float drainPerSeverityPerDay = 5.0f;

        public HediffCompProperties_TrionWoundDrain()
        {
            compClass = typeof(HediffComp_TrionWoundDrain);
        }
    }

    /// <summary>
    /// HediffComp: Trion伤口流失
    ///
    /// 职责: 基于Pawn当前原版伤口的总严重度,持续消耗Trion
    /// 触发: CompPostTick (低频,每250 ticks检查一次)
    ///
    /// 重构说明:
    /// - 替代原9种自定义伤口的Trion流失
    /// - 直接读取原版Hediff_Injury数据
    /// - 通过CompTrion.RegisterDrain注册持续消耗
    /// </summary>
    public class HediffComp_TrionWoundDrain : HediffComp
    {
        public HediffCompProperties_TrionWoundDrain Props => (HediffCompProperties_TrionWoundDrain)props;

        /// <summary>
        /// Tick计数器
        /// </summary>
        private int tickCounter = 0;

        /// <summary>
        /// 检查间隔(ticks)
        /// </summary>
        private const int CheckInterval = 250;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            tickCounter++;
            if (tickCounter >= CheckInterval)
            {
                tickCounter = 0;
                UpdateWoundDrain();
            }
        }

        /// <summary>
        /// 更新伤口Trion流失
        /// </summary>
        private void UpdateWoundDrain()
        {
            if (Pawn?.health?.hediffSet == null) return;

            // 计算总伤口严重度
            float totalSeverity = 0f;

            foreach (var hediff in Pawn.health.hediffSet.hediffs)
            {
                // 只统计原版伤口
                if (hediff is Hediff_Injury injury)
                {
                    totalSeverity += injury.Severity;
                }
                // 也可以考虑新鲜的MissingPart (IsFresh)
                else if (hediff is Hediff_MissingPart missingPart && missingPart.IsFresh)
                {
                    // 新鲜缺失部位也算作"伤口"
                    totalSeverity += 5f; // 固定值,可配置
                }
            }

            // 计算drain rate (per day)
            float drainRate = totalSeverity * Props.drainPerSeverityPerDay;

            // 注册到CompTrion
            var compTrion = Pawn.GetComp<CompTrion>();
            if (compTrion != null)
            {
                compTrion.RegisterDrain("CombatWounds", drainRate);
            }
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            // 激活时立即更新一次
            UpdateWoundDrain();
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            // 移除时清理drain注册
            var compTrion = Pawn?.GetComp<CompTrion>();
            if (compTrion != null)
            {
                compTrion.UnregisterDrain("CombatWounds");
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
        }
    }
}
