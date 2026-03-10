using BDP.Core;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// Ability的Trion消耗CompProperties。
    /// XML中配置在AbilityDef.comps列表中。
    /// v4.0：已废弃trionCostPerUse字段，改用芯片统一层usageCost。
    /// </summary>
    public class CompProperties_AbilityTrionCost : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityTrionCost()
        {
            compClass = typeof(CompAbilityEffect_TrionCost);
        }
    }

    /// <summary>
    /// Ability的Trion消耗效果组件——Apply()时消耗Trion，
    /// GizmoDisabled()和CanApplyOn()检查Trion可用量。
    /// 与CompAbilityEffect_Teleport等原版效果组件配合使用。
    /// v4.0：从激活的能力芯片读取usageCost（统一层）。
    /// </summary>
    public class CompAbilityEffect_TrionCost : CompAbilityEffect
    {
        public new CompProperties_AbilityTrionCost Props
            => (CompProperties_AbilityTrionCost)props;

        /// <summary>
        /// 获取Trion消耗量（从激活的能力芯片读取usageCost）。
        /// 查找路径：parent.pawn → CompTriggerBody → 激活槽位 → 芯片usageCost。
        /// </summary>
        private float GetTrionCost()
        {
            var pawn = parent.pawn;
            if (pawn == null) return 0f;

            // 获取CompTriggerBody
            var triggerBody = pawn.equipment?.Primary;
            if (triggerBody == null) return 0f;

            var triggerComp = triggerBody.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return 0f;

            // 遍历左右手槽位，找到配置了当前AbilityDef的芯片
            var abilityDef = parent.def;

            // 检查左手槽位
            var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
            if (leftSlot?.loadedChip != null)
            {
                var abilityConfig = leftSlot.loadedChip.def.GetModExtension<AbilityChipConfig>();
                if (abilityConfig?.abilityDef == abilityDef)
                {
                    return ChipUsageCostHelper.GetUsageCost(leftSlot.loadedChip);
                }
            }

            // 检查右手槽位
            var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);
            if (rightSlot?.loadedChip != null)
            {
                var abilityConfig = rightSlot.loadedChip.def.GetModExtension<AbilityChipConfig>();
                if (abilityConfig?.abilityDef == abilityDef)
                {
                    return ChipUsageCostHelper.GetUsageCost(rightSlot.loadedChip);
                }
            }

            return 0f; // 未找到对应芯片
        }

        /// <summary>执行效果时消耗Trion（统一层）。</summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = parent.pawn;
            if (pawn == null) return;

            float cost = GetTrionCost();
            if (cost > 0f)
            {
                var trion = pawn.GetComp<CompTrion>();
                trion?.Consume(cost);
            }
        }

        /// <summary>检查Trion是否足够（供Gizmo灰显）。</summary>
        public override bool GizmoDisabled(out string reason)
        {
            var pawn = parent.pawn;
            float cost = GetTrionCost();

            if (cost > 0f)
            {
                var trion = pawn?.GetComp<CompTrion>();
                if (trion == null || trion.Available < cost)
                {
                    reason = $"Trion不足（需要{cost:F1}，可用{trion?.Available ?? 0f:F1}）";
                    return true;
                }
            }

            reason = null;
            return false;
        }

        /// <summary>前置条件检查：Trion可用量是否足够。</summary>
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            var pawn = parent.pawn;
            float cost = GetTrionCost();

            if (cost > 0f)
            {
                var trion = pawn?.GetComp<CompTrion>();
                if (trion == null || trion.Available < cost)
                {
                    if (throwMessages)
                        Messages.Message($"Trion不足（需要{cost:F1}）",
                            pawn, MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }

            return true;
        }
    }
}
