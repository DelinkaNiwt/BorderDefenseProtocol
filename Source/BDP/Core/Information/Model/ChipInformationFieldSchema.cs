namespace BDP.Core.Information
{
    /// <summary>
    /// 一个公开信息字段的结构声明。
    /// </summary>
    public sealed class ChipInformationFieldSchema
    {
        /// <summary>稳定字段键。</summary>
        public string FieldKey { get; private set; }

        /// <summary>字段语言包键。</summary>
        public string LabelKey { get; private set; }

        /// <summary>字段值类型。</summary>
        public ChipInformationValueKind ValueKind { get; private set; }

        /// <summary>字段单位语言包键，可为空。</summary>
        public string UnitKey { get; private set; }

        /// <summary>字段是否允许在当前没有值时仍保留在 Schema 中。</summary>
        public bool IsOptional { get; private set; }

        /// <summary>创建字段声明。</summary>
        public ChipInformationFieldSchema(
            string fieldKey,
            string labelKey,
            ChipInformationValueKind valueKind,
            string unitKey,
            bool isOptional)
        {
            FieldKey = fieldKey;
            LabelKey = labelKey;
            ValueKind = valueKind;
            UnitKey = unitKey;
            IsOptional = isOptional;
        }
    }
}
