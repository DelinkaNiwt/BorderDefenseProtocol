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
        /// 从CompTriggerBody读取UtilityChipConfig（委托给通用GetChipExtension）。
        /// </summary>
        private static UtilityChipConfig GetConfig(Thing triggerBody)
        {
            return triggerBody.TryGetComp<CompTriggerBody>()?.GetChipExtension<UtilityChipConfig>();
        }
    }

    /// <summary>辅助芯片的DefModExtension配置。</summary>
    public class UtilityChipConfig : DefModExtension
    {
        public AbilityDef abilityDef;
    }
}
