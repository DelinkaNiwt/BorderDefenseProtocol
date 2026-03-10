using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击结果：宿主合并所有FireIntent后产出
    /// </summary>
    public class FireResult
    {
        public ThingDef ProjectileDef;
        public float SpreadRadius;
        public float DamageMultiplier = 1f;
        public float SpeedMultiplier = 1f;
        public float TrionCost;
        public bool SkipTrionConsumption;
        public bool EnableAutoRoute;
        public ThingDef AutoRouteProjectileDef;
        public bool Abort;
        public string AbortReason;
    }
}
