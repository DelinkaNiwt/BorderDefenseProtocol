using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Core.Information
{
    /// <summary>当前自动攻击显示模式。</summary>
    public enum AutomaticAttackDisplayMode
    {
        /// <summary>近战攻击。</summary>
        Melee,

        /// <summary>远程攻击。</summary>
        Ranged
    }

    /// <summary>
    /// 当前触发体自动攻击的只读显示快照。
    /// 该类型只承载最终显示值，不承载攻击执行状态，也不进入存档。
    /// </summary>
    public sealed class TriggerBodyAutomaticAttackDisplaySnapshot
    {
        /// <summary>当前表达投影版本。</summary>
        public int ProjectionVersion { get; internal set; }

        /// <summary>当前攻击结果稳定标识。</summary>
        public string ResultId { get; internal set; }

        /// <summary>当前攻击显示名称。</summary>
        public string DisplayLabel { get; internal set; }

        /// <summary>当前结果引用的来源结果只读列表。</summary>
        public IReadOnlyList<string> SourceResultIds { get; internal set; }

        /// <summary>当前攻击模式。</summary>
        public AutomaticAttackDisplayMode WeaponMode { get; internal set; }

        /// <summary>最终伤害。</summary>
        public float Damage { get; internal set; }

        /// <summary>最终护甲穿透。</summary>
        public float ArmorPenetration { get; internal set; }

        /// <summary>远程射程。</summary>
        public float RangedRange { get; internal set; }

        /// <summary>远程瞄准时间。</summary>
        public float RangedWarmupSeconds { get; internal set; }

        /// <summary>远程冷却时间。</summary>
        public float RangedCooldownSeconds { get; internal set; }

        /// <summary>远程齐射发射次数。</summary>
        public int RangedBurstCount { get; internal set; }

        /// <summary>远程齐射间隔游戏刻。</summary>
        public int RangedBurstIntervalTicks { get; internal set; }

        /// <summary>远程射速；单发武器没有该值。</summary>
        public float? RangedFireRate { get; internal set; }

        /// <summary>远程停止能力。</summary>
        public float? RangedStoppingPower { get; internal set; }

        /// <summary>远程伤害类型。</summary>
        public DamageDef RangedDamageType { get; internal set; }

        /// <summary>按完整远程攻击周期计算的每秒伤害。</summary>
        public float RangedAverageDps { get; internal set; }

        /// <summary>近战一轮攻击的命中次数。</summary>
        public int MeleeAttackCount { get; internal set; }

        /// <summary>近战连续命中间隔秒数。</summary>
        public float MeleeAttackIntervalSeconds { get; internal set; }

        /// <summary>近战完整攻击周期秒数。</summary>
        public float MeleeCycleSeconds { get; internal set; }

        /// <summary>考虑多击后的近战每秒伤害。</summary>
        public float MeleeAverageDps { get; internal set; }

        /// <summary>近战显示用命中概率。</summary>
        public float MeleeHitChance { get; internal set; }

        /// <summary>近战一击最大伤害。</summary>
        public float MeleeMaxDamage { get; internal set; }

        /// <summary>是否包含火焰额外伤害。</summary>
        public bool HasFlameDamage { get; internal set; }

        /// <summary>是否包含 EMP 额外伤害。</summary>
        public bool HasEmpDamage { get; internal set; }

        /// <summary>判断快照是否可以作为 NIT 或原版显示输入。</summary>
        public bool IsValid
        {
            get
            {
                return ProjectionVersion > 0
                    && !string.IsNullOrWhiteSpace(ResultId)
                    && (WeaponMode == AutomaticAttackDisplayMode.Melee
                        || WeaponMode == AutomaticAttackDisplayMode.Ranged);
            }
        }
    }
}
