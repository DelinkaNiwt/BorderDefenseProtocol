using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 来源 Worker 返回的一项类型化功能属性贡献。
    /// </summary>
    public sealed class ChipFeatureValueContribution
    {
        /// <summary>贡献对应的功能属性 Def。</summary>
        public ChipFeaturePropertyDef Property { get; private set; }

        /// <summary>贡献的类型化值。</summary>
        public ChipInformationValue Value { get; private set; }

        /// <summary>贡献所在的值层级。</summary>
        public ChipFeatureValueLayer Layer { get; private set; }

        /// <summary>贡献的来源链记录。</summary>
        public IReadOnlyList<ChipInformationProvenance> Provenance { get; private set; }

        /// <summary>创建类型化功能属性贡献。</summary>
        public ChipFeatureValueContribution(
            ChipFeaturePropertyDef property,
            ChipInformationValue value,
            ChipFeatureValueLayer layer,
            IEnumerable<ChipInformationProvenance> provenance)
        {
            Property = property;
            Value = value;
            Layer = layer;
            List<ChipInformationProvenance> result =
                new List<ChipInformationProvenance>();
            if (provenance != null)
            {
                foreach (ChipInformationProvenance item in provenance)
                {
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            Provenance = result.AsReadOnly();
        }
    }
}
