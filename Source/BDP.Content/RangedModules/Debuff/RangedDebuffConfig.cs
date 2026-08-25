using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.AttackExecution;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.Debuff
{
    /// <summary>
    /// 远程命中减益模块的 Content 配置快照。
    /// </summary>
    public sealed class RangedDebuffConfig : RangedModuleConfigNode, IRangedModuleWeaponVisualOverride
    {
        /// <summary>
        /// 额外效果执行器键。
        /// </summary>
        public string EffectKind = "hediff";

        /// <summary>
        /// 命中后要施加的 Hediff Def。
        /// </summary>
        public HediffDef HediffDef;

        /// <summary>
        /// 额外效果使用的中性目标事件来源。
        /// </summary>
        public ExtraEffectTargetScope TargetScope = ExtraEffectTargetScope.DirectHitThing;

        /// <summary>
        /// 当前模块的目标筛选方式。
        /// </summary>
        public RangedDebuffTargetFilter TargetFilter = RangedDebuffTargetFilter.PawnsOnly;

        /// <summary>
        /// 当前模块独立提交的伤害处置。
        /// </summary>
        public DamageDisposition DamageSuppression = DamageDisposition.SuppressAllProjectileImpact;

        /// <summary>
        /// 伤害被取消时是否仍保留攻击生产者的目标解析。
        /// </summary>
        public bool PreserveTargetResolutionWhenDamageSuppressed;

        /// <summary>
        /// 伤害被本模块拦截后是否补回原版 Pawn 受击反馈。
        /// </summary>
        public ImpactHitFeedbackMode InterceptedHitFeedback = ImpactHitFeedbackMode.None;

        /// <summary>
        /// 是否覆盖本模块目标的原版受击闪烁颜色。
        /// 未启用时不改变原版红色反馈。
        /// </summary>
        public bool HasHitFeedbackColor;

        /// <summary>
        /// 本模块目标使用的受击闪烁颜色。
        /// </summary>
        public Color HitFeedbackColor = Color.white;

        /// <summary>
        /// 是否覆盖本发投射物拖尾颜色。
        /// 未启用时保持投射物原有拖尾颜色或视觉提供器默认行为。
        /// </summary>
        public bool HasProjectileTrailColor;

        /// <summary>
        /// 本模块声明的投射物拖尾颜色。
        /// </summary>
        public Color ProjectileTrailColor = Color.white;

        /// <summary>
        /// 是否在原有发光拖尾内部追加非发光内芯。
        /// </summary>
        public bool HasProjectileTrailCore;

        /// <summary>
        /// 本模块声明的投射物拖尾内芯颜色。
        /// </summary>
        public Color ProjectileTrailCoreColor = Color.black;

        /// <summary>
        /// 投射物拖尾内芯相对原有外层的宽度比例。
        /// </summary>
        public float ProjectileTrailCoreWidthRatio = 0.45f;

        /// <summary>
        /// 投射物拖尾内芯透明度倍率。
        /// </summary>
        public float ProjectileTrailCoreOpacity = 1f;

        /// <summary>
        /// 是否把当前激活武器的手持贴图替换为指定视觉预设。
        /// 留空时完全沿用当前武器原有视觉。
        /// </summary>
        public string WeaponVisualPresetDefName;

        /// <summary>
        /// 本模块对投射物初始飞行速度的倍率。
        /// 1 表示不改变原版速度。
        /// </summary>
        public float ProjectileSpeedMultiplier = 1f;

        /// <summary>
        /// 每次有效命中的初始严重度或叠加量。
        /// </summary>
        public float Severity = 0.1f;

        /// <summary>
        /// 模块声明的 Hediff 持续时间，交给具体 Hediff 方案消费。
        /// </summary>
        public int DurationTicks = 300;

        /// <summary>
        /// 叠加策略键。
        /// </summary>
        public RangedDebuffStackMode StackMode = RangedDebuffStackMode.Add;

        /// <summary>
        /// 是否在投射物阶段绕过原版投射物拦截器。
        /// </summary>
        public bool BypassProjectileInterceptors = true;

        /// <summary>
        /// 是否在伤害前阶段绕过已注册伤害护盾。
        /// </summary>
        public bool BypassRegisteredDamageShields = true;

        /// <summary>
        /// 是否关闭原版范围爆炸视觉。
        /// </summary>
        public bool SuppressVanillaExplosionVisualEffects = true;

        /// <summary>
        /// 是否关闭原版范围爆炸音效。
        /// </summary>
        public bool SuppressVanillaExplosionSoundEffects = true;

        /// <summary>
        /// 是否把范围爆炸屏幕震动覆盖为零。
        /// </summary>
        public bool SuppressVanillaExplosionScreenShake = true;

        /// <summary>
        /// 生成一份强类型配置快照。
        /// </summary>
        public RangedDebuffConfig CloneTyped()
        {
            return new RangedDebuffConfig
            {
                EffectKind = EffectKind,
                HediffDef = HediffDef,
                TargetScope = TargetScope,
                TargetFilter = TargetFilter,
                DamageSuppression = DamageSuppression,
                PreserveTargetResolutionWhenDamageSuppressed = PreserveTargetResolutionWhenDamageSuppressed,
                InterceptedHitFeedback = InterceptedHitFeedback,
                HasHitFeedbackColor = HasHitFeedbackColor,
                HitFeedbackColor = HitFeedbackColor,
                HasProjectileTrailColor = HasProjectileTrailColor,
                ProjectileTrailColor = ProjectileTrailColor,
                HasProjectileTrailCore = HasProjectileTrailCore,
                ProjectileTrailCoreColor = ProjectileTrailCoreColor,
                ProjectileTrailCoreWidthRatio = ProjectileTrailCoreWidthRatio,
                ProjectileTrailCoreOpacity = ProjectileTrailCoreOpacity,
                WeaponVisualPresetDefName = WeaponVisualPresetDefName,
                ProjectileSpeedMultiplier = ProjectileSpeedMultiplier,
                Severity = Severity,
                DurationTicks = DurationTicks,
                StackMode = StackMode,
                BypassProjectileInterceptors = BypassProjectileInterceptors,
                BypassRegisteredDamageShields = BypassRegisteredDamageShields,
                SuppressVanillaExplosionVisualEffects = SuppressVanillaExplosionVisualEffects,
                SuppressVanillaExplosionSoundEffects = SuppressVanillaExplosionSoundEffects,
                SuppressVanillaExplosionScreenShake = SuppressVanillaExplosionScreenShake
            };
        }

        /// <summary>
        /// 生成配置副本。
        /// </summary>
        public override RangedModuleConfigNode Clone()
        {
            return CloneTyped();
        }

        /// <summary>
        /// 向 Core 视觉投影层提供当前模块声明的武器贴图覆盖。
        /// </summary>
        string IRangedModuleWeaponVisualOverride.WeaponVisualPresetDefName
        {
            get { return WeaponVisualPresetDefName; }
        }
    }

    /// <summary>
    /// 远程减益目标筛选策略。
    /// </summary>
    public enum RangedDebuffTargetFilter
    {
        /// <summary>
        /// 只允许 Pawn。
        /// </summary>
        PawnsOnly,

        /// <summary>
        /// 允许所有原版爆炸目标 Thing。
        /// </summary>
        AnyThing
    }

    /// <summary>
    /// 远程减益叠加策略。
    /// </summary>
    public enum RangedDebuffStackMode
    {
        /// <summary>
        /// 每次有效命中增加严重度。
        /// </summary>
        Add,

        /// <summary>
        /// 每次有效命中替换严重度。
        /// </summary>
        Replace,

        /// <summary>
        /// 保留较高严重度。
        /// </summary>
        Max,

        /// <summary>
        /// 刷新为至少当前严重度。
        /// </summary>
        Refresh
    }
}
