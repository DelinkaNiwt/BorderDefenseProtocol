using System;
using System.Collections.Generic;
using System.Reflection;
using BDP.Core.Expressions;
using RimWorld;
using Verse;

namespace BDP.Core.Information.Vanilla
{
    /// <summary>
    /// 集中调用 RimWorld 原版武器统计读取入口。
    /// 该适配器只转发原版结果，不复制原版伤害、DPS 或护甲穿透公式。
    /// </summary>
    public static class VanillaWeaponStatSurfaceReader
    {
        /// <summary>原版 Def 统计枚举入口；不同版本可能是公开或私有成员。</summary>
        private static readonly MethodInfo StatsToDrawMethod =
            typeof(StatsReportUtility).GetMethod(
                "StatsToDraw",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Def), typeof(ThingDef) },
                null);

        /// <summary>
        /// 读取一条原版兼容表面的统计条目。
        /// </summary>
        public static bool TryRead(
            VanillaWeaponStatSurface surface,
            out IReadOnlyList<StatDrawEntry> entries,
            out string failureReason)
        {
            List<StatDrawEntry> result = new List<StatDrawEntry>();
            entries = result.AsReadOnly();
            failureReason = null;
            if (surface?.Definition == null)
            {
                failureReason = "原版统计表面为空。";
                return false;
            }

            try
            {
                StatRequest request = StatRequest.For(surface.Definition, null);
                if (surface.Mode == VerbExpressionModeConfig.Ranged)
                {
                    foreach (StatDrawEntry entry in surface.Definition.SpecialDisplayStats(request))
                    {
                        if (entry != null
                            && entry.category == StatCategoryDefOf.Weapon_Ranged)
                        {
                            result.Add(entry);
                        }
                    }

                    AddRangedStatEntries(surface.Definition, result);
                }
                else if (surface.Mode == VerbExpressionModeConfig.Melee)
                {
                    AddMeleeStat(
                        surface.Definition,
                        request,
                        "MeleeWeapon_AverageDPS",
                        result);
                    AddMeleeStat(
                        surface.Definition,
                        request,
                        "MeleeWeapon_AverageArmorPenetration",
                        result);
                }

                entries = result.AsReadOnly();
                return true;
            }
            catch (Exception exception)
            {
                failureReason = "原版武器统计读取异常：" + exception.GetType().Name;
                entries = result.AsReadOnly();
                return false;
            }
        }

        /// <summary>
        /// 读取原版基于 ThingDef.statBases 的远程统计字段。
        /// SpecialDisplayStats 只覆盖伤害、射程、连射等手工条目；精度和远程冷却
        /// 属于 StatsReportUtility 的 StatDef 枚举链，必须从这里补入。
        /// </summary>
        private static void AddRangedStatEntries(
            ThingDef definition,
            ICollection<StatDrawEntry> target)
        {
            if (definition == null || target == null)
            {
                return;
            }

            if (StatsToDrawMethod == null)
            {
                return;
            }

            IEnumerable<StatDrawEntry> entries = StatsToDrawMethod.Invoke(
                null,
                new object[] { definition, null }) as IEnumerable<StatDrawEntry>;
            if (entries == null)
            {
                return;
            }

            foreach (StatDrawEntry entry in entries)
            {
                if (entry != null && entry.category == StatCategoryDefOf.Weapon_Ranged)
                {
                    target.Add(entry);
                }
            }
        }

        /// <summary>调用一个原版近战统计 Worker 并追加结果。</summary>
        private static void AddMeleeStat(
            ThingDef definition,
            StatRequest request,
            string statDefName,
            ICollection<StatDrawEntry> target)
        {
            StatDef statDef = DefDatabase<StatDef>.GetNamedSilentFail(statDefName);
            if (statDef == null || statDef.Worker == null || !statDef.Worker.ShouldShowFor(request))
            {
                return;
            }

            float value = definition.GetStatValueAbstract(statDef);
            target.Add(new StatDrawEntry(
                statDef.category,
                statDef,
                value,
                request));
        }
    }
}
