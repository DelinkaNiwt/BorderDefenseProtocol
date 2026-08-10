using BDP.Core.CombatModel;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技执行节奏字段的求值结果。
    /// </summary>
    internal sealed class ComboResolvedExecution
    {
        /// <summary>
        /// `HitCount` 的求值结果。
        /// </summary>
        public ComboResolvedFieldValue<int> HitCount;

        /// <summary>
        /// `HitIntervalTicks` 的求值结果。
        /// </summary>
        public ComboResolvedFieldValue<int> HitIntervalTicks;

        /// <summary>
        /// 远程射击节奏的求值结果。
        /// 仅对 PrimaryVerb + Ranged 条目生效。
        /// </summary>
        public ComboResolvedFieldValue<RangedExecutionRhythm> Rhythm;
    }
}
