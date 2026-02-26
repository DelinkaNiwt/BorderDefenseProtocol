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

        /// <summary>
        /// 从ActivatingSlot读取UtilityChipConfig，回退到遍历AllActiveSlots。
        /// 优先ActivatingSlot确保激活/关闭时读取正确槽位的配置。
        /// </summary>
        private static UtilityChipConfig GetConfig(Thing triggerBody)
        {
            var triggerComp = triggerBody.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return null;

            // 优先从ActivatingSlot读取
            var slot = triggerComp.ActivatingSlot;
            if (slot?.loadedChip != null)
            {
                var cfg = slot.loadedChip.def.GetModExtension<UtilityChipConfig>();
                if (cfg != null) return cfg;
            }

            // 回退：遍历所有激活槽位（兼容读档恢复）
            foreach (var activeSlot in triggerComp.AllActiveSlots())
            {
                var ext = activeSlot.loadedChip?.def?.GetModExtension<UtilityChipConfig>();
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
