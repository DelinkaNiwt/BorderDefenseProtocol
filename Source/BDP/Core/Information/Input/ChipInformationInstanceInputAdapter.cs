using System.Collections.Generic;
using BDP.Core.Chips;
using Verse;

namespace BDP.Core.Information
{
    /// <summary>
    /// 将芯片实例的当前定义、基础定义和来源身份合成为统一解析输入。
    /// </summary>
    public static class ChipInformationInstanceInputAdapter
    {
        /// <summary>尝试创建一份芯片实例信息输入。</summary>
        public static bool TryCreate(
            Thing chip,
            ChipInformationRuntimeState runtime,
            out ChipInformationParseInput input)
        {
            input = null;
            ChipDefinitionConfig effectiveDefinition;
            if (!ChipInstanceSurfaceAccess.TryGetDefinition(chip, out effectiveDefinition)
                || effectiveDefinition == null)
            {
                return false;
            }

            ChipSourceReferenceSnapshot source =
                ChipInstanceSurfaceAccess.ReadSourceReference(chip);
            ChipInformationIdentity identity = new ChipInformationIdentity(
                chip.ThingID,
                chip.def?.defName,
                null,
                source?.SourceProfessionKey);
            ChipInformationRuntimeState effectiveRuntime = runtime
                ?? ChipInformationRuntimeInputAdapter.CreateStatic();
            ChipInformationParseInput effectiveInput =
                ChipInformationDefinitionInputAdapter.Create(
                    effectiveDefinition,
                    identity,
                    effectiveRuntime,
                    null);

            ChipDefinitionConfig declaredDefinition;
            if (!ChipInstanceSurfaceAccess.TryGetDeclaredDefinition(
                    chip,
                    out declaredDefinition)
                || declaredDefinition == null)
            {
                input = effectiveInput;
                return true;
            }

            ChipInformationParseInput declaredInput =
                ChipInformationDefinitionInputAdapter.Create(
                    declaredDefinition,
                    identity,
                    effectiveRuntime,
                    BuildInstanceProvenance(source));
            ChipInformationParseInput effectiveOverlay =
                ChipInformationInputComposer.ReclassifySources(
                    effectiveInput,
                    ChipInformationSourceKind.EffectiveDefinition,
                    ChipInformationContributionKind.Replacement,
                    "instance.effective");
            input = ChipInformationInputComposer.Combine(
                declaredInput,
                effectiveOverlay);
            return true;
        }

        /// <summary>把实例来源记录转换为通用来源链。</summary>
        private static IReadOnlyList<ChipInformationProvenance> BuildInstanceProvenance(
            ChipSourceReferenceSnapshot source)
        {
            List<ChipInformationProvenance> result =
                new List<ChipInformationProvenance>();
            if (source == null)
            {
                return result;
            }

            if (!string.IsNullOrWhiteSpace(source.SourceProfessionKey))
            {
                result.Add(new ChipInformationProvenance(
                    source.SourceProfessionKey,
                    ChipInformationSourceKind.BaseDefinition,
                    ChipInformationContributionKind.Base,
                    ChipInformationValue.FromEnum(source.SourceProfessionKey),
                    null,
                    true));
            }

            if (source.OrderedSourceKeys != null)
            {
                for (int index = 0; index < source.OrderedSourceKeys.Count; index++)
                {
                    string sourceKey = source.OrderedSourceKeys[index];
                    if (!string.IsNullOrWhiteSpace(sourceKey))
                    {
                        result.Add(new ChipInformationProvenance(
                            sourceKey,
                            ChipInformationSourceKind.BaseDefinition,
                            ChipInformationContributionKind.Base,
                            ChipInformationValue.FromEnum(sourceKey),
                            null,
                            true));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(source.SourceVariantKey))
            {
                result.Add(new ChipInformationProvenance(
                    source.SourceVariantKey,
                    ChipInformationSourceKind.Variant,
                    ChipInformationContributionKind.Variant,
                    ChipInformationValue.FromEnum(source.SourceVariantKey),
                    null,
                    true));
            }

            return result;
        }
    }
}
