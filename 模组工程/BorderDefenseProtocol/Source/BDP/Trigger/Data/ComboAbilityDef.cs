using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 组合能力定义（v4.0 F1新增，v15.0重构）——授予原版 Ability。
    ///
    /// v15.0变更：继承 ComboEffectDef 统一抽象层。
    ///
    /// 使用方式：
    ///   CompTriggerBody 在芯片激活/关闭时通过 UpdateComboEffects 统一检测，
    ///   匹配成功则授予/移除对应 Ability。Ability 自带 Gizmo（原版机制）。
    /// </summary>
    public class ComboAbilityDef : ComboEffectDef
    {
        /// <summary>组合成功后授予的AbilityDef。</summary>
        public AbilityDef abilityDef;

        // ═══════════════════════════════════════════════════════
        // ComboEffectDef 抽象方法实现
        // ═══════════════════════════════════════════════════════

        public override ComboEffectType EffectType => ComboEffectType.Ability;

        public override void ActivateEffect(Pawn pawn, CompTriggerBody triggerComp,
            Thing leftChip, Thing rightChip)
        {
            if (abilityDef == null || pawn?.abilities == null) return;
            pawn.abilities.GainAbility(abilityDef);
        }

        public override void DeactivateEffect(Pawn pawn, CompTriggerBody triggerComp)
        {
            if (abilityDef == null || pawn?.abilities == null) return;
            pawn.abilities.RemoveAbility(abilityDef);
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors())
                yield return e;
            if (abilityDef == null) yield return "abilityDef is null";
        }
    }
}
