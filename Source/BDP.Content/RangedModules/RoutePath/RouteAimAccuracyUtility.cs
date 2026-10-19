using System;

namespace BDP.Content.RangedModules.RoutePath
{
    /// <summary>路线引导的精度曲线；只换算一次瞄准概率，不决定命中分支。</summary>
    public static class RouteAimAccuracyUtility
    {
        /// <summary>
        /// 根据原版实际每格精度压低剩余瞄准失误。
        /// 配置的参照强度为1时保持中性；体型和掩体不在此重复计算。
        /// </summary>
        public static float AdjustChance(
            float standardAimChance, float shooterAccuracy, float referenceAccuracy,
            float referenceErrorPower, float growthExponent)
        {
            if (!IsFinite(standardAimChance)) return 0f;
            double chance = Math.Max(0d, Math.Min(1d, standardAimChance));
            if (chance <= 0d || chance >= 1d || referenceErrorPower <= 1f
                || !IsFinite(shooterAccuracy) || !IsFinite(referenceAccuracy)
                || !IsFinite(referenceErrorPower) || !IsFinite(growthExponent)
                || referenceAccuracy <= 0f || referenceAccuracy >= 1f || growthExponent < 0f)
                return (float)chance;

            // 每格精度可能由其它模组抬到1以上，误差下限只用于避免除零。
            // 使用双精度计算，避免高端小概率在中间步骤过早丢失。
            double error = Math.Max(0.000001d, 1d - Math.Max(0d, shooterAccuracy));
            double power = 1d + (referenceErrorPower - 1d)
                * Math.Pow((1d - referenceAccuracy) / error, growthExponent);
            double adjusted = 1d - Math.Exp(power * Math.Log(1d - chance));
            return (float)Math.Min(1d, Math.Max(chance, adjusted));
        }

        /// <summary>兼容两个目标运行时的有限数值检查。</summary>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
