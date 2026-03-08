using BDP.Core;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// Hediff芯片效果——激活时添加Hediff，关闭时移除。
    /// 适用于护盾、增益、减益等持续状态效果。
    /// 机制：通过RimWorld的Hediff系统实现。
    /// </summary>
    public class HediffChipEffect : IChipEffect
    {
        public void Activate(Pawn pawn, Thing triggerBody)
        {
            var cfg = GetConfig(triggerBody);
            if (cfg?.hediffDef == null) return;
            pawn.health.AddHediff(cfg.hediffDef);
        }

        public void Deactivate(Pawn pawn, Thing triggerBody)
        {
            var cfg = GetConfig(triggerBody);
            if (cfg?.hediffDef == null) return;
            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(cfg.hediffDef);
            if (hediff != null) pawn.health.RemoveHediff(hediff);
        }

        public void Tick(Pawn pawn, Thing triggerBody) { }

        public bool CanActivate(Pawn pawn, Thing triggerBody) => true;

        /// <summary>
        /// 从CompTriggerBody读取HediffChipConfig（委托给通用GetChipExtension）。
        /// </summary>
        private static HediffChipConfig GetConfig(Thing triggerBody)
        {
            return triggerBody.TryGetComp<CompTriggerBody>()?.GetChipExtension<HediffChipConfig>();
        }
    }

    /// <summary>Hediff芯片的DefModExtension配置。</summary>
    public class HediffChipConfig : DefModExtension
    {
        public HediffDef hediffDef;
        public float trionCostPerDamageFactor = 1f; // 每点伤害消耗的Trion倍率
    }
}
