using BDP.Content.Assembly.ChipManufacturing.Defs;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>
    /// 统一解析制造台各类预设面向玩家显示的名称。
    /// 动作预设允许使用显式语言键，武装型统一追加“型”，其他定义保持原版 LabelCap 行为。
    /// </summary>
    internal static class ChipPresetLabelResolver
    {
        /// <summary>解析一项制造预设的当前语言显示名。</summary>
        internal static string Resolve(Def preset)
        {
            if (preset == null)
            {
                return string.Empty;
            }

            ChipActionPresetDef action = preset as ChipActionPresetDef;
            if (action != null)
            {
                return action.ResolvedLabel.CapitalizeFirst();
            }

            ChipArmamentFormDef armamentForm = preset as ChipArmamentFormDef;
            return armamentForm != null
                ? armamentForm.LabelCap.ToString() + "型"
                : preset.LabelCap.ToString();
        }

        /// <summary>解析一项制造预设的当前语言说明。</summary>
        internal static string ResolveDescription(Def preset)
        {
            if (preset == null)
            {
                return string.Empty;
            }

            ChipActionPresetDef action = preset as ChipActionPresetDef;
            return action != null ? action.ResolvedDescription : preset.description;
        }
    }
}
