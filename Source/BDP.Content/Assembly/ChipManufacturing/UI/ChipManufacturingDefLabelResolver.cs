using BDP.Content.Assembly.ChipManufacturing.Defs;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>
    /// 统一解析制造台各类定义面向玩家显示的名称。
    /// 芯片功能模板与武装构型允许使用显式语言键，武装构型统一追加语言包后缀，其他定义保持原版 LabelCap 行为。
    /// </summary>
    internal static class ChipManufacturingDefLabelResolver
    {
        /// <summary>解析一项制造定义的当前语言显示名。</summary>
        internal static string Resolve(Def definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            ChipManufacturingPresetDef function = definition as ChipManufacturingPresetDef;
            if (function != null)
            {
                return function.ResolvedLabel.CapitalizeFirst();
            }

            ChipArmamentFormDef armamentForm = definition as ChipArmamentFormDef;
            if (armamentForm != null)
            {
                return "BDP_ChipManufacturing_ArmamentFormSuffix".Translate(
                    armamentForm.ResolvedLabel.CapitalizeFirst()).ToString();
            }

            return definition.LabelCap.ToString();
        }

        /// <summary>解析一项制造定义的当前语言说明。</summary>
        internal static string ResolveDescription(Def definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            ChipManufacturingPresetDef function = definition as ChipManufacturingPresetDef;
            if (function != null)
            {
                return function.ResolvedDescription;
            }

            ChipArmamentFormDef armamentForm = definition as ChipArmamentFormDef;
            return armamentForm != null
                ? armamentForm.ResolvedDescription
                : definition.description;
        }
    }
}
