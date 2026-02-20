using BDP.Core;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 护盾类芯片效果——激活时添加护盾Hediff，受击时消耗Trion。
    /// 具体HediffDef通过CompProperties_TriggerChip的DefModExtension配置。
    /// </summary>
    public class ShieldChipEffect : IChipEffect
    {
        public void Activate(Pawn pawn, Thing triggerBody)
        {
            var cfg = GetConfig(triggerBody);
            if (cfg?.shieldHediffDef == null) return;
            pawn.health.AddHediff(cfg.shieldHediffDef);
        }

        public void Deactivate(Pawn pawn, Thing triggerBody)
        {
            var cfg = GetConfig(triggerBody);
            if (cfg?.shieldHediffDef == null) return;
            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(cfg.shieldHediffDef);
            if (hediff != null) pawn.health.RemoveHediff(hediff);
        }

        public void Tick(Pawn pawn, Thing triggerBody) { }

        public bool CanActivate(Pawn pawn, Thing triggerBody) => true;

        private static ShieldChipConfig GetConfig(Thing triggerBody)
        {
            // 从触发体上激活的芯片读取配置
            var triggerComp = triggerBody.TryGetComp<CompTriggerBody>();
            foreach (var slot in triggerComp?.AllActiveSlots() ?? System.Linq.Enumerable.Empty<ChipSlot>())
            {
                var ext = slot.loadedChip?.def?.GetModExtension<ShieldChipConfig>();
                if (ext != null) return ext;
            }
            return null;
        }
    }

    /// <summary>护盾芯片的DefModExtension配置。</summary>
    public class ShieldChipConfig : DefModExtension
    {
        public HediffDef shieldHediffDef;
        public float trionCostPerDamageFactor = 1f; // 每点伤害消耗的Trion倍率
    }
}
