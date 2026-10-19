using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息规则注册表。
    /// </summary>
    public sealed class ChipInformationRuleRegistry
    {
        /// <summary>已注册的规则集合。</summary>
        private readonly List<IChipInformationRule> rules =
            new List<IChipInformationRule>();

        /// <summary>按稳定注册顺序读取规则。</summary>
        public IReadOnlyList<IChipInformationRule> Rules => rules;

        /// <summary>注册一条通用信息规则。</summary>
        public void Register(IChipInformationRule rule)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.RuleKey))
            {
                throw new ArgumentException("信息规则必须提供稳定 RuleKey。", nameof(rule));
            }

            for (int index = 0; index < rules.Count; index++)
            {
                if (string.Equals(rules[index].RuleKey, rule.RuleKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "重复注册芯片信息规则：" + rule.RuleKey);
                }
            }

            rules.Add(rule);
        }
    }
}
