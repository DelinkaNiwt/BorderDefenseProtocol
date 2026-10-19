using UnityEngine;

namespace BDP.Content.Trigger.Black
{
    /// <summary>
    /// 黑触发器从 Pawn 合并池拆回独立容器时的纯计算结果。
    /// </summary>
    public sealed class BlackTriggerTrionCapacitySplitResult
    {
        /// <summary>
        /// 拆分后保留在 Pawn 合并池中的当前量。
        /// </summary>
        public float PawnCurrent { get; set; }

        /// <summary>
        /// 拆分后回到黑触发器容器的当前量。
        /// </summary>
        public float TriggerCurrent { get; set; }

        /// <summary>
        /// 超出 Pawn 与黑触发器总容量、按规则丢失的当前量。
        /// </summary>
        public float LostCurrent { get; set; }
    }

    /// <summary>
    /// 黑触发器 Trion 合并池拆分计算器。
    /// </summary>
    public static class BlackTriggerTrionCapacityCalculator
    {
        /// <summary>
        /// 按“先填 Pawn，再填触发器，最后丢失溢出”的规则拆分当前量。
        /// </summary>
        public static BlackTriggerTrionCapacitySplitResult Calculate(
            float combinedCurrent,
            float pawnMax,
            float triggerMax)
        {
            combinedCurrent = SanitizeNonNegative(combinedCurrent);
            pawnMax = SanitizeNonNegative(pawnMax);
            triggerMax = SanitizeNonNegative(triggerMax);

            float pawnCurrent = Mathf.Min(combinedCurrent, pawnMax);
            float triggerCurrent = Mathf.Min(
                Mathf.Max(combinedCurrent - pawnMax, 0f),
                triggerMax);
            float lostCurrent = Mathf.Max(
                combinedCurrent - pawnMax - triggerMax,
                0f);

            return new BlackTriggerTrionCapacitySplitResult
            {
                PawnCurrent = pawnCurrent,
                TriggerCurrent = triggerCurrent,
                LostCurrent = lostCurrent
            };
        }

        /// <summary>
        /// 把异常浮点输入安全压回非负有限值。
        /// </summary>
        private static float SanitizeNonNegative(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? 0f
                : Mathf.Max(0f, value);
        }
    }
}
