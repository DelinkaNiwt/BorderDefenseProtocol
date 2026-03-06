using System.Collections.Generic;
using BDP.FireMode;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody的Gizmo生成部分类（阶段3.2）。
    ///
    /// 职责：
    /// - 生成芯片攻击Gizmo（左手/右手/双手/组合技）
    /// - 生成射击模式Gizmo
    /// - 生成状态Gizmo（切换进度显示）
    /// - 生成调试Gizmo
    /// </summary>
    public partial class CompTriggerBody
    {
        // ═══════════════════════════════════════════
        //  Gizmo生成
        // ═══════════════════════════════════════════

        public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra()
        {
            foreach (var g in base.CompGetEquippedGizmosExtra()) yield return g;

            if (!OwnerHasTrionGland()) yield break;

            // ── v5.0新增：芯片攻击Gizmo（仅征召时显示，减少UI噪音） ──
            bool drafted = OwnerPawn?.Drafted == true;
            if (drafted)
            {
                var leftSlot = GetActiveSlot(SlotSide.LeftHand);
                var rightSlot = GetActiveSlot(SlotSide.RightHand);
                var leftChipDef = leftSlot?.loadedChip?.def;
                var rightChipDef = rightSlot?.loadedChip?.def;

                if (leftHandAttackVerb != null && leftChipDef != null)
                {
                    yield return new Command_BDPChipAttack
                    {
                        verb = leftHandAttackVerb,
                        secondaryVerb = leftHandSecondaryVerb, // v9.0：副攻击（可以是齐射或其他模式）
                        attackId = leftChipDef.defName,
                        icon = leftChipDef.uiIcon,
                        defaultLabel = leftChipDef.label,
                    };
                }
                if (rightHandAttackVerb != null && rightChipDef != null)
                {
                    yield return new Command_BDPChipAttack
                    {
                        verb = rightHandAttackVerb,
                        secondaryVerb = rightHandSecondaryVerb, // v9.0：副攻击（可以是齐射或其他模式）
                        attackId = rightChipDef.defName,
                        icon = rightChipDef.uiIcon,
                        defaultLabel = rightChipDef.label,
                    };
                }
                if (dualAttackVerb != null && leftChipDef != null && rightChipDef != null)
                {
                    // 排序保证A+B=B+A
                    var a = leftChipDef.defName;
                    var b = rightChipDef.defName;
                    if (string.Compare(a, b, System.StringComparison.Ordinal) > 0)
                    { var tmp = a; a = b; b = tmp; }

                    yield return new Command_BDPChipAttack
                    {
                        verb = dualAttackVerb,
                        secondaryVerb = dualSecondaryVerb, // v9.0：副攻击（可以是齐射或其他模式）
                        attackId = "dual:" + a + "+" + b,
                        icon = parent.def.uiIcon, // 触发体图标
                        defaultLabel = "双手触发",
                    };
                }

                // v10.0：组合技Gizmo（B+C同时激活时显示）
                if (comboAttackVerb != null && matchedComboDef != null)
                {
                    yield return new Command_BDPChipAttack
                    {
                        verb = comboAttackVerb,
                        secondaryVerb = comboSecondaryVerb, // v9.0：副攻击
                        attackId = "combo:" + matchedComboDef.defName,
                        icon = parent.def.uiIcon, // 暂用触发体图标
                        defaultLabel = matchedComboDef.label ?? "组合技",
                    };
                }
            }

            // v9.0：射击模式Gizmo（始终显示，方便战前配置）
            foreach (var slot in AllActiveSlots())
            {
                var fm = slot.loadedChip?.TryGetComp<CompFireMode>();
                if (fm != null)
                    yield return new Gizmo_FireMode(fm, slot.loadedChip.def.label);
            }

            // v2.1.1：allowChipManagement=false时不显示状态Gizmo
            // 原因：近界/黑触发器玩家无法操作芯片，显示Gizmo无意义
            if (Props.allowChipManagement)
                yield return new Gizmo_TriggerBodyStatus(this);

            if (!DebugSettings.godMode) yield break;
            foreach (var g in GetDebugGizmos()) yield return g;
        }
    }
}
