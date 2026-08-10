using System.Collections.Generic;
using BDP.Core.Requirements;
using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技定义层解释后的正式总契约。
    /// 它只承载正式声明结果，不直接承担匹配和攻击执行。
    /// </summary>
    internal sealed class ComboDefinitionContract
    {
        /// <summary>
        /// 当前契约对应的 ComboDef。
        /// </summary>
        public ComboDef Definition;

        /// <summary>
        /// 当前契约对应的统一配置镜像。
        /// </summary>
        public ComboDefinitionConfig Config;

        /// <summary>
        /// 来源芯片 A 的 DefName。
        /// </summary>
        public string ChipADefName;

        /// <summary>
        /// 来源芯片 B 的 DefName。
        /// </summary>
        public string ChipBDefName;

        /// <summary>
        /// 使用整个组合技前必须持续满足的有序角色条件。
        /// </summary>
        public IReadOnlyList<PawnRequirement> UseRequirements;

        /// <summary>
        /// 当前组合技的表达声明句柄。
        /// </summary>
        public ComboExpressionContractHandle Expression;

    }
}
