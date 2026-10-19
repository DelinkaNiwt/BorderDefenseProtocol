using RimWorld;
using Verse;

namespace BDP.Content.Shield
{
    /// <summary>
    /// 正式能量护盾的 HediffComp 配置。
    /// 防护判定与共享耐久参数均由正式内容配置，不向核心层写入业务。
    /// </summary>
    public sealed class HediffCompProperties_EnergyShield : HediffCompProperties
    {
        /// <summary>同人物同组共享耐久和恢复计时；姿态变更必须保持组键一致。</summary>
        public string durabilityGroupKey;
        /// <summary>可选的盾组状态卡标题语言键；同组各姿态使用相同键，未配置时使用定义名称。</summary>
        public string statusCardLabelKey;
        /// <summary>每个有效来源贡献的最大耐久。</summary>
        public float maximumDurability = 120f;
        /// <summary>是否按 Severity（严重度）中的有效来源数量相加上限。</summary>
        public bool aggregateDurability;
        /// <summary>成功格挡的独立耐久损耗倍率，不与能量扣费倍率耦合。</summary>
        public float durabilityLossMultiplier = 1f;
        /// <summary>破盾恢复时长，单位游戏刻；默认 10 秒。</summary>
        public int recoveryTicks = 600;
        /// <summary>恢复量是否按上限比例解释；默认使用每秒固定点数。</summary>
        public bool naturalRecoveryUsePercentage;
        /// <summary>每游戏秒恢复量；固定模式为点数，百分比模式中 0.1 表示 10%，零关闭恢复。</summary>
        public float naturalRecoveryPerSecond = 24f;
        /// <summary>最后实际扣耐力到开始自然恢复的等待时长，单位游戏刻。</summary>
        public int naturalRecoveryDelayTicks;

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
                // 原版和其他模组声明的穿盾伤害先行退出，不扣能量或盾耐久。
                && !damageDef.ignoreShields
                && (damageDef.isRanged
                    || damageDef.isExplosive
                    || (allowMeleeDamage && EnergyShieldBlockPolicy.IsMeleeDamage(damageInfo)));
        }
    }
}
