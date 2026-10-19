using System.Collections.Generic;
using BDP.Core.Information;
using Verse;

namespace BDP.Development.Information.Samples
{
    /// <summary>第三方自定义护盾路线的最小 Worker 样例。</summary>
    public sealed class CustomBarrierFeatureSourceWorker : IChipFeatureSourceWorker
    {
        /// <summary>读取自定义路线提供的反射比例属性。</summary>
        public IReadOnlyList<ChipFeatureValueContribution> Resolve(
            ChipFeatureSourceConfig source,
            ChipFeatureReadContext context)
        {
            CustomBarrierFeatureSourceConfig custom = source as CustomBarrierFeatureSourceConfig;
            List<ChipFeatureValueContribution> result = new List<ChipFeatureValueContribution>();
            CustomBarrierRuleDef rule = custom == null || string.IsNullOrWhiteSpace(custom.RuleDefName)
                ? null
                : DefDatabase<CustomBarrierRuleDef>.GetNamedSilentFail(custom.RuleDefName);
            ChipFeaturePropertyDef property = DefDatabase<ChipFeaturePropertyDef>.GetNamedSilentFail(
                rule?.PropertyDefName);
            if (custom == null || rule == null || property == null)
            {
                return result.AsReadOnly();
            }

            ChipInformationValue value = ChipInformationValue.FromPercentage(rule.ReflectionRatio);
            result.Add(new ChipFeatureValueContribution(
                property,
                value,
                ChipFeatureValueLayer.DeclaredDefinition,
                new[]
                {
                    new ChipInformationProvenance(
                        custom.Id,
                        ChipInformationSourceKind.Extension,
                        ChipInformationContributionKind.Base,
                        value,
                        null,
                        true)
                }));
            return result.AsReadOnly();
        }
    }
}
