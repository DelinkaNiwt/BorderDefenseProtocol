namespace BDP.Content.Shield
{
    /// <summary>供调用方与开发工具读取的不可写盾组快照，不暴露存档记录。</summary>
    public sealed class EnergyShieldSnapshot
    {
        /// <summary>组键。</summary>
        public string GroupKey { get; private set; }
        /// <summary>当前剩余比例。</summary>
        public float DurabilityFraction { get; private set; }
        /// <summary>当前有效宿主上限；关闭或破盾时为最近宿主上限，仅作诊断参考。</summary>
        public float MaximumDurability { get; private set; }
        /// <summary>当前比例投影出的剩余耐久。</summary>
        public float CurrentDurability { get; private set; }
        /// <summary>绝对恢复完成刻。</summary>
        public int RecoveryUntilTick { get; private set; }
        /// <summary>最近来源配置的恢复总时长，单位游戏刻；供恢复进度展示。</summary>
        public int RecoveryDurationTicks { get; private set; }
        /// <summary>是否仍处于恢复。</summary>
        public bool IsBroken { get; private set; }
        /// <summary>当前是否存在防护宿主。</summary>
        public bool HasDefenseHost { get; private set; }
        /// <summary>复制真实状态；查询不推进或重置任何游戏状态。</summary>
        internal EnergyShieldSnapshot(EnergyShieldDurabilityState state, float maximum, int tick, bool hasHost)
        {
            GroupKey = state.GroupKey;
            IsBroken = state.IsBrokenAt(tick);
            DurabilityFraction = !IsBroken && state.RecoveryUntilTick > 0 ? 1f : state.DurabilityFraction;
            MaximumDurability = maximum;
            CurrentDurability = maximum * DurabilityFraction;
            RecoveryUntilTick = state.RecoveryUntilTick;
            RecoveryDurationTicks = state.RecoveryTicks;
            HasDefenseHost = hasHost;
        }
    }
}
