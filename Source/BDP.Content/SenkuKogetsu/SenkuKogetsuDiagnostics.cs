using System.Globalization;
using BDP.Support.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.SenkuKogetsu
{
    /// <summary>
    /// 旋空弧月专用诊断入口。
    /// 它只服务旋空弧月运行时的可插拔诊断。
    /// </summary>
    internal static class SenkuKogetsuDiagnostics
    {
        /// <summary>
        /// 当前日志统一前缀。
        /// </summary>
        private const string LogPrefix = "[BDP.Content][SenkuKogetsu]";

        /// <summary>
        /// 记录一次 Ability 施放时的目标与射程事实。
        /// </summary>
        /// <param name="pawn">当前施法者。</param>
        /// <param name="target">当前技能目标。</param>
        /// <param name="rawRange">未钳制前的原始射程。</param>
        /// <param name="setRange">钳制后的实际射程。</param>
        /// <param name="waveDef">即将生成的波体 Def。</param>
        internal static void LogCast(
            Pawn pawn,
            LocalTargetInfo target,
            float rawRange,
            float setRange,
            ThingDef waveDef)
        {
            string key = "devharness.senkukogetsu.cast."
                + SafeText(pawn != null ? pawn.ThingID : null)
                + "."
                + SafeText(waveDef != null ? waveDef.defName : null)
                + "."
                + GetSafeCurrentTick();
            BdpDiagnostics.Throttled(
                key,
                LogPrefix
                + " event=cast"
                + ", pawn=" + DescribePawn(pawn)
                + ", target=" + DescribeTarget(target)
                + ", rawRange=" + DescribeFloat(rawRange)
                + ", setRange=" + DescribeFloat(setRange)
                + ", waveDef=" + SafeText(waveDef != null ? waveDef.defName : null),
                1);
        }

        /// <summary>
        /// 记录一次月牙参数插值结果。
        /// </summary>
        /// <param name="setRange">当前设定射程。</param>
        /// <param name="halfWidth">当前半宽。</param>
        /// <param name="bulge">当前凸度。</param>
        /// <param name="thickness">当前厚度。</param>
        internal static void LogCrescentParams(float setRange, float halfWidth, float bulge, float thickness)
        {
            string key = "devharness.senkukogetsu.crescent."
                + DescribeFloat(setRange)
                + "."
                + GetSafeCurrentTick();
            BdpDiagnostics.Throttled(
                key,
                LogPrefix
                + " event=crescent_params"
                + ", setRange=" + DescribeFloat(setRange)
                + ", halfWidth=" + DescribeFloat(halfWidth)
                + ", bulge=" + DescribeFloat(bulge)
                + ", thickness=" + DescribeFloat(thickness),
                1);
        }

        /// <summary>
        /// 记录一次天然山体阻挡判定命中。
        /// </summary>
        /// <param name="origin">施法原点格。</param>
        /// <param name="target">被阻挡的目标格。</param>
        internal static void LogMountainBlock(IntVec3 origin, IntVec3 target)
        {
            string key = "devharness.senkukogetsu.mountain_block."
                + origin + "."
                + target + "."
                + GetSafeCurrentTick();
            BdpDiagnostics.Throttled(
                key,
                LogPrefix
                + " event=mountain_block"
                + ", origin=" + origin
                + ", target=" + target,
                1);
        }

        /// <summary>
        /// 记录一次建筑倍率伤害实际触发。
        /// </summary>
        /// <param name="building">当前受击建筑。</param>
        /// <param name="finalDamage">当前最终建筑伤害。</param>
        /// <param name="factor">当前建筑倍率。</param>
        internal static void LogBuildingDamage(Building building, int finalDamage, float factor)
        {
            string key = "devharness.senkukogetsu.building_hit."
                + SafeText(building != null ? building.ThingID : null)
                + "."
                + GetSafeCurrentTick();
            BdpDiagnostics.Throttled(
                key,
                LogPrefix
                + " event=building_hit"
                + ", building=" + DescribeThing(building)
                + ", finalDamage=" + finalDamage
                + ", factor=" + DescribeFloat(factor),
                1);
        }

        /// <summary>
        /// 记录当前目标在去重/过滤后的最终结果。
        /// </summary>
        /// <param name="thing">当前目标。</param>
        /// <param name="outcome">当前结果类型。</param>
        /// <param name="damageAmount">当前实际伤害值；未命中时可为空。</param>
        internal static void LogTargetResolution(Thing thing, string outcome, int? damageAmount = null)
        {
            string key = "devharness.senkukogetsu.target."
                + SafeText(thing != null ? thing.ThingID : null)
                + "."
                + SafeText(outcome)
                + "."
                + GetSafeCurrentTick();
            BdpDiagnostics.Throttled(
                key,
                LogPrefix
                + " event=target_resolution"
                + ", target=" + DescribeThing(thing)
                + ", outcome=" + SafeText(outcome)
                + ", damage=" + (damageAmount.HasValue ? damageAmount.Value.ToString() : "<none>"),
                1);
        }

        /// <summary>
        /// 安全读取当前游戏 tick。
        /// </summary>
        /// <returns>当前 tick；未就绪时返回 -1。</returns>
        private static int GetSafeCurrentTick()
        {
            try
            {
                return Find.TickManager != null ? Find.TickManager.TicksGame : -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 输出 Pawn 摘要。
        /// </summary>
        /// <param name="pawn">当前 Pawn。</param>
        /// <returns>便于检索的简短文本。</returns>
        private static string DescribePawn(Pawn pawn)
        {
            return pawn != null
                ? SafeText(pawn.LabelShortCap) + "(" + pawn.ThingID + ")"
                : "<none>";
        }

        /// <summary>
        /// 输出 Thing 摘要。
        /// </summary>
        /// <param name="thing">当前 Thing。</param>
        /// <returns>便于检索的简短文本。</returns>
        private static string DescribeThing(Thing thing)
        {
            if (thing == null)
            {
                return "<none>";
            }

            return SafeText(thing.LabelShortCap) + "(" + thing.ThingID + ")@" + thing.Position;
        }

        /// <summary>
        /// 输出目标摘要。
        /// </summary>
        /// <param name="target">当前目标。</param>
        /// <returns>便于检索的简短文本。</returns>
        private static string DescribeTarget(LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return "invalid";
            }

            if (target.HasThing)
            {
                return DescribeThing(target.Thing);
            }

            return target.Cell.ToString();
        }

        /// <summary>
        /// 输出浮点摘要。
        /// </summary>
        /// <param name="value">当前浮点值。</param>
        /// <returns>固定格式的文本。</returns>
        private static string DescribeFloat(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 统一清理空字符串。
        /// </summary>
        /// <param name="text">当前文本。</param>
        /// <returns>非空文本；为空时返回占位文本。</returns>
        private static string SafeText(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "<none>" : text;
        }
    }
}
