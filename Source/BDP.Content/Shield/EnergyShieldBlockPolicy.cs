using RimWorld;
using Verse;
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
        /// 正东、正西归入前景时使用的浮点误差容限。
        /// </summary>
        private const float CardinalDepthEpsilon = 0.0001f;

        /// <summary>
        /// 判断一次原版伤害是否带有近战来源语义。
        /// DamageDef（伤害定义）的远程语义优先于 Weapon（武器）承载物。
        /// 这样不会把“带有近战敲击工具的触发体”承载的远程投射物误判为近战。
        /// Tool（工具）覆盖徒手与武器近战；Weapon（武器）只用于补足非远程的近战装备来源。
        /// </summary>
        internal static bool IsMeleeDamage(DamageInfo damageInfo)
        {
            if (damageInfo.Def != null && damageInfo.Def.isRanged)
            {
                return false;
            }

            return damageInfo.Tool != null
                || (damageInfo.Weapon != null && damageInfo.Weapon.IsMeleeWeapon);
        }

        /// <summary>
        /// 解析伤害实际行进方向的世界角度。
        /// 原版近战会把缺省角度随机化，因此近战优先从攻击者到受击者的实际位置恢复方向。
        /// </summary>
        internal static float ResolveAttackTravelAngle(Pawn target, DamageInfo damageInfo)
        {
            Thing instigator = damageInfo.Instigator;
            if (target != null
                && IsMeleeDamage(damageInfo)
                && instigator != null
                && instigator.Spawned
                && target.Spawned
                && instigator.Map == target.Map)
            {
                Vector3 travel = target.TrueCenter() - instigator.TrueCenter();
                if (travel.x != 0f || travel.z != 0f)
                {
                    return travel.AngleFlat();
                }
            }

            return damageInfo.Angle;
        }

        /// <summary>
        /// 判断攻击来源是否位于 Pawn 的地图北半圆，从而需要把命中特效放到后景。
        /// 只读取世界方向，不受 Pawn 面朝方向影响；正东、正西稳定归入前景。
        /// </summary>
        internal static bool ShouldRenderImpactBehindPawn(Vector3 sourceDirection)
        {
            return sourceDirection.z > CardinalDepthEpsilon;
        }

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
