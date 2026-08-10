using System.Collections.Generic;

namespace BDP.Core.Requirements
{
    /// <summary>
    /// 一组角色条件的有序只读检查结果。
    /// </summary>
    public sealed class PawnRequirementCheckResult
    {
        /// <summary>全部条件是否都满足。</summary>
        public bool Satisfied { get; internal set; }

        /// <summary>按 XML 声明顺序保存的全部条件快照。</summary>
        public IReadOnlyList<PawnRequirementSnapshot> Requirements { get; internal set; }

        /// <summary>按 XML 声明顺序保存的全部失败条件快照。</summary>
        public IReadOnlyList<PawnRequirementSnapshot> Failures { get; internal set; }
    }
}
