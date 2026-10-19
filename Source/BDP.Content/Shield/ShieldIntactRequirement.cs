using BDP.Core.Requirements;
using Verse;

namespace BDP.Content.Shield
{
    /// <summary>推进等组合能力复用的角色条件；按钮、AI、目标确认和执行读取同一盾组。</summary>
    public sealed class ShieldIntactRequirement : PawnRequirement
    {
        /// <summary>能力要求完好的盾组。</summary>
        public string GroupKey;
        /// <summary>条件名称来自语言包。</summary>
        public override string Label { get { return "BDP_Shield_RequireIntact".Translate(); } }
        /// <summary>静态要求说明。</summary>
        public override PawnRequirementSnapshot Describe()
        { return PawnRequirementSnapshot.Description(Label, "BDP_Shield_Intact".Translate()); }
        /// <summary>定义必须提供明确的共享组键。</summary>
        public override string ValidateDefinition()
        { return string.IsNullOrWhiteSpace(GroupKey) ? "BDP_Shield_GroupMissing".Translate().ToString() : null; }
        /// <summary>实时检查护盾，不复制冷却或耐久算法。</summary>
        public override PawnRequirementSnapshot Evaluate(Pawn pawn)
        {
            bool intact = EnergyShieldApi.IsIntact(pawn, GroupKey);
            return PawnRequirementSnapshot.Evaluation(Label,
                (intact ? "BDP_Shield_Intact" : "BDP_Shield_Broken").Translate(),
                "BDP_Shield_Intact".Translate(), intact,
                intact ? null : "BDP_Shield_RequireIntactFailure".Translate().ToString());
        }
    }
}
