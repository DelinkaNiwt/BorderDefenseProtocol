using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Core.Chips;

namespace BDP.Content.Information.Combos
{
    /// <summary>从来源功能定义建立组合技理论输入，不调用制造准入解析。</summary>
    internal static class ComboDeclaredSourceResolver
    {
        /// <summary>复制来源功能定义，并按当前宿主构型直接应用构型字段修正。</summary>
        internal static bool TryResolve(string sourceKey, string professionKey, string variantKey,
            out ChipDefinitionConfig config, out ChipSourceReferenceSnapshot reference)
        {
            config = null;
            reference = null;
            ChipManufacturingPresetDef function = ChipManufacturingDefLookup.FindChipFunctionDef(sourceKey);
            if (function?.config?.Profile?.Category == null) return false;

            config = ChipConfigurationMergeService.CloneSingle(function);
            ChipArmamentFormDef armamentForm = null;
            if (!string.IsNullOrWhiteSpace(variantKey))
            {
                armamentForm = ChipManufacturingDefLookup.FindArmamentForm(variantKey);
                if (armamentForm == null)
                {
                    config = null;
                    return false;
                }

                config = ChipConfigurationMergeService.CloneConfig(config);
                ChipArmamentFormApplicationService.Apply(config, armamentForm);
            }

            reference = new ChipSourceReferenceSnapshot
            {
                OrderedSourceKeys = new List<string> { sourceKey }.AsReadOnly(),
                SourceProfessionKey = string.IsNullOrWhiteSpace(variantKey)
                    ? function.profession?.defName
                    : professionKey,
                SourceVariantKey = armamentForm?.defName
            };
            return true;
        }
    }
}
