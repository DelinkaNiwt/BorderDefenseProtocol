using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>解析 Trion（能量资源）能力字段。</summary>
    internal sealed class TrionInformationRule : FactInformationRuleBase
    {
        /// <summary>规则键。</summary>
        public override string RuleKey => "trion-fields";

        /// <summary>信息块键。</summary>
        protected override string BlockKey => "trion";

        /// <summary>信息块类型。</summary>
        protected override string BlockKind => "trion";

        /// <summary>信息块标题键。</summary>
        protected override string BlockLabelKey => "BDP_Info_Block_Trion";

        /// <summary>信息块顺序。</summary>
        protected override int BlockOrder => 30;

        /// <summary>取得 Trion 事实。</summary>
        protected override IEnumerable<ChipInformationFactValue> GetFields(
            ChipInformationFacts facts)
        {
            return facts.TrionFields;
        }
    }
}
