using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 辅助类芯片效果——激活时授予Ability，关闭时移除。
    /// 适用于"点击→选目标→执行"的一次性动作（蜘蛛、蚱蜢等）。
    /// </summary>
    public class UtilityChipEffect : IChipEffect
    {
        public void Activate(Pawn pawn, Thing triggerBody)
        {
            var cfg = GetConfig(triggerBody);
            if (cfg?.abilityDef == null) return;
            pawn.abilities?.GainAbility(cfg.abilityDef);
        }

        public void Deactivate(Pawn pawn, Thing triggerBody)
        {
            var cfg = GetConfig(triggerBody);
            if (cfg?.abilityDef == null) return;
            pawn.abilities?.RemoveAbility(cfg.abilityDef);
        }

        public void Tick(Pawn pawn, Thing triggerBody) { }

        public bool CanActivate(Pawn pawn, Thing triggerBody) => true;

        private static UtilityChipConfig GetConfig(Thing triggerBody)
        {
            var triggerComp = triggerBody.TryGetComp<CompTriggerBody>();
            foreach (var slot in triggerComp?.AllActiveSlots() ?? System.Linq.Enumerable.Empty<ChipSlot>())
            {
                var ext = slot.loadedChip?.def?.GetModExtension<UtilityChipConfig>();
                if (ext != null) return ext;
            }
            return null;
        }
    }

    /// <summary>辅助芯片的DefModExtension配置。</summary>
    public class UtilityChipConfig : DefModExtension
    {
        public AbilityDef abilityDef;
    }
}
