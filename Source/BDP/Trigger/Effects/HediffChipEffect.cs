using BDP.Core;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// Hediff芯片效果——激活时添加Hediff，关闭时移除。
    /// 适用于护盾、增益、减益等持续状态效果。
    /// 机制：通过RimWorld的Hediff系统实现。
    /// v2.0：支持Severity机制，多个相同芯片激活时增加Severity而非添加多个hediff。
    /// </summary>
    public class HediffChipEffect : IChipEffect
    {
        public void Activate(Pawn pawn, Thing triggerBody)
        {
            var cfg = GetConfig(triggerBody);
            if (cfg?.hediffDef == null) return;

            // 检查是否已存在相同的hediff
            var existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(cfg.hediffDef);

            if (existingHediff != null)
            {
                // 已存在：检查Severity上限，然后增加Severity
                if (existingHediff.Severity >= existingHediff.def.maxSeverity)
                {
                    Log.Warning($"[BDP-HediffChip] {pawn.LabelShort} 护盾已达到最大Severity（{existingHediff.def.maxSeverity}），无法继续叠加");
                    return;
                }
                existingHediff.Severity += 1f;
                Log.Message($"[BDP-HediffChip] {pawn.LabelShort} 增加护盾Severity: {existingHediff.Severity}");
            }
            else
            {
                // 不存在：添加新hediff，初始Severity=1
                var newHediff = pawn.health.AddHediff(cfg.hediffDef);
                newHediff.Severity = 1f;
                Log.Message($"[BDP-HediffChip] {pawn.LabelShort} 添加护盾，Severity=1");
            }
        }

        public void Deactivate(Pawn pawn, Thing triggerBody)
        {
            var cfg = GetConfig(triggerBody);
            if (cfg?.hediffDef == null) return;

            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(cfg.hediffDef);
            if (hediff == null) return;

            // 减少Severity
            hediff.Severity -= 1f;
            Log.Message($"[BDP-HediffChip] {pawn.LabelShort} 减少护盾Severity: {hediff.Severity}");

            // 只有Severity<=0时才移除hediff
            if (hediff.Severity <= 0f)
            {
                pawn.health.RemoveHediff(hediff);
                Log.Message($"[BDP-HediffChip] {pawn.LabelShort} 移除护盾");
            }
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
    }
}
