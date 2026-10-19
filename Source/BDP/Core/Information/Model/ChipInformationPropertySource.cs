using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 一条可关联到功能属性的客观来源事实。
    /// </summary>
    public sealed class ChipInformationPropertySource
    {
        /// <summary>目标功能属性稳定键。</summary>
        public string PropertyKey { get; private set; }

        /// <summary>目标属性的原始来源记录。</summary>
        public ChipInformationProvenance Provenance { get; private set; }

        /// <summary>该构型字段作用到的原始来源类别；空集合表示不限定。</summary>
        public IReadOnlyList<ChipInformationSourceKind> ApplicableSourceKinds { get; private set; }

        /// <summary>创建一条功能属性来源事实。</summary>
        public ChipInformationPropertySource(
            string propertyKey,
            ChipInformationProvenance provenance,
            IEnumerable<ChipInformationSourceKind> applicableSourceKinds = null)
        {
            PropertyKey = propertyKey;
            Provenance = provenance;
            List<ChipInformationSourceKind> sourceKinds =
                new List<ChipInformationSourceKind>();
            if (applicableSourceKinds != null)
            {
                foreach (ChipInformationSourceKind sourceKind in applicableSourceKinds)
                {
                    if (!sourceKinds.Contains(sourceKind))
                    {
                        sourceKinds.Add(sourceKind);
                    }
                }
            }

            ApplicableSourceKinds = sourceKinds.AsReadOnly();
        }
    }
}
