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

        /// <summary>
        /// 深复制当前投射物覆盖块。
        /// 组合结果和制造结果都必须拥有自己的可变配置副本。
        /// </summary>
        public ProjectileOverrides Clone()
        {
            return new ProjectileOverrides
            {
                damageMultiplier = damageMultiplier,
                speedMultiplier = speedMultiplier,
                stoppingPowerMultiplier = stoppingPowerMultiplier,
                beamTrailPreset = beamTrailPreset,
                damageDef = damageDef
            };
        }
    }
}
