using BDP.Core;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 隐身类芯片效果——激活时添加隐身Hediff并注册持续Trion消耗。
    /// 持续消耗通过CompTrion.RegisterDrain聚合结算（设计决策T12）。
    /// </summary>
    public class StealthChipEffect : IChipEffect
    {
        public void Activate(Pawn pawn, Thing triggerBody)
        {
            var cfg = GetConfig(triggerBody);
            if (cfg?.stealthHediffDef == null) return;

            pawn.health.AddHediff(cfg.stealthHediffDef);

            // 注册持续消耗（key包含side以支持主副侧同时激活不同隐身芯片）
            if (cfg.drainPerDay > 0f)
            {
                var trion = pawn.GetComp<CompTrion>();
                trion?.RegisterDrain(GetDrainKey(triggerBody), cfg.drainPerDay);
            }
        }

        public void Deactivate(Pawn pawn, Thing triggerBody)
        {
            var cfg = GetConfig(triggerBody);
            if (cfg?.stealthHediffDef == null) return;

            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(cfg.stealthHediffDef);
            if (hediff != null) pawn.health.RemoveHediff(hediff);

            // 注销持续消耗
            pawn.GetComp<CompTrion>()?.UnregisterDrain(GetDrainKey(triggerBody));
        }

        public void Tick(Pawn pawn, Thing triggerBody) { }

        public bool CanActivate(Pawn pawn, Thing triggerBody) => true;

        private static string GetDrainKey(Thing triggerBody)
            => $"stealth_{triggerBody.thingIDNumber}";

        private static StealthChipConfig GetConfig(Thing triggerBody)
        {
            var triggerComp = triggerBody.TryGetComp<CompTriggerBody>();
            foreach (var slot in triggerComp?.AllActiveSlots() ?? System.Linq.Enumerable.Empty<ChipSlot>())
            {
                var ext = slot.loadedChip?.def?.GetModExtension<StealthChipConfig>();
                if (ext != null) return ext;
            }
            return null;
        }
    }

    /// <summary>隐身芯片的DefModExtension配置。</summary>
    public class StealthChipConfig : DefModExtension
    {
        public HediffDef stealthHediffDef;
        public float drainPerDay = 5f; // 每天持续消耗的Trion
    }
}
