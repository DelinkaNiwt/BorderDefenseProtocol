using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Core.Combos;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Validation
{
    /// <summary>
    /// 在加载期检查芯片制造 Def 的最低结构合法性。
    /// </summary>
    public static class ChipManufacturingDefValidator
    {
        /// <summary>可搜索的统一校验日志前缀。</summary>
        private const string LogPrefix = "[BDP.ChipManufacturing.DefValidation] ";

        /// <summary>
        /// 检查全部芯片功能定义和武装型 Def；每个错误只报告，不阻断其它 Def 扫描。
        /// </summary>
        public static void ValidateAll()
        {
            List<ChipManufacturingPresetDef> functionDefs = DefDatabase<ChipManufacturingPresetDef>.AllDefsListForReading;
            for (int index = 0; index < functionDefs.Count; index++)
            {
                ValidateChipFunctionDef(functionDefs[index]);
            }

            List<ChipArmamentFormDef> forms = DefDatabase<ChipArmamentFormDef>.AllDefsListForReading;
            for (int index = 0; index < forms.Count; index++)
            {
                ValidateArmamentForm(forms[index]);
            }
            ValidateImplicitDefaultConflicts(forms);

            List<ComboDef> combos = DefDatabase<ComboDef>.AllDefsListForReading;
            for (int index = 0; index < combos.Count; index++)
            {
                ValidateComboSources(combos[index]);
            }
        }

        /// <summary>检查单个芯片功能模板。</summary>
        private static void ValidateChipFunctionDef(ChipManufacturingPresetDef function)
        {
            if (function == null)
            {
                return;
            }

            if (function.config == null)
            {
                Report(function.defName, "缺少 config。");
            }
            ValidateCosts(function.defName, function.costList);
            if (function.additionalWorkAmount < 0f)
            {
                Report(function.defName, "附加工作量不得为负数。");
            }
        }

        /// <summary>检查单个武装构型定义。</summary>
        private static void ValidateArmamentForm(ChipArmamentFormDef form)
        {
            if (form == null)
            {
                return;
            }

            if (form.compatibleProfessions == null || form.compatibleProfessions.Count == 0)
            {
                Report(form.defName, "武装型必须声明至少一个兼容职业。");
            }

            if (form.maxFunctionCount < 1)
            {
                Report(form.defName, "武装型最大功能数量必须大于零。");
            }

            ValidateCosts(form.defName, form.additionalCost);
            if (form.additionalWorkAmount < 0f)
            {
                Report(form.defName, "附加工作量不得为负数。");
            }

            ValidateCompatibleFunctions(form);
            ValidateOverrideLists(form);
        }

        /// <summary>
        /// 检查武装型的功能适用范围；空列表表示不限制芯片功能模板，非空列表必须全部指向现有芯片功能定义。
        /// </summary>
        private static void ValidateCompatibleFunctions(ChipArmamentFormDef form)
        {
            if (form.compatibleChipFunctionDefNames == null)
            {
                return;
            }

            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < form.compatibleChipFunctionDefNames.Count; index++)
            {
                string functionDefName = form.compatibleChipFunctionDefNames[index];
                if (string.IsNullOrWhiteSpace(functionDefName))
                {
                    Report(form.defName, "功能适用范围不得包含空芯片功能定义 DefName。");
                    continue;
                }

                if (!names.Add(functionDefName))
                {
                    Report(form.defName, "功能适用范围不得重复引用芯片功能定义：" + functionDefName);
                }

                if (ChipManufacturingDefLookup.FindChipFunctionDef(functionDefName) == null)
                {
                    Report(form.defName, "功能适用范围引用不存在的芯片功能定义：" + functionDefName);
                }
            }
        }

        /// <summary>检查远程模块和近战工具覆盖列表不得包含空项。</summary>
        private static void ValidateOverrideLists(ChipArmamentFormDef form)
        {
            if (form.overrides == null)
            {
                return;
            }

            if (form.overrides.rangedModules != null)
            {
                for (int index = 0; index < form.overrides.rangedModules.Count; index++)
                {
                    if (form.overrides.rangedModules[index] == null)
                    {
                        Report(form.defName, "远程模块覆盖列表不得包含空项。");
                    }
                }
            }

            if (form.overrides.tools != null)
            {
                for (int index = 0; index < form.overrides.tools.Count; index++)
                {
                    if (form.overrides.tools[index] == null)
                    {
                        Report(form.defName, "近战工具覆盖列表不得包含空项。");
                    }
                }
            }
        }

        /// <summary>
        /// 检查隐式默认型之间是否可能同时命中，避免默认解析结果依赖 Def 加载顺序。
        /// </summary>
        private static void ValidateImplicitDefaultConflicts(List<ChipArmamentFormDef> forms)
        {
            if (forms == null)
            {
                return;
            }

            for (int leftIndex = 0; leftIndex < forms.Count; leftIndex++)
            {
                ChipArmamentFormDef left = forms[leftIndex];
                if (left == null || !left.implicitDefault)
                {
                    continue;
                }

                for (int rightIndex = leftIndex + 1; rightIndex < forms.Count; rightIndex++)
                {
                    ChipArmamentFormDef right = forms[rightIndex];
                    if (right == null || !right.implicitDefault)
                    {
                        continue;
                    }

                    if (ProfessionsOverlap(left, right) && FunctionScopesOverlap(left, right))
                    {
                        Report(left.defName, "隐式默认型与 " + right.defName + " 的职业和功能范围重叠，默认解析顺序不明确。");
                    }
                }
            }
        }

        /// <summary>判断两个武装型是否至少兼容同一个职业。</summary>
        private static bool ProfessionsOverlap(ChipArmamentFormDef left, ChipArmamentFormDef right)
        {
            if (left.compatibleProfessions == null || right.compatibleProfessions == null)
            {
                return false;
            }

            for (int leftIndex = 0; leftIndex < left.compatibleProfessions.Count; leftIndex++)
            {
                ChipProfessionDef profession = left.compatibleProfessions[leftIndex];
                if (profession != null && right.compatibleProfessions.Contains(profession))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>判断两个武装型的功能范围是否有交集；空范围代表全部芯片功能模板。</summary>
        private static bool FunctionScopesOverlap(ChipArmamentFormDef left, ChipArmamentFormDef right)
        {
            if (left.compatibleChipFunctionDefNames == null
                || left.compatibleChipFunctionDefNames.Count == 0
                || right.compatibleChipFunctionDefNames == null
                || right.compatibleChipFunctionDefNames.Count == 0)
            {
                return true;
            }

            for (int leftIndex = 0; leftIndex < left.compatibleChipFunctionDefNames.Count; leftIndex++)
            {
                if (right.compatibleChipFunctionDefNames.Contains(left.compatibleChipFunctionDefNames[leftIndex]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>在 Content 边界检查组合技引用的芯片功能来源仍然存在。</summary>
        private static void ValidateComboSources(ComboDef combo)
        {
            if (combo == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(combo.firstSourceKey)
                && ChipManufacturingDefLookup.FindChipFunctionDef(combo.firstSourceKey) == null)
            {
                Report(combo.defName, "组合技第一来源芯片功能定义不存在：" + combo.firstSourceKey);
            }

            if (!string.IsNullOrWhiteSpace(combo.secondSourceKey)
                && ChipManufacturingDefLookup.FindChipFunctionDef(combo.secondSourceKey) == null)
            {
                Report(combo.defName, "组合技第二来源芯片功能定义不存在：" + combo.secondSourceKey);
            }
        }

        /// <summary>检查材料条目必须引用具体物品且数量严格大于零。</summary>
        private static void ValidateCosts(string ownerDefName, List<ThingDefCountClass> costs)
        {
            if (costs == null)
            {
                return;
            }

            for (int index = 0; index < costs.Count; index++)
            {
                ThingDefCountClass cost = costs[index];
                if (cost?.thingDef == null || cost.count <= 0)
                {
                    Report(ownerDefName, "材料条目必须引用具体 ThingDef 且数量大于零。");
                }
            }
        }

        /// <summary>输出一条统一前缀的定义错误。</summary>
        private static void Report(string defName, string message)
        {
            Log.Error(LogPrefix + (defName ?? "<null>") + "：" + message);
        }
    }
}
