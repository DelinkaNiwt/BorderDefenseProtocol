using RimWorld;
using Verse;

namespace BDP.Content.Shield
{
    /// <summary>
    /// 正式能量护盾的 HediffComp 配置。
    /// 这里只承载旧版护盾实际使用的业务参数，不扩展主模组协议。
    /// </summary>
    public sealed class HediffCompProperties_EnergyShield : HediffCompProperties
    {
        /// <summary>
        /// 单枚护盾是否检查攻击来源角度。
        /// </summary>
        public bool enableAngleCheck = true;

        /// <summary>
        /// 单枚护盾的防护角度范围。
        /// </summary>
        public float blockAngleRange = 180f;

        /// <summary>
        /// 防护中心相对 Pawn 朝向的角度偏移。
        /// </summary>
        public float blockAngleOffset;

        /// <summary>
        /// 单枚护盾的抵挡成功率。
        /// </summary>
        public float blockChance = 0.7f;

        /// <summary>
        /// 当前护盾是否允许近战伤害进入抵挡流程。
        /// 默认关闭，保持既有正式护盾只挡远程和爆炸的行为。
        /// </summary>
        public bool allowMeleeDamage = false;

        /// <summary>
        /// 成功抵挡时按伤害值计算 Trion 成本的倍率。
        /// </summary>
        public float trionCostMultiplier = 0.7f;

        /// <summary>
        /// 双枚聚合护盾是否检查攻击来源角度。
        /// </summary>
        public bool stackedEnableAngleCheck;

        /// <summary>
        /// 双枚聚合护盾的定义角度范围。
        /// </summary>
        public float stackedBlockAngleRange = 360f;

        /// <summary>
        /// 双枚聚合护盾的抵挡成功率。
        /// </summary>
        public float stackedBlockChance = 0.95f;

        /// <summary>
        /// 抵挡成功时优先使用的原版 Effecter 定义。
        /// </summary>
        public EffecterDef blockEffectDef;

        /// <summary>
        /// 抵挡命中特效缩放。
        /// </summary>
        public float effectScale = 0.25f;

        /// <summary>
        /// 成功抵挡时是否生成六边形护盾瞬时贴图。
        /// 默认开启，保持既有能量护盾表现。
        /// </summary>
        public bool showBlockGraphic = true;

        /// <summary>
        /// 护盾球半径和命中特效离 Pawn 中心的距离。
        /// </summary>
        public float shieldRadius = 0.8f;

        /// <summary>
        /// 成功抵挡特效离 Pawn 中心的距离。
        /// 负值表示沿用 shieldRadius，保持旧定义兼容。
        /// </summary>
        public float impactEffectRadius = -1f;

        /// <summary>
        /// 是否绘制原版护盾球。
        /// </summary>
        public bool showShieldBubble = true;

        /// <summary>
        /// 抵挡成功后表达视觉受击位移持续的 tick 数。
        /// 小于等于零表示不启用该表现。
        /// </summary>
        public int blockVisualImpulseTicks;

        /// <summary>
        /// 抵挡成功后表达视觉沿攻击行进方向内缩的最大距离。
        /// 小于等于零表示不启用该表现。
        /// </summary>
        public float blockVisualImpulseDistance;

        /// <summary>
        /// 是否按攻击来源所在的地图南北半圆切换命中特效前后景。
        /// 默认关闭，保持既有护盾表现。
        /// </summary>
        public bool useDirectionalImpactDepth;

        /// <summary>
        /// 北半圆命中时替换默认白闪的后景 Fleck 定义。
        /// </summary>
        public FleckDef backgroundBlockFlashFleckDef;

        /// <summary>
        /// 南半圆及正东、正西命中时使用的前景偏转组合特效。
        /// </summary>
        public EffecterDef foregroundDeflectEffectDef;

        /// <summary>
        /// 北半圆命中时使用的后景偏转组合特效。
        /// </summary>
        public EffecterDef backgroundDeflectEffectDef;

        /// <summary>
        /// 初始化配置并绑定实际 HediffComp 类型。
        /// </summary>
        public HediffCompProperties_EnergyShield()
        {
            compClass = typeof(HediffComp_EnergyShield);
        }

        /// <summary>解析成功抵挡特效实际使用的贴身距离。</summary>
        public float ResolveImpactEffectRadius()
        {
            return impactEffectRadius >= 0f ? impactEffectRadius : shieldRadius;
        }

        /// <summary>
        /// 判断目标伤害是否属于当前护盾允许的远程、爆炸或可选近战伤害。
        /// </summary>
        public bool CanAbsorb(DamageInfo damageInfo)
        {
            DamageDef damageDef = damageInfo.Def;
            return damageDef != null
                && (damageDef.isRanged
                    || damageDef.isExplosive
                    || (allowMeleeDamage && EnergyShieldBlockPolicy.IsMeleeDamage(damageInfo)));
        }
    }
}
