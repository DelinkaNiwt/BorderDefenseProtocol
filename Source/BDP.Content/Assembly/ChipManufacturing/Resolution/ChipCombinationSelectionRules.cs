using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Model;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>
    /// 制造编辑器和解析器共用的芯片功能模板选择规则。
    /// </summary>
    public static class ChipCombinationSelectionRules
    {
        /// <summary>读取当前职业允许的最大芯片功能模板数量。</summary>
        public static int MaxFunctionCount(ChipProfessionDef profession)
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
                    && form.maxFunctionCount > result)
                {
                    result = form.maxFunctionCount;
                }
            }

            return result;
        }

        /// <summary>读取当前明确武装型允许的最大芯片功能模板数量。</summary>
        public static int MaxFunctionCount(
            ChipProfessionDef profession,
            ChipArmamentFormDef armamentForm)
        {
            if (armamentForm != null)
            {
                return Math.Max(1, armamentForm.maxFunctionCount);
            }

            return MaxFunctionCount(profession);
        }

        /// <summary>判断最终职业是否单向接纳芯片功能模板的原生职业。</summary>
        public static bool CanUseFunction(ChipProfessionDef profession, ChipManufacturingPresetDef function)
        {
            if (function == null)
            {
                return false;
            }

            // 功能未声明原生职业时不限制最终职业；声明职业时统一走 Def 的单向接纳表。
            if (function.profession == null)
            {
                return true;
            }

            if (profession == null)
            {
                return false;
            }

            return profession.acceptedSourceProfessions != null
                && profession.acceptedSourceProfessions.Contains(function.profession);
        }

        /// <summary>
        /// 判断一个芯片功能模板是否在武装型的功能适用范围内。
        /// 空白白名单表示不限制芯片功能模板；非空白名单只允许列出的芯片功能模板。
        /// </summary>
        public static bool CanUseArmamentFormFunction(
            ChipArmamentFormDef armamentForm,
            ChipManufacturingPresetDef function)
        {
            if (armamentForm == null || function == null)
            {
                return armamentForm == null;
            }

            if (armamentForm.compatibleChipFunctionDefNames == null
                || armamentForm.compatibleChipFunctionDefNames.Count == 0)
            {
                return true;
            }

            return armamentForm.compatibleChipFunctionDefNames.Contains(function.defName);
        }

        /// <summary>
        /// 判断当前芯片功能模板集合是否全部属于武装型的功能适用范围。
        /// 构型仍作为最终合并结果的一次性外层覆盖，不支持只覆盖双芯片功能模板中的一个形态。
        /// </summary>
        public static bool CanUseArmamentForm(
            ChipArmamentFormDef armamentForm,
            IList<ChipManufacturingPresetDef> functionDefs)
        {
            if (armamentForm == null
                || armamentForm.compatibleChipFunctionDefNames == null
                || armamentForm.compatibleChipFunctionDefNames.Count == 0)
            {
                return true;
            }

            if (functionDefs == null || functionDefs.Count == 0)
            {
                return true;
            }

            for (int index = 0; index < functionDefs.Count; index++)
            {
                if (!CanUseArmamentFormFunction(armamentForm, functionDefs[index]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>判断芯片功能模板自身是否已经包含多个形态。</summary>
        public static bool HasIntrinsicMultipleModes(ChipManufacturingPresetDef function)
        {
            return function?.config?.Expression?.Modes != null
                && function.config.Expression.Modes.Count > 1;
        }

        /// <summary>
        /// 尝试选择芯片功能模板；单选职业直接替换，枪手第三项不会自动顶替。
        /// </summary>
        public static bool TrySelect(
            ChipCombinationRecord record,
            ChipProfessionDef profession,
            ChipManufacturingPresetDef function,
            out string failureCode)
        {
            failureCode = null;
            if (record == null || function == null || !CanUseFunction(profession, function))
            {
                failureCode = "ProfessionMismatch";
                return false;
            }

            if (record.OrderedChipFunctionDefNames == null)
            {
                record.OrderedChipFunctionDefNames = new List<string>();
            }

            if (record.OrderedChipFunctionDefNames.Contains(function.defName))
            {
                return true;
            }

            ChipArmamentFormDef selectedForm =
                ChipManufacturingDefLookup.FindArmamentForm(record.ArmamentFormDefName);
            if (!CanUseArmamentFormFunction(selectedForm, function))
            {
                failureCode = "ArmamentFormFunctionMismatch";
                return false;
            }
            int maximum = MaxFunctionCount(profession, selectedForm);
            if (maximum == 1)
            {
                record.OrderedChipFunctionDefNames.Clear();
                record.OrderedChipFunctionDefNames.Add(function.defName);
                return true;
            }

            if (HasIntrinsicMultipleModes(function) && record.OrderedChipFunctionDefNames.Count > 0)
            {
                failureCode = "IntrinsicMultiModeMustStandAlone";
                return false;
            }

            for (int index = 0; index < record.OrderedChipFunctionDefNames.Count; index++)
            {
                ChipManufacturingPresetDef selected =
                    ChipManufacturingDefLookup.FindChipFunctionDef(record.OrderedChipFunctionDefNames[index]);
                if (HasIntrinsicMultipleModes(selected))
                {
                    failureCode = "IntrinsicMultiModeAlreadySelected";
                    return false;
                }
            }

            if (record.OrderedChipFunctionDefNames.Count >= maximum)
            {
                failureCode = "FunctionLimitReached";
                return false;
            }

            record.OrderedChipFunctionDefNames.Add(function.defName);
            return true;
        }

        /// <summary>按索引取消芯片功能模板；删除首项时后一项自然前移。</summary>
        public static void RemoveAt(ChipCombinationRecord record, int index)
        {
            if (record?.OrderedChipFunctionDefNames == null
                || index < 0
                || index >= record.OrderedChipFunctionDefNames.Count)
            {
                return;
            }

            record.OrderedChipFunctionDefNames.RemoveAt(index);
        }

        /// <summary>交换恰好两个芯片功能模板的形态顺序。</summary>
        public static bool Swap(ChipCombinationRecord record)
        {
            if (record?.OrderedChipFunctionDefNames == null
                || record.OrderedChipFunctionDefNames.Count != 2)
            {
                return false;
            }

            string first = record.OrderedChipFunctionDefNames[0];
            record.OrderedChipFunctionDefNames[0] = record.OrderedChipFunctionDefNames[1];
            record.OrderedChipFunctionDefNames[1] = first;
            return true;
        }
    }
}
