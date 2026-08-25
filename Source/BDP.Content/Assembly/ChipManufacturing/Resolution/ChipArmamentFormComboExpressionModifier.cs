using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Combos;
using BDP.Core.Expressions;
using BDP.Core.Expressions.External;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>
    /// 把来源变体对应的武装构型修正应用到组合结果条目副本。
    /// 只处理组合结果显式字段，不读取或修改第一、第二来源结果。
    /// </summary>
    public sealed class ChipArmamentFormComboExpressionModifier :
        IComboExpressionVariantModifierProvider
    {
        /// <summary>
        /// 按中性来源变体键查找武装构型，并对组合条目副本应用一次修正。
        /// </summary>
        public void Apply(
            IList<ComboExpressionEntryConfig> comboEntries,
            string sourceVariantKey)
        {
            if (comboEntries == null || string.IsNullOrWhiteSpace(sourceVariantKey))
            {
                return;
            }

            ChipArmamentFormDef armamentForm = ChipManufacturingDefLookup.FindArmamentForm(
                sourceVariantKey.Trim());
            if (armamentForm == null
                || (armamentForm.overrides == null && armamentForm.projectileOverrides == null))
            {
                return;
            }

            for (int index = 0; index < comboEntries.Count; index++)
            {
                ComboExpressionEntryConfig entry = comboEntries[index];
                if (entry != null)
                {
                    ChipArmamentFormExpressionService.ApplyArmamentFormOverrides(
                        entry,
                        armamentForm);
                }
            }
        }
    }
}
