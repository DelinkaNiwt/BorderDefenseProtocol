using System.Collections.Generic;
namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技表达配置块。
    /// 当前尽量复用芯片表达条目结构，只在需要自动求值的位置补最小声明。
    /// </summary>
    public sealed class ComboExpressionConfig
    {
        /// <summary>
        /// 组合技声明的表达条目集合。
        /// 每条条目自己声明显式值与自动求值规则，避免把规则错误挂到整个 Expression 上。
        /// </summary>
        public List<ComboExpressionEntryConfig> Entries;
    }

    /// <summary>
    /// VerbProps 的字段级自动求值声明。
    /// 它不替代显式 VerbProps，只负责给缺失字段提供中性求值协议。
    /// </summary>
    public sealed class ComboVerbPropsResolutionConfig
    {
        /// <summary>
        /// `range` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? RangeResolve;

        /// <summary>
        /// `warmupTime` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? WarmupTimeResolve;

        /// <summary>
        /// `burstShotCount` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? BurstShotCountResolve;

        /// <summary>
        /// `ticksBetweenBurstShots` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? TicksBetweenBurstShotsResolve;

        /// <summary>
        /// `minRange` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? MinRangeResolve;

        /// <summary>
        /// `forcedMissRadius` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? ForcedMissRadiusResolve;

        /// <summary>
        /// `accuracyTouch` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? AccuracyTouchResolve;

        /// <summary>
        /// `accuracyShort` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? AccuracyShortResolve;

        /// <summary>
        /// `accuracyMedium` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? AccuracyMediumResolve;

        /// <summary>
        /// `accuracyLong` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? AccuracyLongResolve;

        /// <summary>
        /// `defaultCooldownTime` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? DefaultCooldownTimeResolve;
    }

    /// <summary>
    /// 执行节奏字段的自动求值声明。
    /// 当前只先覆盖最小近战节奏字段。
    /// </summary>
    public sealed class ComboExecutionResolutionConfig
    {
        /// <summary>
        /// `HitCount` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? HitCountResolve;

        /// <summary>
        /// `HitIntervalTicks` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? HitIntervalTicksResolve;

        /// <summary>
        /// 远程射击节奏的自动求值方式。
        /// 仅对 PrimaryVerb + Ranged 条目生效。
        /// </summary>
        public ComboValueResolveMode? RhythmResolve;
    }

    /// <summary>
    /// 表达级 Trion 字段的自动求值声明。
    /// 它只负责说明 Combo 条目缺少显式 Trion 时应从哪侧来源结果取值或怎样合成。
    /// </summary>
    public sealed class ComboExpressionTrionResolutionConfig
    {
        /// <summary>
        /// `UseCost` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? UseCostResolve;

        /// <summary>
        /// `MinimumRequired` 的自动求值方式。
        /// </summary>
        public ComboValueResolveMode? MinimumRequiredResolve;
    }
}
