using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Model;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>
    /// 制造编辑器和解析器共用的动作选择规则。
    /// </summary>
    public static class ChipCombinationSelectionRules
    {
        /// <summary>武装主分类稳定 DefName。</summary>
        private const string WeaponCategoryDefName = "BDP_ChipCategory_Weapon";

        /// <summary>读取当前职业允许的最大动作数。</summary>
        public static int MaxActionCount(ChipProfessionDef profession)
        {
            int result = 1;
            if (profession == null)
            {
                return result;
            }

            List<ChipArmamentFormDef> forms =
                Verse.DefDatabase<ChipArmamentFormDef>.AllDefsListForReading;
            for (int index = 0; index < forms.Count; index++)
            {
                ChipArmamentFormDef form = forms[index];
                if (form?.compatibleProfessions != null
                    && form.compatibleProfessions.Contains(profession)
                    && form.maxActionCount > result)
                {
                    result = form.maxActionCount;
                }
            }

            return result;
        }

        /// <summary>读取当前明确武装型允许的最大动作数。</summary>
        public static int MaxActionCount(
            ChipProfessionDef profession,
            ChipArmamentFormDef armamentForm)
        {
            if (armamentForm != null)
            {
                return Math.Max(1, armamentForm.maxActionCount);
            }

            return MaxActionCount(profession);
        }

        /// <summary>判断最终职业是否单向接纳动作的原生职业。</summary>
        public static bool CanUseAction(ChipProfessionDef profession, ChipActionPresetDef action)
        {
            if (action == null)
            {
                return false;
            }

            // 职业是武装分类的制造分支；其他主分类保留动作定义上的职业语义，但不参与筛选。
            if (!IsWeaponAction(action))
            {
                return true;
            }

            if (action.profession == null)
            {
                return profession == null;
            }

            return profession != null
                && profession.acceptedActionProfessions != null
                && profession.acceptedActionProfessions.Contains(action.profession);
        }

        /// <summary>
        /// 判断一个动作是否在武装型的动作适用范围内。
        /// 空白白名单表示不限制动作；非空白名单只允许列出的动作。
        /// </summary>
        public static bool CanUseArmamentFormAction(
            ChipArmamentFormDef armamentForm,
            ChipActionPresetDef action)
        {
            if (armamentForm == null || action == null)
            {
                return armamentForm == null;
            }

            if (armamentForm.compatibleActionPresetDefNames == null
                || armamentForm.compatibleActionPresetDefNames.Count == 0)
            {
                return true;
            }

            return armamentForm.compatibleActionPresetDefNames.Contains(action.defName);
        }

        /// <summary>
        /// 判断当前动作集合是否全部属于武装型的动作适用范围。
        /// 构型仍作为最终合并结果的一次性外层覆盖，不支持只覆盖双动作中的一个形态。
        /// </summary>
        public static bool CanUseArmamentForm(
            ChipArmamentFormDef armamentForm,
            IList<ChipActionPresetDef> actions)
        {
            if (armamentForm == null
                || armamentForm.compatibleActionPresetDefNames == null
                || armamentForm.compatibleActionPresetDefNames.Count == 0)
            {
                return true;
            }

            if (actions == null || actions.Count == 0)
            {
                return true;
            }

            for (int index = 0; index < actions.Count; index++)
            {
                if (!CanUseArmamentFormAction(armamentForm, actions[index]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>判断动作是否属于需要职业筛选的武装主分类。</summary>
        private static bool IsWeaponAction(ChipActionPresetDef action)
        {
            return action?.config?.Profile?.Category?.defName == WeaponCategoryDefName;
        }

        /// <summary>判断动作预设自身是否已经包含多个形态。</summary>
        public static bool HasIntrinsicMultipleModes(ChipActionPresetDef action)
        {
            return action?.config?.Expression?.Modes != null
                && action.config.Expression.Modes.Count > 1;
        }

        /// <summary>
        /// 尝试选择动作；单选职业直接替换，枪手第三项不会自动顶替。
        /// </summary>
        public static bool TrySelect(
            ChipCombinationRecord record,
            ChipProfessionDef profession,
            ChipActionPresetDef action,
            out string failureCode)
        {
            failureCode = null;
            if (record == null || action == null || !CanUseAction(profession, action))
            {
                failureCode = "ProfessionMismatch";
                return false;
            }

            if (record.OrderedActionPresetDefNames == null)
            {
                record.OrderedActionPresetDefNames = new List<string>();
            }

            if (record.OrderedActionPresetDefNames.Contains(action.defName))
            {
                return true;
            }

            ChipArmamentFormDef selectedForm =
                ChipManufacturingDefLookup.FindArmamentForm(record.ArmamentFormDefName);
            if (!CanUseArmamentFormAction(selectedForm, action))
            {
                failureCode = "ArmamentFormActionMismatch";
                return false;
            }
            int maximum = MaxActionCount(profession, selectedForm);
            if (maximum == 1)
            {
                record.OrderedActionPresetDefNames.Clear();
                record.OrderedActionPresetDefNames.Add(action.defName);
                return true;
            }

            if (HasIntrinsicMultipleModes(action) && record.OrderedActionPresetDefNames.Count > 0)
            {
                failureCode = "IntrinsicMultiModeMustStandAlone";
                return false;
            }

            for (int index = 0; index < record.OrderedActionPresetDefNames.Count; index++)
            {
                ChipActionPresetDef selected =
                    ChipManufacturingDefLookup.FindAction(record.OrderedActionPresetDefNames[index]);
                if (HasIntrinsicMultipleModes(selected))
                {
                    failureCode = "IntrinsicMultiModeAlreadySelected";
                    return false;
                }
            }

            if (record.OrderedActionPresetDefNames.Count >= maximum)
            {
                failureCode = "ActionLimitReached";
                return false;
            }

            record.OrderedActionPresetDefNames.Add(action.defName);
            return true;
        }

        /// <summary>按索引取消动作；删除首项时后一项自然前移。</summary>
        public static void RemoveAt(ChipCombinationRecord record, int index)
        {
            if (record?.OrderedActionPresetDefNames == null
                || index < 0
                || index >= record.OrderedActionPresetDefNames.Count)
            {
                return;
            }

            record.OrderedActionPresetDefNames.RemoveAt(index);
        }

        /// <summary>交换恰好两个动作的形态顺序。</summary>
        public static bool Swap(ChipCombinationRecord record)
        {
            if (record?.OrderedActionPresetDefNames == null
                || record.OrderedActionPresetDefNames.Count != 2)
            {
                return false;
            }

            string first = record.OrderedActionPresetDefNames[0];
            record.OrderedActionPresetDefNames[0] = record.OrderedActionPresetDefNames[1];
            record.OrderedActionPresetDefNames[1] = first;
            return true;
        }
    }
}
