using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 输入事实中的一个类型化字段。
    /// </summary>
    public sealed class ChipInformationFactValue
    {
        /// <summary>稳定字段键。</summary>
        public string FieldKey { get; private set; }

        /// <summary>字段语言包键。</summary>
        public string LabelKey { get; private set; }

        /// <summary>字段值类型。</summary>
        public ChipInformationValueKind ValueKind { get; private set; }

        /// <summary>字段类型化值。</summary>
        public ChipInformationValue Value { get; private set; }

        /// <summary>字段单位语言包键。</summary>
        public string UnitKey { get; private set; }

        /// <summary>字段来源链。</summary>
        public IReadOnlyList<ChipInformationProvenance> Provenance { get; private set; }

        /// <summary>创建一个输入事实字段。</summary>
        public ChipInformationFactValue(
            string fieldKey,
            string labelKey,
            ChipInformationValueKind valueKind,
            ChipInformationValue value,
            string unitKey,
            IEnumerable<ChipInformationProvenance> provenance)
        {
            FieldKey = fieldKey;
            LabelKey = labelKey;
            ValueKind = valueKind;
            Value = value;
            UnitKey = unitKey;
            Provenance = Copy(provenance);
        }

        /// <summary>复制来源链。</summary>
        private static IReadOnlyList<ChipInformationProvenance> Copy(
            IEnumerable<ChipInformationProvenance> source)
        {
            List<ChipInformationProvenance> result = new List<ChipInformationProvenance>();
            if (source != null)
            {
                foreach (ChipInformationProvenance item in source)
                {
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result.AsReadOnly();
        }
    }
}
