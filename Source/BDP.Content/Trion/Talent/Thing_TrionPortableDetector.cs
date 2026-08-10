using System.Collections.Generic;
using Verse;

namespace BDP.Content.Trion.Talent
{
    /// <summary>
    /// 检测成功后消耗一个的一次性便携 Trion 天赋检测器。
    /// </summary>
    public sealed class Thing_TrionPortableDetector : ThingWithComps
    {
        /// <summary>向原版右键菜单追加统一检测入口。</summary>
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            foreach (FloatMenuOption option in TrionTalentAssessmentFloatMenuUtility.BuildOptions(selPawn, this))
            {
                yield return option;
            }
        }
    }
}
