using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody 部分类——组合效果系统（v15.0新增）。
    /// 统一管理所有类型的组合效果（Verb/Ability/Hediff）。
    /// </summary>
    public partial class CompTriggerBody
    {
        // ═══════════════════════════════════════════════════════
        // 字段（在 Fields.cs 中定义）
        // ═══════════════════════════════════════════════════════
        // private readonly List<ComboEffectDef> activeComboEffects;

        /// <summary>
        /// 检测并激活所有匹配的组合效果（统一入口）。
        /// 在芯片激活/关闭后调用，替代原有的 TryGrantComboAbility 和 CreateComboVerbs。
        /// </summary>
        private void UpdateComboEffects(Pawn pawn)
        {
            var leftSlot = GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveSlot(SlotSide.RightHand);

            // 无双侧芯片时清空所有组合效果
            if (leftSlot?.loadedChip == null || rightSlot?.loadedChip == null)
            {
                DeactivateAllComboEffects(pawn);
                return;
            }

            // 遍历所有组合效果定义
            foreach (var comboDef in DefDatabase<ComboEffectDef>.AllDefsListForReading)
            {
                bool matches = comboDef.Matches(leftSlot.loadedChip.def, rightSlot.loadedChip.def);
                bool isActive = activeComboEffects.Contains(comboDef);

                if (matches && !isActive)
                {
                    // 匹配成功且未激活 → 激活
                    try
                    {
                        comboDef.ActivateEffect(pawn, this, leftSlot.loadedChip, rightSlot.loadedChip);
                        activeComboEffects.Add(comboDef);

                        if (Prefs.DevMode)
                            Log.Message($"[BDP] 激活组合效果: {comboDef.defName} ({comboDef.EffectType})");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[BDP] 激活组合效果失败: {comboDef.defName} — {ex}");
                    }
                }
                else if (!matches && isActive)
                {
                    // 不再匹配但仍激活 → 撤销
                    try
                    {
                        comboDef.DeactivateEffect(pawn, this);
                        activeComboEffects.Remove(comboDef);

                        if (Prefs.DevMode)
                            Log.Message($"[BDP] 撤销组合效果: {comboDef.defName} ({comboDef.EffectType})");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[BDP] 撤销组合效果失败: {comboDef.defName} — {ex}");
                    }
                }
            }
        }

        /// <summary>
        /// 撤销所有已激活的组合效果。
        /// </summary>
        private void DeactivateAllComboEffects(Pawn pawn)
        {
            for (int i = activeComboEffects.Count - 1; i >= 0; i--)
            {
                try
                {
                    activeComboEffects[i].DeactivateEffect(pawn, this);
                }
                catch (Exception ex)
                {
                    Log.Error($"[BDP] 撤销组合效果失败: {activeComboEffects[i].defName} — {ex}");
                }
            }
            activeComboEffects.Clear();
        }

        /// <summary>
        /// 为指定 ComboVerbDef 创建 Verb 实例（由 ComboVerbDef.ActivateEffect 调用）。
        /// 保持现有 CreateComboVerb 逻辑，但通过统一接口调用。
        /// </summary>
        internal void CreateComboVerbsForDef(ComboVerbDef comboDef, Thing leftChip, Thing rightChip)
        {
            // 复用现有的 CreateComboVerb 方法
            var leftSlot = GetActiveOrActivatingSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveOrActivatingSlot(SlotSide.RightHand);
            if (leftSlot == null || rightSlot == null) return;

            matchedComboDef = comboDef;
            comboAttackVerb = CreateComboVerb(comboDef, false, OwnerPawn, leftSlot, rightSlot);
            comboSecondaryVerb = CreateComboVerb(comboDef, true, OwnerPawn, leftSlot, rightSlot);
        }

        /// <summary>
        /// 清除组合技 Verb 缓存（由 ComboVerbDef.DeactivateEffect 调用）。
        /// </summary>
        internal void ClearComboVerbs()
        {
            comboAttackVerb = null;
            comboSecondaryVerb = null;
            matchedComboDef = null;
        }
    }
}
