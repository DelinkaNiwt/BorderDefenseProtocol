namespace BDP.Core.Information
{
    /// <summary>一条芯片条件声明的原始类型化事实。</summary>
    public sealed class ChipInformationConditionFact
    {
        /// <summary>该条件实例的稳定键。</summary>
        public string ConditionKey { get; private set; }

        /// <summary>条件类型的稳定键。</summary>
        public string ConditionTypeKey { get; private set; }

        /// <summary>条件名称语言键。</summary>
        public string LabelKey { get; private set; }

        /// <summary>作者声明的条件门槛原始值。</summary>
        public ChipInformationValue RequiredValue { get; private set; }

        /// <summary>条件门槛单位语言键。</summary>
        public string RequiredUnitKey { get; private set; }

        /// <summary>条件引用的定义对象；没有引用时为空。</summary>
        public ChipInformationValue ReferenceValue { get; private set; }

        /// <summary>创建原始条件事实。</summary>
        public ChipInformationConditionFact(
            string conditionKey,
            string conditionTypeKey,
            string labelKey,
            ChipInformationValue requiredValue,
            string requiredUnitKey = null,
            ChipInformationValue referenceValue = null)
        {
            ConditionKey = conditionKey;
            ConditionTypeKey = conditionTypeKey;
            LabelKey = labelKey;
            RequiredValue = requiredValue;
            RequiredUnitKey = requiredUnitKey;
            ReferenceValue = referenceValue;
        }
    }
}
