using System;
using BDP.Core.Requirements;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>一种角色条件参与双动作合并时的显式规则。</summary>
    public interface IChipRequirementMergeRule
    {
        /// <summary>当前规则负责的条件具体类型。</summary>
        Type RequirementType { get; }

        /// <summary>判断两项是否属于同一个需要折叠的条件槽。</summary>
        bool BelongsToSameSlot(PawnRequirement first, PawnRequirement second);

        /// <summary>把同槽条件合成一项全新的结果。</summary>
        PawnRequirement Merge(PawnRequirement first, PawnRequirement second);
    }
}
