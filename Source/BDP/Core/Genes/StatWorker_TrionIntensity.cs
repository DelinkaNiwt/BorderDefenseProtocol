using RimWorld;
using Verse;

namespace BDP.Core.Genes
{
    /// <summary>
    /// 控制 Trion 释放力在原版角色属性页中的显示资格。
    /// </summary>
    public sealed class StatWorker_TrionIntensity : StatWorker
    {
        /// <summary>
        /// 只向具有有效 Trion 腺体的人形角色显示正式释放力属性。
        /// </summary>
        public override bool ShouldShowFor(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            return pawn != null
                && pawn.RaceProps.Humanlike
                && TrionGlandEligibility.HasActiveTrionGland(pawn)
                && base.ShouldShowFor(req);
        }
    }
}
