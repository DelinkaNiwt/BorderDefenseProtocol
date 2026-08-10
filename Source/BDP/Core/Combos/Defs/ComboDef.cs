using System.Collections.Generic;
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
        /// 组合技来源芯片 A 的 DefName。
        /// </summary>
        public string chipA;

        /// <summary>
        /// 组合技来源芯片 B 的 DefName。
        /// </summary>
        public string chipB;

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
        /// 把当前 Def 表面写法收拢成统一配置对象。
        /// 读取器和解释器统一消费这份结构，避免各层自己散读字段。
        /// </summary>
        internal ComboDefinitionConfig ToConfig()
        {
            return new ComboDefinitionConfig
            {
                chipA = chipA,
                chipB = chipB,
                UseRequirements = UseRequirements != null
                    ? new List<PawnRequirement>(UseRequirements)
                    : new List<PawnRequirement>(),
                Expression = Expression
            };
        }
    }
}
