using System.Collections.Generic;
using BDP.Core.Trion;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Bootstrap
{
    /// <summary>
    /// Pawn Trion 宿主接线器。
    /// 它只负责把 `CompTrion` 接到人形 Pawn 的原版 `ThingDef.comps` 上，
    /// 让资源真值继续落在 Pawn `ThingComp` 宿主，而不是新造平行载体。
    /// </summary>
    internal static class PawnTrionCompInjector
    {
        /// <summary>
        /// 防止重复执行。
        /// </summary>
        private static bool applied;

        /// <summary>
        /// 把 `CompProperties_Trion` 注入所有人形 Pawn ThingDef。
        /// 这里做的只是正式宿主接线，不承载战斗体、Trigger 或其它业务规则。
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
                if (!ShouldInject(thingDef))
                {
                    continue;
                }

                if (HasTrionComp(thingDef))
                {
                    continue;
                }

                if (thingDef.comps == null)
                {
                    thingDef.comps = new List<CompProperties>();
                }

                thingDef.comps.Add(BuildDefaultCompProperties());
                injectedCount++;
            }

            BdpDiagnostics.Once(
                "trion.injector.applied",
                "PawnTrionCompInjector 已为 " + injectedCount + " 个人形 Pawn ThingDef 接入 CompTrion 宿主。");
        }

        /// <summary>
        /// 当前只给人形 Pawn 接入 Trion 宿主。
        /// 这是资源宿主接线，不等于这些 Pawn 已拥有 BDP 完整玩法资格。
        /// </summary>
        private static bool ShouldInject(ThingDef thingDef)
        {
            return thingDef != null
                   && thingDef.category == ThingCategory.Pawn
                   && thingDef.race != null
                   && thingDef.race.Humanlike;
        }

        /// <summary>
        /// 检查当前 Pawn Def 是否已经具备 `CompTrion` 宿主。
        /// </summary>
        private static bool HasTrionComp(ThingDef thingDef)
        {
            if (thingDef.comps == null)
            {
                return false;
            }

            for (int i = 0; i < thingDef.comps.Count; i++)
            {
                if (thingDef.comps[i] is CompProperties_Trion)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 构建当前阶段统一使用的 Pawn Trion 宿主配置。
        /// 当前改成休眠底座，真正数值改由 Pawn stat 派生。
        /// </summary>
        private static CompProperties_Trion BuildDefaultCompProperties()
        {
            return new CompProperties_Trion
            {
                baseMax = 0f,
                startPercent = 1f,
                recoveryPerDay = 0f,
                drainSettleInterval = 60,
                recoveryInterval = 150
            };
        }
    }
}
