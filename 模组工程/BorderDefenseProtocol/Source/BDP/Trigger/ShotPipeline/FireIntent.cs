using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击意图：IShotFireModule产出
    /// </summary>
    public struct FireIntent
    {
        // 投射物
        public ThingDef OverrideProjectileDef;

        // 发射参数修正
        public float SpreadRadius;
        public float DamageMultiplier;   // 默认1.0
        public float SpeedMultiplier;    // 默认1.0

        // 资源消耗
        public float TrionCost;
        public bool SkipTrionConsumption;

        // 控制标志
        public bool AbortShot;
        public string AbortReason;

        // 弹道管线注入
        public bool EnableAutoRoute;
        public ThingDef AutoRouteProjectileDef;

        public static FireIntent Default => new FireIntent
        {
            DamageMultiplier = 1f,
            SpeedMultiplier = 1f
        };
    }
}
