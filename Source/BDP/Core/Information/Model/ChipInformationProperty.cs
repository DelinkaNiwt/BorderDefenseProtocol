using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 统一信息节点上的类型化客观属性；Value 始终表示正式来源给出的最终值。
    /// </summary>
    public sealed class ChipInformationProperty
    {
        /// <summary>属性稳定键。</summary>
        public string Key { get; private set; }

        /// <summary>属性名称语言键；没有名称时为空。</summary>
        public string LabelKey { get; private set; }

        /// <summary>属性值的稳定类型。</summary>
        public ChipInformationValueKind ValueKind { get; private set; }

        /// <summary>正式来源给出的最终类型化值。</summary>
        public ChipInformationValue Value { get; private set; }

        /// <summary>属性单位语言键；没有单位时为空。</summary>
        public string UnitKey { get; private set; }

        /// <summary>基础值及原始构型来源值；不包含计算出的差值。</summary>
        public IReadOnlyList<ChipInformationProvenance> Sources { get; private set; }

        /// <summary>创建只包含客观类型化值及来源的不可变信息属性。</summary>
        public ChipInformationProperty(
            string key,
            string labelKey,
            ChipInformationValueKind valueKind,
            ChipInformationValue value,
            string unitKey,
            IEnumerable<ChipInformationProvenance> sources)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Key = key;
            LabelKey = labelKey;
            ValueKind = valueKind;
            Value = value;
            UnitKey = unitKey;
            Sources = Copy(sources);
        }

        /// <summary>复制来源集合并过滤空项。</summary>
        private static IReadOnlyList<ChipInformationProvenance> Copy(
            IEnumerable<ChipInformationProvenance> source)
        {
            List<ChipInformationProvenance> result =
                new List<ChipInformationProvenance>();
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
