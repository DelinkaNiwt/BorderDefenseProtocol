using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// Ability芯片效果——激活时授予Ability，关闭时移除。
    /// 适用于"点击→选目标→执行"的一次性动作（蜘蛛、蚱蜢等）。
    /// 机制：通过RimWorld的Ability系统实现。
    /// </summary>
    public class AbilityChipEffect : IChipEffect
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
        /// 从CompTriggerBody读取AbilityChipConfig（委托给通用GetChipExtension）。
        /// </summary>
        private static AbilityChipConfig GetConfig(Thing triggerBody)
        {
            return triggerBody.TryGetComp<CompTriggerBody>()?.GetChipExtension<AbilityChipConfig>();
        }
    }

    /// <summary>Ability芯片的DefModExtension配置。</summary>
    public class AbilityChipConfig : DefModExtension
    {
        public AbilityDef abilityDef;
    }
}
