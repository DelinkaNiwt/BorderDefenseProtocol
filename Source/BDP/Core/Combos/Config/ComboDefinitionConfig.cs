using System.Collections.Generic;
using BDP.Core.Requirements;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技定义的统一配置镜像。
    /// 它只收拢 ComboDef 的正式输入字段，不承担运行时匹配。
    /// </summary>
    internal sealed class ComboDefinitionConfig
    {
        /// <summary>
        /// 组合技第一来源动作预设的 DefName。
        /// </summary>
        public string firstSourceActionDefName;

        /// <summary>
        /// 组合技第二来源动作预设的 DefName。
        /// </summary>
        public string secondSourceActionDefName;

        /// <summary>第一来源项的可选成品身份准入规则。</summary>
        public ComboSourceAdmissionConfig FirstSourceAdmission;

        /// <summary>第二来源项的可选成品身份准入规则。</summary>
        public ComboSourceAdmissionConfig SecondSourceAdmission;

        /// <summary>
        /// 使用整个组合技前必须满足的有序角色条件镜像。
        /// </summary>
        public List<PawnRequirement> UseRequirements;

        /// <summary>
        /// 组合技表达声明块。
        /// </summary>
        public ComboExpressionConfig Expression;

        /// <summary>
        /// 是否要求第一、第二来源项使用同一来源变体。
        /// </summary>
        public bool RequireSameSourceVariant;
    }
}
