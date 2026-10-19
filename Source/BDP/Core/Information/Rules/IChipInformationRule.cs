using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息通用规则契约。
    /// 规则只能针对中性能力和数据结构，不能针对单一芯片身份。
    /// </summary>
    public interface IChipInformationRule
    {
        /// <summary>规则稳定键。</summary>
        string RuleKey { get; }

        /// <summary>判断当前输入是否具备本规则所需的通用能力。</summary>
        bool CanApply(ChipInformationRuleContext context);

        /// <summary>声明本规则可公开的信息块和字段。</summary>
        IReadOnlyList<ChipInformationBlockSchema> DescribeSchema(
            ChipInformationRuleContext context);

        /// <summary>解析本规则实际存在的字段值。</summary>
        IReadOnlyList<ChipInformationContribution> Resolve(
            ChipInformationRuleContext context);
    }
}
