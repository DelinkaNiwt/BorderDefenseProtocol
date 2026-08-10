using System.Collections.Generic;
using BDP.Core.CombatBody;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Bootstrap
{
    /// <summary>
    /// Pawn 战斗体宿主接线器。
    /// 它只负责把主链宿主 comp 接到人形 Pawn 的 `ThingDef.comps` 上，Hediff 不再承担主链宿主职责。
    /// </summary>
    internal static class PawnCombatBodyCompInjector
    {
        /// <summary>
        /// 防止重复执行。
        /// </summary>
        private static bool applied;

        /// <summary>
        /// 把战斗体主链宿主注入所有人形 Pawn Def。
        /// 这里只做启动接线，不塞入玩法判定。
        /// </summary>
        public static void Apply()
        {
            if (applied)
            {
                return;
            }

            applied = true;

            int injectedCount = 0;
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!ShouldInject(thingDef) || HasCombatBodyHostComp(thingDef))
                {
                    continue;
                }

                if (thingDef.comps == null)
                {
                    thingDef.comps = new List<CompProperties>();
                }

                thingDef.comps.Add(new CompProperties_CombatBodyHost());
                injectedCount++;
            }

            BdpDiagnostics.Once(
                "combatbody.injector.applied",
                "PawnCombatBodyCompInjector 已为 " + injectedCount + " 个人形 Pawn ThingDef 接入战斗体主链宿主。");
        }

        /// <summary>
        /// 当前只给人形 Pawn 接入战斗体宿主。
        /// </summary>
        private static bool ShouldInject(ThingDef thingDef)
        {
            return thingDef != null
                   && thingDef.category == ThingCategory.Pawn
                   && thingDef.race != null
                   && thingDef.race.Humanlike;
        }

        /// <summary>
        /// 检查当前 Pawn Def 是否已经带有战斗体宿主 Comp。
        /// </summary>
        private static bool HasCombatBodyHostComp(ThingDef thingDef)
        {
            if (thingDef.comps == null)
            {
                return false;
            }

            for (int i = 0; i < thingDef.comps.Count; i++)
            {
                if (thingDef.comps[i] is CompProperties_CombatBodyHost)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
