using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Trigger.Shield
{
    /// <summary>
    /// 护盾Hediff组件配置类
    /// 定义护盾的所有可配置参数（方向判定、成功率、Trion消耗、特效等）
    /// </summary>
    public class HediffCompProperties_BDPShield : HediffCompProperties
    {
        // ==================== 方向/角度配置 ====================

        /// <summary>
        /// 是否启用角度检查（false=全方位防护，true=仅特定角度范围）
        /// </summary>
        public bool enableAngleCheck = false;

        /// <summary>
        /// 可抵挡的角度范围（度数，0-360）
        /// 例如：180表示前方180度，360表示全方位
        /// </summary>
        public float blockAngleRange = 360f;

        /// <summary>
        /// 角度偏移（度数，相对于pawn朝向）
        /// 0=正前方，90=右侧，-90=左侧，180=正后方
        /// </summary>
        public float blockAngleOffset = 0f;

        // ==================== 成功率配置 ====================

        /// <summary>
        /// 抵挡成功率（0.0-1.0）
        /// 1.0=100%抵挡，0.5=50%抵挡
        /// </summary>
        public float blockChance = 1f;

        // ==================== Trion消耗配置 ====================

        /// <summary>
        /// Trion消耗倍率
        /// 公式：Trion扣除 = 伤害值 × trionCostMultiplier
        /// 例如：0.5表示受到20点伤害时消耗10 Trion
        /// </summary>
        public float trionCostMultiplier = 0.5f;

        // ==================== Severity叠加配置 ====================

        /// <summary>
        /// Severity>=2时是否启用角度检查
        /// false=全方位防护，true=使用stackedBlockAngleRange
        /// </summary>
        public bool stackedEnableAngleCheck = false;

        /// <summary>
        /// Severity>=2时的防护角度范围（度数）
        /// 例如：360表示全方位防护
        /// </summary>
        public float stackedBlockAngleRange = 360f;

        /// <summary>
        /// Severity>=2时的抵挡成功率（0.0-1.0）
        /// 例如：0.95表示95%成功率
        /// </summary>
        public float stackedBlockChance = 0.95f;

        // ==================== 特效配置 ====================

        /// <summary>
        /// 抵挡成功时播放的特效
        /// 默认使用Interceptor_BlockedProjectilePsychic（灵能护盾拦截子弹的虫洞特效）
        /// </summary>
        public EffecterDef blockEffectDef;

        /// <summary>
        /// 特效缩放比例
        /// 默认0.5（缩小到原来的50%）
        /// </summary>
        public float effectScale = 0.5f;

        /// <summary>
        /// 护盾半径（格数）
        /// 特效会显示在这个半径的边缘
        /// 默认1.5格
        /// </summary>
        public float shieldRadius = 1.5f;

        /// <summary>
        /// 是否显示护盾范围球体
        /// </summary>
        public bool showShieldBubble = true;

        /// <summary>
        /// 护盾球体颜色（RGBA）
        /// 默认浅蓝色半透明
        /// </summary>
        public ColorInt shieldColor = new ColorInt(100, 150, 255, 50);

        // ==================== 近战防护配置 ====================

        /// <summary>
        /// 是否可以抵挡近战攻击
        /// 默认false（仅拦截远程和爆炸伤害）
        /// </summary>
        public bool canBlockMelee = false;

        /// <summary>
        /// 近战抵挡成功率倍率（相对于blockChance）
        /// 例如：blockChance=0.9，meleeBlockChanceMultiplier=0.7，则近战成功率=0.63
        /// 默认1.0（与远程相同）
        /// </summary>
        public float meleeBlockChanceMultiplier = 1f;

        /// <summary>
        /// 近战Trion消耗倍率（相对于trionCostMultiplier）
        /// 例如：trionCostMultiplier=0.5，meleeTrionCostMultiplier=1.5，则近战消耗=0.75
        /// 默认1.0（与远程相同）
        /// </summary>
        public float meleeTrionCostMultiplier = 1f;

        // ==================== 伤害类型过滤 ====================

        /// <summary>
        /// 可吸收的伤害类型列表（白名单）
        /// 如果指定，则只拦截列表中的伤害类型
        /// 如果为null或空，则拦截所有远程和爆炸伤害（以及近战，如果canBlockMelee=true）
        /// </summary>
        public List<DamageDef> absorbDamageTypes;

        /// <summary>
        /// 忽略的伤害类型列表（黑名单）
        /// 列表中的伤害类型不会被拦截
        /// </summary>
        public List<DamageDef> ignoreDamageTypes;

        // ==================== 构造函数 ====================

        /// <summary>
        /// 构造函数，设置组件类型
        /// </summary>
        public HediffCompProperties_BDPShield()
        {
            compClass = typeof(HediffComp_BDPShield);
        }

        // ==================== 辅助方法 ====================

        /// <summary>
        /// 检查护盾是否可以吸收指定类型的伤害
        /// </summary>
        /// <param name="damageDef">伤害类型</param>
        /// <returns>true=可以吸收，false=不能吸收</returns>
        public bool CanAbsorb(DamageDef damageDef)
        {
            // 1. 如果在忽略列表中，不拦截
            if (ignoreDamageTypes != null && ignoreDamageTypes.Contains(damageDef))
                return false;

            // 2. 如果指定了可拦截列表，只拦截列表中的类型
            if (absorbDamageTypes != null && absorbDamageTypes.Count > 0)
                return absorbDamageTypes.Contains(damageDef);

            // 3. 默认拦截远程和爆炸伤害
            bool canAbsorb = damageDef.isRanged || damageDef.isExplosive;

            // 4. 如果启用近战拦截，也拦截近战伤害
            // 近战伤害的特征：hasForcefulImpact=true（有物理冲击力）
            if (canBlockMelee && damageDef.hasForcefulImpact)
            {
                canAbsorb = true;
            }

            return canAbsorb;
        }
    }
}
