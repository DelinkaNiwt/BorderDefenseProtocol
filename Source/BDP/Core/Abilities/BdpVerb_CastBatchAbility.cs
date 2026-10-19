using System.Collections.Generic;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>按已确认目标数扣费，然后继续既有正式能力施放链。</summary>
    public class BdpVerb_CastBatchAbility : BdpVerb_CastAbility
    {
        /// <summary>生成或扣费之前复查完整批次，不能只检查代表格。</summary>
        protected override bool TryCastShot()
        {
            var batch = ability as BdpBatchAbility;
            List<IntVec3> targets;
            string reason;
            if (batch == null || !batch.TryValidateBatch(batch.CurrentBatchTargets, out targets, out reason))
                return false;
            return batch.ExecutePreparedCast(targets, () => base.TryCastShot());
        }

        /// <summary>同一轮只提交一次总费用，禁止再叠加单次扣费。</summary>
        protected override bool TryCommitTrionCosts()
        {
            var batch = ability as BdpBatchAbility;
            return batch != null && BdpAbilityTrionCostCommitter.TryCommit(batch, batch.CurrentBatchTargets.Count);
        }
    }
}
