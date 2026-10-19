using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>解析画像能力字段。</summary>
    internal sealed class ProfileInformationRule : FactInformationRuleBase
    {
        /// <summary>规则键。</summary>
        public override string RuleKey => "profile-fields";

        /// <summary>信息块键。</summary>
        protected override string BlockKey => "profile";

        /// <summary>信息块类型。</summary>
        protected override string BlockKind => "profile";

        /// <summary>信息块标题键。</summary>
        protected override string BlockLabelKey => "BDP_Info_Block_Profile";

        /// <summary>信息块顺序。</summary>
        protected override int BlockOrder => 10;

        /// <summary>取得画像事实。</summary>
        protected override IEnumerable<ChipInformationFactValue> GetFields(
            ChipInformationFacts facts)
        {
            return facts.ProfileFields;
        }
    }
}
