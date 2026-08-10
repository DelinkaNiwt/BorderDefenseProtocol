using System.Collections.Generic;
using Verse;

namespace BDP.Content.Trion.Talent
{
    /// <summary>
    /// Content 侧 Trion 天赋检测宿主接线器。
    /// </summary>
    internal static class PawnTrionTalentAssessmentInjector
    {
        /// <summary>
        /// 防止重复注入。
        /// </summary>
        private static bool applied;

        /// <summary>
        /// 将检测状态组件注入全部人形 Pawn Def。
        /// </summary>
        public static void Apply()
        {
            if (applied)
            {
                return;
            }

            applied = true;
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!ShouldInject(thingDef) || HasAssessmentComp(thingDef))
                {
                    continue;
                }

                if (thingDef.comps == null)
                {
                    thingDef.comps = new List<CompProperties>();
                }

                thingDef.comps.Add(new CompProperties_TrionTalentAssessment());
            }
        }

        /// <summary>
        /// 当前检测业务只对人形 Pawn 提供角色状态。
        /// </summary>
        private static bool ShouldInject(ThingDef thingDef)
        {
            return thingDef != null
                   && thingDef.category == ThingCategory.Pawn
                   && thingDef.race != null
                   && thingDef.race.Humanlike;
        }

        /// <summary>
        /// 判断指定 Pawn Def 是否已经接入检测状态组件。
        /// </summary>
        private static bool HasAssessmentComp(ThingDef thingDef)
        {
            if (thingDef.comps == null)
            {
                return false;
            }

            for (int i = 0; i < thingDef.comps.Count; i++)
            {
                if (thingDef.comps[i] is CompProperties_TrionTalentAssessment)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
