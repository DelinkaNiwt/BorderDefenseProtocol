namespace BDP.Core.Chips
{
    /// <summary>运行时投射物属性的中性可选覆盖块。</summary>
    public sealed class ProjectileOverrides
    {
        /// <summary>伤害倍率。</summary>
        public float? damageMultiplier;

        /// <summary>飞行速度倍率。</summary>
        public float? speedMultiplier;

        /// <summary>停止力倍率。</summary>
        public float? stoppingPowerMultiplier;

        /// <summary>拖尾预设 DefName 覆盖。</summary>
        public string beamTrailPreset;

        /// <summary>伤害类型 DefName 覆盖。</summary>
        public string damageDef;
    }
}
