using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 飞行重定向配置——挂在ThingDef.modExtensions上。
    /// 控制ApplyFlightRedirect中的origin后退距离和远距离策略参数。
    /// 无此配置时宿主使用默认值。
    /// </summary>
    public class FlightRedirectConfig : DefModExtension
    {
        /// <summary>origin后退距离（格），恢复vanilla沿途拦截。默认6f。</summary>
        public float originOffset = 6f;

        /// <summary>远距离时固定tick数的速度倍率。默认3f。</summary>
        public float farDistanceSpeedMult = 3f;

        /// <summary>远距离时固定tick数。默认60。</summary>
        public int farDistanceFixedTicks = 60;
    }
}
