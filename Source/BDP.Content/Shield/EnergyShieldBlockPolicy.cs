using UnityEngine;

namespace BDP.Content.Shield
{
    /// <summary>
    /// 正式能量护盾的确定性判定策略。
    /// 它只计算角度、聚合概率和扣费数值，不持有游戏真值。
    /// </summary>
    internal static class EnergyShieldBlockPolicy
    {
        /// <summary>
        /// 判断攻击来源是否落在当前护盾防护弧内。
        /// </summary>
        internal static bool IsWithinArc(
            float pawnAngle,
            float damageAngle,
            bool enableAngleCheck,
            float angleRange,
            float angleOffset)
        {
            if (!enableAngleCheck)
            {
                return true;
            }

            float sourceAngle = (damageAngle + 180f) % 360f;
            float relativeAngle = Mathf.DeltaAngle(pawnAngle, sourceAngle);
            float minimum = angleOffset - (angleRange / 2f);
            float maximum = angleOffset + (angleRange / 2f);
            return relativeAngle >= minimum && relativeAngle <= maximum;
        }

        /// <summary>
        /// 根据 Hediff Severity 选择单枚或双枚护盾抵挡率。
        /// </summary>
        internal static float ResolveBlockChance(
            float severity,
            float singleChance,
            float stackedChance)
        {
            return severity >= 2f ? stackedChance : singleChance;
        }

        /// <summary>
        /// 按本次原始伤害和配置倍率计算抵挡 Trion 成本。
        /// </summary>
        internal static float CalculateTrionCost(float damageAmount, float multiplier)
        {
            return damageAmount * multiplier;
        }
    }
}
