using System;
using Verse;

namespace BDP.Content.Shield
{
    /// <summary>人物一组护盾的持久真值；比例让来源数量和姿态变化不刷新耐久。</summary>
    public sealed class EnergyShieldDurabilityState : IExposable
    {
        /// <summary>由护盾定义声明的组键，同组来源共用本记录。</summary>
        public string GroupKey;
        /// <summary>剩余比例；双精度避免逐刻自然恢复的累加误差拖后回满时点。</summary>
        private double durabilityFraction = 1d;
        /// <summary>绝对恢复完成游戏刻，零表示未处于破盾恢复。</summary>
        private int recoveryUntilTick;
        /// <summary>最近启用来源配置的恢复时长，供外部强制破盾共用。</summary>
        public int RecoveryTicks = 600;
        /// <summary>每秒自然恢复量是否按耐力上限比例解释；默认使用固定点数。</summary>
        public bool NaturalRecoveryUsePercentage;
        /// <summary>每秒自然恢复量；固定模式为点数，百分比模式中 0.1 表示 10%，零关闭恢复。</summary>
        public float NaturalRecoveryPerSecond = 24f;
        /// <summary>最近有效来源配置的受击恢复延迟，单位游戏刻。</summary>
        public int NaturalRecoveryDelayTicks;
        /// <summary>最后实际扣除耐力的游戏刻；未扣过耐力时为负一。</summary>
        private int lastBlockedTick = -1;
        /// <summary>最后结算恢复的游戏刻，防止同组来源或重复查询重复推进。</summary>
        private int lastRecoveryTick;
        /// <summary>当前或最近防护宿主上限；固定恢复据此换算比例，关闭后沿用。</summary>
        public float LastMaximumDurability;

        /// <summary>当前剩余比例。</summary>
        public float DurabilityFraction { get { return (float)durabilityFraction; } }
        /// <summary>当前恢复完成游戏刻。</summary>
        public int RecoveryUntilTick { get { return recoveryUntilTick; } }
        /// <summary>判断在指定游戏刻是否仍破损；查询不修改状态。</summary>
        public bool IsBrokenAt(int tick) { return recoveryUntilTick > tick; }
        /// <summary>把剩余比例投影到当前有效来源上限。</summary>
        public float CurrentDurability(float maximum) { return (float)(Math.Max(0f, maximum) * durabilityFraction); }

        /// <summary>仅在伤害已完整吸收后调用；返回本次是否从完好转入破盾。</summary>
        public bool ApplyBlockedDamage(float damage, float maximum, float multiplier, int tick, int resetTicks)
        {
            RecoverIfDue(tick);
            if (IsBrokenAt(tick) || maximum <= 0f || damage <= 0f || multiplier <= 0f) return false;
            LastMaximumDurability = maximum;
            double remaining = maximum * durabilityFraction - (double)damage * multiplier;
            // 按实际耐久扣减后再存比例，避免小额多次相减留下足以多挡一击的浮点残量。
            durabilityFraction = remaining <= Math.Max(0.0001f, maximum * 0.000001f)
                ? 0d : remaining / maximum;
            lastBlockedTick = tick;
            return durabilityFraction <= 0f && TryBreak(tick, resetTicks);
        }

        /// <summary>统一受击耗尽和外部破盾；重复调用不延长既有计时。</summary>
        public bool TryBreak(int tick, int resetTicks)
        {
            RecoverIfDue(tick);
            if (IsBrokenAt(tick)) return false;
            durabilityFraction = 0d;
            recoveryUntilTick = tick + Math.Max(1, resetTicks);
            return true;
        }

        /// <summary>统一推进恢复；仅破盾到期回满返回真，普通耐力增长不触发表达重建。</summary>
        public bool RecoverIfDue(int tick)
        {
            if (tick < lastRecoveryTick) return false;
            int previousTick = lastRecoveryTick;
            lastRecoveryTick = tick;
            if (recoveryUntilTick > 0)
            {
                if (tick < recoveryUntilTick) return false;
                recoveryUntilTick = 0;
                durabilityFraction = 1d;
                return true;
            }
            if (durabilityFraction >= 1d || NaturalRecoveryPerSecond <= 0f || lastBlockedTick < 0)
                return false;

            // 配置和计时属于共用持久盾组；关闭芯片、切换姿态不停止或重开恢复。
            // 固定点数只属于整个盾组，不因同组来源数量增加而叠加恢复速度。
            double fractionPerSecond = NaturalRecoveryUsePercentage ? NaturalRecoveryPerSecond
                : LastMaximumDurability > 0f ? (double)NaturalRecoveryPerSecond / LastMaximumDurability : 0d;
            int startTick = Math.Max(previousTick, lastBlockedTick + Math.Max(0, NaturalRecoveryDelayTicks));
            if (tick > startTick)
                durabilityFraction = Math.Min(1d, durabilityFraction
                    + (tick - startTick) * fractionPerSecond / 60d);
            return false;
        }

        /// <summary>只存真值与最近来源配置，不保存临时宿主或运行时引用。</summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref GroupKey, "groupKey");
            Scribe_Values.Look(ref durabilityFraction, "durabilityFraction", 1d);
            Scribe_Values.Look(ref recoveryUntilTick, "recoveryUntilTick");
            Scribe_Values.Look(ref RecoveryTicks, "recoveryTicks", 600);
            Scribe_Values.Look(ref NaturalRecoveryUsePercentage, "naturalRecoveryUsePercentage");
            Scribe_Values.Look(ref NaturalRecoveryPerSecond, "naturalRecoveryPerSecond", 24f);
            Scribe_Values.Look(ref NaturalRecoveryDelayTicks, "naturalRecoveryDelayTicks");
            Scribe_Values.Look(ref lastBlockedTick, "lastBlockedTick", -1);
            Scribe_Values.Look(ref lastRecoveryTick, "lastRecoveryTick");
            Scribe_Values.Look(ref LastMaximumDurability, "lastMaximumDurability");
        }
    }
}
