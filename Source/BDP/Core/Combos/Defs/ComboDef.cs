using System.Collections.Generic;
using BDP.Core.Information;
using BDP.Core.Requirements;
using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技正式定义。
    /// 它表示由两枚芯片匹配出来的组合表达定义。
    /// </summary>
    public sealed class ComboDef : Def
    {
        /// <summary>
        /// 组合技第一来源芯片功能模板的 DefName。
        /// </summary>
        public string firstSourceKey;

        /// <summary>
        /// 组合技第二来源芯片功能模板的 DefName。
        /// </summary>
        public string secondSourceKey;

        /// <summary>第一来源项的可选成品身份准入规则。</summary>
        public ComboSourceAdmissionConfig FirstSourceAdmission;

        /// <summary>第二来源项的可选成品身份准入规则。</summary>
        public ComboSourceAdmissionConfig SecondSourceAdmission;

        /// <summary>第一来源项的可选运行时形态与姿态范围。</summary>
        public ComboSourceScopeConfig FirstSourceScope;

        /// <summary>第二来源项的可选运行时形态与姿态范围。</summary>
        public ComboSourceScopeConfig SecondSourceScope;

        /// <summary>
        /// 使用整个组合技前必须持续满足的有序角色条件。
        /// 空列表表示该组合技没有额外角色门槛。
        /// </summary>
        public List<PawnRequirement> UseRequirements;

        /// <summary>
        /// 组合技表达声明块。
        /// </summary>
        public ComboExpressionConfig Expression;

        /// <summary>
        /// 组合技最终结果的功能语义声明。
        /// </summary>
        public List<ChipFeatureDeclaration> Features;

        /// <summary>
        /// 是否要求第一、第二来源项使用同一来源变体。
        /// 默认启用，避免组合结果在来源构型不一致时失去确定语义。
        /// </summary>
        public bool RequireSameSourceVariant = true;

        /// <summary>
        /// 把当前 Def 表面写法收拢成统一配置对象。
        /// 读取器和解释器统一消费这份结构，避免各层自己散读字段。
        /// </summary>
        internal ComboDefinitionConfig ToConfig()
        {
            return new ComboDefinitionConfig
            {
                firstSourceKey = firstSourceKey,
                secondSourceKey = secondSourceKey,
                FirstSourceAdmission = FirstSourceAdmission,
                SecondSourceAdmission = SecondSourceAdmission,
                FirstSourceScope = FirstSourceScope,
                SecondSourceScope = SecondSourceScope,
                RequireSameSourceVariant = RequireSameSourceVariant,
                UseRequirements = UseRequirements != null
                    ? new List<PawnRequirement>(UseRequirements)
                    : new List<PawnRequirement>(),
                Expression = Expression,
                Features = Features != null
                    ? new List<ChipFeatureDeclaration>(Features)
                    : new List<ChipFeatureDeclaration>()
            };
        }
    }
}
