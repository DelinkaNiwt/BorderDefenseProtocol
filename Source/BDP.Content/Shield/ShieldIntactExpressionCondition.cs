using BDP.Core.Expressions;
using Verse;

namespace BDP.Content.Shield
{
    /// <summary>防护、盾面与盾击共用的来源条件；姿态惩罚不声明此条件。</summary>
    public sealed class ShieldIntactExpressionCondition : ExpressionSourceConditionConfig
    {
        /// <summary>与防护组件定义一致的共享耐久组键。</summary>
        public string GroupKey;
        /// <summary>读取人物盾组事实，不在核心层登记业务字符串。</summary>
        public override bool? Evaluate(Pawn pawn, Thing sourceChip) { return EnergyShieldApi.IsIntact(pawn, GroupKey); }
    }
}
