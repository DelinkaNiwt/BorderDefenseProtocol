using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>解析装配与装载能力字段。</summary>
    internal sealed class LoadoutInformationRule : FactInformationRuleBase
    {
        /// <summary>规则键。</summary>
        public override string RuleKey => "loadout-fields";

        /// <summary>信息块键。</summary>
        protected override string BlockKey => "loadout";

        /// <summary>信息块类型。</summary>
        protected override string BlockKind => "loadout";

        /// <summary>信息块标题键。</summary>
        protected override string BlockLabelKey => "BDP_Info_Block_Loadout";

        /// <summary>信息块顺序。</summary>
        protected override int BlockOrder => 20;

        /// <summary>取得装载事实。</summary>
        protected override IEnumerable<ChipInformationFactValue> GetFields(
            ChipInformationFacts facts)
        {
            return facts.LoadoutFields;
        }
    }
}
