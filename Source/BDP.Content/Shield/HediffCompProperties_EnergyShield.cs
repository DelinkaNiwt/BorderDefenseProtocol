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
        /// 护盾球半径和命中特效离 Pawn 中心的距离。
        /// </summary>
        public float shieldRadius = 0.8f;

        /// <summary>
        /// 是否绘制原版护盾球。
        /// </summary>
        public bool showShieldBubble = true;

        /// <summary>
        /// 初始化配置并绑定实际 HediffComp 类型。
        /// </summary>
        public HediffCompProperties_EnergyShield()
        {
            compClass = typeof(HediffComp_EnergyShield);
        }

        /// <summary>
        /// 判断目标伤害类型是否属于旧版护盾允许的远程或爆炸伤害。
        /// </summary>
        public bool CanAbsorb(DamageDef damageDef)
        {
            return damageDef != null && (damageDef.isRanged || damageDef.isExplosive);
        }
    }
}
