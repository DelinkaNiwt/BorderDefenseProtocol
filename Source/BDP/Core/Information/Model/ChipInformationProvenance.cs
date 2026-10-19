namespace BDP.Core.Information
{
    /// <summary>
    /// 单个字段来源链中的一条贡献记录。
    /// </summary>
    public sealed class ChipInformationProvenance
    {
        /// <summary>稳定来源键。</summary>
        public string SourceKey { get; private set; }

        /// <summary>来源类别。</summary>
        public ChipInformationSourceKind SourceKind { get; private set; }

        /// <summary>贡献方式。</summary>
        public ChipInformationContributionKind ContributionKind { get; private set; }

        /// <summary>该来源贡献的值。</summary>
        public ChipInformationValue Value { get; private set; }

        /// <summary>采用的运算符，例如替换、加法或倍率。</summary>
        public string Operation { get; private set; }

        /// <summary>该贡献是否实际参与最终值。</summary>
        public bool Applied { get; private set; }

        /// <summary>该来源值自身的单位语言键；没有单独单位时为空。</summary>
        public string UnitKey { get; private set; }

        /// <summary>创建来源贡献记录。</summary>
        public ChipInformationProvenance(
            string sourceKey,
            ChipInformationSourceKind sourceKind,
            ChipInformationContributionKind contributionKind,
            ChipInformationValue value,
            string operation,
            bool applied,
            string unitKey = null)
        {
            SourceKey = sourceKey;
            SourceKind = sourceKind;
            ContributionKind = contributionKind;
            Value = value;
            Operation = operation;
            Applied = applied;
            UnitKey = unitKey;
        }
    }
}
