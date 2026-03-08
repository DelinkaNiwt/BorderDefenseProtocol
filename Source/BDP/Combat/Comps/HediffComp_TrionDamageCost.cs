using BDP.Core;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// HediffComp配置: Trion伤害消耗
    ///
    /// 功能: 受到伤害时按比例消耗Trion
    /// 配置参数:
    /// - costPerDamage: 每点伤害消耗的Trion量(默认0.5)
    /// </summary>
    public class HediffCompProperties_TrionDamageCost : HediffCompProperties
    {
        /// <summary>
        /// 每点伤害消耗的Trion量
        /// 示例: 受到20点伤害 → 消耗10 Trion (20 × 0.5)
        /// </summary>
        public float costPerDamage = 0.5f;

        public HediffCompProperties_TrionDamageCost()
        {
            compClass = typeof(HediffComp_TrionDamageCost);
        }
    }

    /// <summary>
    /// HediffComp: Trion伤害消耗（v13.1重构：事件订阅模式）
    ///
    /// 职责: 受到伤害时消耗等比例的Trion
    /// 触发: 通过订阅BDPEvents.OnDamageReceived事件
    ///
    /// 重构说明:
    /// - 替代原TrionCostHandler
    /// - 在原版伤害流程之后执行
    /// - 不干扰原版伤害计算
    /// - v13.1: 改为静态事件订阅，消除Patch硬编码调用
    /// </summary>
    [StaticConstructorOnStartup]
    public class HediffComp_TrionDamageCost : HediffComp
    {
        // 静态构造函数：订阅全局伤害事件
        static HediffComp_TrionDamageCost()
        {
            BDPEvents.OnDamageReceived += OnDamageReceivedGlobal;
        }

        /// <summary>
        /// 全局伤害事件处理器（静态）
        /// </summary>
        private static void OnDamageReceivedGlobal(DamageReceivedEventArgs args)
        {
            if (args?.Pawn?.health?.hediffSet == null) return;

            // 查找战斗体激活Hediff
            var hediff = args.Pawn.health.hediffSet.GetFirstHediffOfDef(BDP_DefOf.BDP_CombatBodyActive);
            if (hediff == null) return;

            // 查找TrionDamageCost Comp
            var comp = hediff.TryGetComp<HediffComp_TrionDamageCost>();
            comp?.ConsumeTrionForDamage(args.TotalDamageDealt);
        }

        public HediffCompProperties_TrionDamageCost Props => (HediffCompProperties_TrionDamageCost)props;

        /// <summary>
        /// 处理伤害后的Trion消耗
        /// </summary>
        /// <param name="totalDamageDealt">实际造成的伤害量</param>
        private void ConsumeTrionForDamage(float totalDamageDealt)
        {
            if (totalDamageDealt <= 0) return;

            // 计算需要消耗的Trion
            float cost = totalDamageDealt * Props.costPerDamage;

            // 获取CompTrion
            var compTrion = Pawn.GetComp<CompTrion>();
            if (compTrion == null)
            {
                Log.Error($"[BDP] HediffComp_TrionDamageCost: Pawn {Pawn} 没有CompTrion");
                return;
            }

            // 消耗Trion（不再通知RuptureMonitor，由Hediff_CombatBodyActive轮询检测）
            compTrion.Consume(cost);
        }
    }
}
