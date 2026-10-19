namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息模型的身份元数据。
    /// </summary>
    public sealed class ChipInformationIdentity
    {
        /// <summary>芯片实例稳定键，可为空。</summary>
        public string InstanceKey { get; private set; }

        /// <summary>芯片定义稳定键，可为空。</summary>
        public string DefinitionKey { get; private set; }

        /// <summary>芯片显示名称语言键，可为空。</summary>
        public string DisplayLabelKey { get; private set; }

        /// <summary>芯片实例来源的最终职业键，可为空。</summary>
        public string SourceProfessionKey { get; private set; }

        /// <summary>创建身份元数据。</summary>
        public ChipInformationIdentity(
            string instanceKey,
            string definitionKey,
            string displayLabelKey,
            string sourceProfessionKey = null)
        {
            InstanceKey = instanceKey;
            DefinitionKey = definitionKey;
            DisplayLabelKey = displayLabelKey;
            SourceProfessionKey = sourceProfessionKey;
        }
    }
}
