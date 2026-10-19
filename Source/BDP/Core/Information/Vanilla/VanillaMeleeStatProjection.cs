using System;
using BDP.Core.AttackExecution;
using RimWorld;
using Verse;

namespace BDP.Core.Information.Vanilla
{
    /// <summary>
    /// 一把近战 Tool 的成品统计投影。
    /// 原版伤害和护甲穿透调整入口继续复用，额外只计算多击周期。
    /// </summary>
    public sealed class VanillaMeleeStatProjection
    {
        /// <summary>原版近战每秒伤害字段的显示标签。</summary>
        public static string DamageLabel
        {
            get { return StatDefOf.MeleeWeapon_AverageDPS.LabelCap.ToString(); }
        }

        /// <summary>原版近战护甲穿透字段的显示标签。</summary>
        public static string ArmorPenetrationLabel
        {
            get
            {
                StatDef stat = DefDatabase<StatDef>.GetNamedSilentFail(
                    "MeleeWeapon_AverageArmorPenetration");
                return stat != null
                    ? stat.LabelCap.ToString()
                    : "MeleeWeapon_AverageArmorPenetration".Translate().ToString();
            }
        }

        /// <summary>当前工具最终单次伤害。</summary>
        public float Damage { get; private set; }

        /// <summary>当前工具最终护甲穿透。</summary>
        public float ArmorPenetration { get; private set; }

        /// <summary>当前工具一轮攻击的命中次数。</summary>
        public int HitCount { get; private set; }

        /// <summary>当前工具连续命中的间隔游戏刻。</summary>
        public int HitIntervalTicks { get; private set; }

        /// <summary>当前工具原版攻击冷却秒数。</summary>
        public float CooldownSeconds { get; private set; }

        /// <summary>当前工具原版预热秒数。</summary>
        public float WarmupSeconds { get; private set; }

        /// <summary>当前工具一轮完整攻击的总耗时秒数。</summary>
        public float CycleSeconds { get; private set; }

        /// <summary>考虑多击后的当前工具近战每秒伤害。</summary>
        public float DamagePerSecond { get; private set; }

        /// <summary>
        /// 从逐 Tool 运行时表面建立统计投影。
        /// </summary>
        public static bool TryCreate(
            MeleeToolSurface surface,
            out VanillaMeleeStatProjection projection)
        {
            projection = null;
            if (surface?.Tool == null || surface.VerbProps == null)
            {
                return false;
            }

            VerbProperties verbProps = surface.VerbProps;
            Tool tool = surface.Tool;
            float damage = verbProps.AdjustedMeleeDamageAmount(
                tool,
                null,
                (ThingDef)null,
                null,
                null);
            float armorPenetration = verbProps.AdjustedArmorPenetration(
                tool,
                null,
                (ThingDef)null,
                null,
                null);
            float cooldown = verbProps.AdjustedCooldown(
                tool,
                null,
                (ThingDef)null,
                null);
            int hitCount = Math.Max(1, verbProps.burstShotCount);
            int hitIntervalTicks = Math.Max(0, verbProps.ticksBetweenBurstShots);
            float cycleSeconds = verbProps.warmupTime
                + cooldown
                + ((hitCount - 1) * hitIntervalTicks / 60f);

            projection = new VanillaMeleeStatProjection
            {
                Damage = damage,
                ArmorPenetration = armorPenetration,
                HitCount = hitCount,
                HitIntervalTicks = hitIntervalTicks,
                CooldownSeconds = cooldown,
                WarmupSeconds = verbProps.warmupTime,
                CycleSeconds = cycleSeconds,
                DamagePerSecond = cycleSeconds > 0f
                    ? damage * hitCount / cycleSeconds
                    : 0f
            };
            return true;
        }

        /// <summary>从单个近战统计表面读取逐 Tool 投影。</summary>
        public static bool TryCreate(
            VanillaWeaponStatSurface surface,
            out VanillaMeleeStatProjection projection)
        {
            return TryCreate(surface?.MeleeToolSurface, out projection);
        }
    }
}
