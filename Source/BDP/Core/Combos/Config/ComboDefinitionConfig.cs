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
        /// 组合技来源芯片 A 的 DefName。
        /// </summary>
        public string chipA;

        /// <summary>
        /// 组合技来源芯片 B 的 DefName。
        /// </summary>
        public string chipB;

        /// <summary>
        /// 使用整个组合技前必须满足的有序角色条件镜像。
        /// </summary>
        public List<PawnRequirement> UseRequirements;

        /// <summary>
        /// 组合技表达声明块。
        /// </summary>
        public ComboExpressionConfig Expression;
    }
}
