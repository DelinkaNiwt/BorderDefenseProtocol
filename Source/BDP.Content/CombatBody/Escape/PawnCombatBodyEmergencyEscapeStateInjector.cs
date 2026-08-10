using System.Collections.Generic;
using Verse;

namespace BDP.Content.CombatBody.Escape
{
    /// <summary>
    /// 为人形 Pawn 注入 Content 侧紧急脱离缓存组件。
    /// </summary>
    internal static class PawnCombatBodyEmergencyEscapeStateInjector
    {
        /// <summary>
        /// 防止重复注入。
        /// </summary>
        private static bool applied;

        /// <summary>
        /// 将紧急脱离缓存组件注入所有人形 Pawn Def。
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
                if (!ShouldInject(thingDef) || HasStateComp(thingDef))
                {
                    continue;
                }

                if (thingDef.comps == null)
                {
                    thingDef.comps = new List<CompProperties>();
                }

                thingDef.comps.Add(new CompProperties_CombatBodyEmergencyEscapeState());
            }
        }

        /// <summary>
        /// 判断指定 Def 是否为人形 Pawn。
        /// </summary>
        private static bool ShouldInject(ThingDef thingDef)
        {
            return thingDef != null
                && thingDef.category == ThingCategory.Pawn
                && thingDef.race != null
                && thingDef.race.Humanlike;
        }

        /// <summary>
        /// 判断指定 Def 是否已经拥有缓存组件。
        /// </summary>
        private static bool HasStateComp(ThingDef thingDef)
        {
            if (thingDef.comps == null)
            {
                return false;
            }

            for (int i = 0; i < thingDef.comps.Count; i++)
            {
                if (thingDef.comps[i] is CompProperties_CombatBodyEmergencyEscapeState)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
