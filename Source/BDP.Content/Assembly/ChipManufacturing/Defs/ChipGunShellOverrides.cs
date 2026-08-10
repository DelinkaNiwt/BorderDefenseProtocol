using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Defs
{
    /// <summary>
    /// 枪壳对远程表达字段的可选覆盖块。
    /// 所有可空字段只有在显式声明时才覆盖动作原值。
    /// </summary>
    public sealed class ChipGunShellOverrides
    {
        /// <summary>最大射程绝对值。</summary>
        public float? range;

        /// <summary>最小射程绝对值。</summary>
        public float? minRange;

        /// <summary>贴身精度绝对值。</summary>
        public float? accuracyTouch;

        /// <summary>近距精度绝对值。</summary>
        public float? accuracyShort;

        /// <summary>中距精度绝对值。</summary>
        public float? accuracyMedium;

        /// <summary>远距精度绝对值。</summary>
        public float? accuracyLong;

        /// <summary>预热时间绝对值。</summary>
        public float? warmupTime;

        /// <summary>冷却时间绝对值。</summary>
        public float? defaultCooldownTime;

        /// <summary>连发数量绝对值。</summary>
        public int? burstShotCount;

        /// <summary>连发数量倍率。</summary>
        public float? burstShotCountMultiplier;

        /// <summary>连发间隔绝对值。</summary>
        public int? ticksBetweenBurstShots;

        /// <summary>是否均匀分布脱靶。</summary>
        public bool? forcedMissEvenDispersal;

        /// <summary>是否允许低命中时飞偏。</summary>
        public bool? canGoWild;

        /// <summary>开火音效 DefName。</summary>
        public string soundCast;

        /// <summary>枪声尾音 DefName。</summary>
        public string soundCastTail;

        /// <summary>瞄准持续音效 DefName。</summary>
        public string soundAiming;

        /// <summary>枪口闪光大小。</summary>
        public float? muzzleFlashScale;

        /// <summary>枪声传播半径。</summary>
        public float? noiseRadius;

        /// <summary>射击动画规则包 DefName。</summary>
        public string rangedFireRulepack;

        /// <summary>投射物 DefName。</summary>
        public string defaultProjectile;

        /// <summary>射程倍率。</summary>
        public float? rangeMultiplier;

        /// <summary>四档精度统一倍率。</summary>
        public float? accuracyMultiplier;

        /// <summary>预热时间倍率。</summary>
        public float? warmupMultiplier;

        /// <summary>冷却时间倍率。</summary>
        public float? cooldownMultiplier;

        /// <summary>远程射击节奏覆盖。</summary>
        public ChipAttackExecutionRhythmConfig? rhythm;

        /// <summary>齐射发射点散布覆盖。</summary>
        public ChipAttackOriginSpreadConfig originSpread;

        /// <summary>单侧视觉预设 DefName。</summary>
        public string visualPresetDefName;

        /// <summary>复合视觉预设 DefName。</summary>
        public string compositeVisualPresetDefName;

        /// <summary>是否强制隐藏宿主装备。</summary>
        public bool? forceSuppressHostEquipment;

        /// <summary>视觉优先级。</summary>
        public int? visualPriority;

        /// <summary>远程模块追加或替换列表。</summary>
        public List<RangedModuleMountConfig> rangedModules;

        /// <summary>远程模块合并策略。</summary>
        public MergeStrategy rangedModulesMerge = MergeStrategy.Append;

        /// <summary>近战工具追加或替换列表。</summary>
        public List<Tool> tools;

        /// <summary>近战工具合并策略。</summary>
        public MergeStrategy toolsMerge = MergeStrategy.Append;
    }
}
