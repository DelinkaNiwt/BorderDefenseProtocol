namespace BDP.Core.Information
{
    /// <summary>结构化信息值中的一个命名字段。</summary>
    public sealed class ChipInformationValueField
    {
        /// <summary>字段的稳定机器键。</summary>
        public string Key { get; private set; }

        /// <summary>字段名称的可选语言键。</summary>
        public string LabelKey { get; private set; }

        /// <summary>字段值的可选单位语言键。</summary>
        public string UnitKey { get; private set; }

        /// <summary>字段的类型化值。</summary>
        public ChipInformationValue Value { get; private set; }

        /// <summary>创建只读命名字段。</summary>
        public ChipInformationValueField(
            string key,
            ChipInformationValue value,
            string labelKey = null,
            string unitKey = null)
        {
            Key = key;
            Value = value;
            LabelKey = labelKey;
            UnitKey = unitKey;
        }
    }
}
