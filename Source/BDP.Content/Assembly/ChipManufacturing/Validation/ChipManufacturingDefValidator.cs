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
        /// 检查全部动作和枪壳 Def；每个错误只报告，不阻断其它 Def 扫描。
        /// </summary>
        public static void ValidateAll()
        {
            List<ChipActionPresetDef> actions = DefDatabase<ChipActionPresetDef>.AllDefsListForReading;
            for (int index = 0; index < actions.Count; index++)
            {
                ValidateAction(actions[index]);
            }

            List<ChipGunShellDef> shells = DefDatabase<ChipGunShellDef>.AllDefsListForReading;
            for (int index = 0; index < shells.Count; index++)
            {
                ValidateShell(shells[index]);
            }

            List<ComboDef> combos = DefDatabase<ComboDef>.AllDefsListForReading;
            for (int index = 0; index < combos.Count; index++)
            {
                ValidateComboSources(combos[index]);
            }
        }

        /// <summary>检查单个动作预设。</summary>
        private static void ValidateAction(ChipActionPresetDef action)
        {
            if (action == null)
            {
                return;
            }

            if (action.config == null)
            {
                Report(action.defName, "缺少 config。");
            }
            else if (action.config.Profile != null
                && action.config.Profile.Category != null
                && action.config.Profile.Category.defName == "BDP_ChipCategory_Weapon"
                && action.profession == null)
            {
                Report(action.defName, "武装动作必须声明唯一职业。");
            }

            ValidateCosts(action.defName, action.costList);
            if (action.additionalWorkAmount < 0f)
            {
                Report(action.defName, "附加工作量不得为负数。");
            }
        }

        /// <summary>检查单个枪壳预设。</summary>
        private static void ValidateShell(ChipGunShellDef shell)
        {
            if (shell == null)
            {
                return;
            }

            if (shell.compatibleProfessions == null || shell.compatibleProfessions.Count == 0)
            {
                Report(shell.defName, "枪壳必须声明至少一个兼容职业。");
            }

            ValidateCosts(shell.defName, shell.additionalCost);
            if (shell.additionalWorkAmount < 0f)
            {
                Report(shell.defName, "附加工作量不得为负数。");
            }
        }

        /// <summary>在 Content 边界检查组合技引用的动作来源仍然存在。</summary>
        private static void ValidateComboSources(ComboDef combo)
        {
            if (combo == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(combo.chipA)
                && ChipManufacturingDefLookup.FindAction(combo.chipA) == null)
            {
                Report(combo.defName, "组合技来源动作 A 不存在：" + combo.chipA);
            }

            if (!string.IsNullOrWhiteSpace(combo.chipB)
                && ChipManufacturingDefLookup.FindAction(combo.chipB) == null)
            {
                Report(combo.defName, "组合技来源动作 B 不存在：" + combo.chipB);
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
