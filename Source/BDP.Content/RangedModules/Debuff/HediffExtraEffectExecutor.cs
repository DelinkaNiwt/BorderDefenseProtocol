using System;
using BDP.Core.Projectiles.RangedFlightProtocol.Effects;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using RimWorld;
using Verse;

namespace BDP.Content.RangedModules.Debuff
{
    /// <summary>
    /// Hediff 额外效果执行器。
    /// </summary>
    public sealed class HediffExtraEffectExecutor : IExtraEffectPlanExecutor
    {
        /// <summary>
        /// 当前执行器承接的效果键。
        /// </summary>
        public string EffectKind
        {
            get { return "hediff"; }
        }

        /// <summary>
        /// 按中性额外效果计划向目标 Pawn 添加或更新 Hediff。
        /// </summary>
        public bool TryExecute(ExtraEffectPlan effectPlan, ExtraEffectExecutionContext context)
        {
            Pawn targetPawn = context?.TargetThing as Pawn;
            if (targetPawn == null || targetPawn.health == null || effectPlan == null)
            {
                return false;
            }

            string targetFilter = ReadParameter(effectPlan, "targetFilter");
            if (!SupportsTargetFilter(targetFilter))
            {
                return false;
            }

            HediffDef hediffDef = ResolveHediffDef(effectPlan);
            if (hediffDef == null)
            {
                return false;
            }

            float severity = ReadFloat(effectPlan, "severity", 0.1f);
            int durationTicks = ReadInt(effectPlan, "durationTicks", 0);
            RangedDebuffStackMode stackMode = ReadEnum(
                effectPlan,
                "stackMode",
                RangedDebuffStackMode.Add);
            Hediff hediff = targetPawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(hediffDef, targetPawn);
                targetPawn.health.AddHediff(hediff);
            }

            ApplySeverity(hediff, severity, stackMode);
            HediffComp_StackingRecovery recovery = hediff.TryGetComp<HediffComp_StackingRecovery>();
            recovery?.NotifyEffectiveHit();

            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null && durationTicks > 0)
            {
                disappears.ticksToDisappear = Math.Max(disappears.ticksToDisappear, durationTicks);
            }

            return true;
        }

        /// <summary>
        /// 按模块叠加策略更新严重度。
        /// </summary>
        private static void ApplySeverity(
            Hediff hediff,
            float severity,
            RangedDebuffStackMode stackMode)
        {
            if (hediff == null)
            {
                return;
            }

            float safeSeverity = Math.Max(0f, severity);
            switch (stackMode)
            {
                case RangedDebuffStackMode.Replace:
                    hediff.Severity = safeSeverity;
                    break;
                case RangedDebuffStackMode.Max:
                case RangedDebuffStackMode.Refresh:
                    hediff.Severity = Math.Max(hediff.Severity, safeSeverity);
                    break;
                default:
                    hediff.Severity += safeSeverity;
                    break;
            }
        }

        /// <summary>
        /// 解析额外效果参数中的 Hediff Def。
        /// </summary>
        private static HediffDef ResolveHediffDef(ExtraEffectPlan effectPlan)
        {
            string defName = ReadParameter(effectPlan, "hediffDef");
            return string.IsNullOrWhiteSpace(defName)
                ? null
                : DefDatabase<HediffDef>.GetNamedSilentFail(defName);
        }

        /// <summary>
        /// Hediff（健康状态）执行器只承接自身支持的目标筛选语义。
        /// Pawn 类型判断已在入口完成，这里只拒绝未知筛选值，不重复伪造目标判定。
        /// </summary>
        private static bool SupportsTargetFilter(string targetFilter)
        {
            return string.IsNullOrWhiteSpace(targetFilter)
                || targetFilter == RangedDebuffTargetFilter.PawnsOnly.ToString()
                || targetFilter == RangedDebuffTargetFilter.AnyThing.ToString();
        }

        /// <summary>
        /// 读取字符串参数。
        /// </summary>
        private static string ReadParameter(ExtraEffectPlan effectPlan, string key)
        {
            string value;
            return effectPlan?.Parameters != null
                && effectPlan.Parameters.TryGetValue(key, out value)
                ? value
                : null;
        }

        /// <summary>
        /// 读取浮点参数。
        /// </summary>
        private static float ReadFloat(ExtraEffectPlan effectPlan, string key, float fallback)
        {
            float value;
            return float.TryParse(
                ReadParameter(effectPlan, key),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value)
                ? value
                : fallback;
        }

        /// <summary>
        /// 读取整数参数。
        /// </summary>
        private static int ReadInt(ExtraEffectPlan effectPlan, string key, int fallback)
        {
            int value;
            return int.TryParse(ReadParameter(effectPlan, key), out value) ? value : fallback;
        }

        /// <summary>
        /// 读取枚举参数。
        /// </summary>
        private static TEnum ReadEnum<TEnum>(
            ExtraEffectPlan effectPlan,
            string key,
            TEnum fallback)
            where TEnum : struct
        {
            TEnum value;
            return Enum.TryParse(ReadParameter(effectPlan, key), out value) ? value : fallback;
        }
    }
}
