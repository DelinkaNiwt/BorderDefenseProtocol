using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技 VerbProps 的字段级求值结果。
    /// 这一层只保留最小正式字段，避免直接在攻击链里临时猜值。
    /// </summary>
    internal sealed class ComboResolvedVerbProps
    {
        /// <summary>
        /// `range` 的求值结果。
        /// </summary>
        public ComboResolvedFieldValue<float> Range;

        /// <summary>
        /// `warmupTime` 的求值结果。
        /// </summary>
        public ComboResolvedFieldValue<float> WarmupTime;

        /// <summary>
        /// `burstShotCount` 的求值结果。
        /// </summary>
        public ComboResolvedFieldValue<int> BurstShotCount;

        /// <summary>
        /// `ticksBetweenBurstShots` 的求值结果。
        /// </summary>
        public ComboResolvedFieldValue<int> TicksBetweenBurstShots;

        /// <summary>
        /// `minRange` 的求值结果。
        /// </summary>
        public ComboResolvedFieldValue<float> MinRange;

        /// <summary>
        /// `forcedMissRadius` 的求值结果。
        /// </summary>
        public ComboResolvedFieldValue<float> ForcedMissRadius;

        /// <summary>
        /// `accuracyTouch` 的求值结果。
        /// </summary>
        public ComboResolvedFieldValue<float> AccuracyTouch;

        /// <summary>
        /// `accuracyShort` 的求值结果。
        /// </summary>
        public ComboResolvedFieldValue<float> AccuracyShort;

        /// <summary>
        /// `accuracyMedium` 的求值结果。
        /// </summary>
        public ComboResolvedFieldValue<float> AccuracyMedium;

        /// <summary>
        /// `accuracyLong` 的求值结果。
        /// </summary>
        public ComboResolvedFieldValue<float> AccuracyLong;

        /// <summary>
        /// `defaultCooldownTime` 的求值结果。
        /// </summary>
        public ComboResolvedFieldValue<float> DefaultCooldownTime;

        /// <summary>
        /// 当前最终选择的默认投射物。
        /// 当前阶段只接受显式值，不做对象引用型平均计算。
        /// </summary>
        public ThingDef DefaultProjectile;
    }
}
