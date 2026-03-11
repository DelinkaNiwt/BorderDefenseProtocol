using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 圆形范围指示器实现。
    /// 使用 RimWorld 原版 GenDraw.DrawRadiusRing API 绘制圆形范围。
    /// </summary>
    public class CircleAreaIndicator : IAreaIndicator
    {
        /// <summary>
        /// 在指定位置绘制圆形范围指示器。
        /// </summary>
        /// <param name="center">圆心位置。</param>
        /// <param name="map">当前地图（当前版本未使用，为未来扩展预留）。</param>
        /// <param name="config">指示器配置。</param>
        public void Draw(IntVec3 center, Map map, AreaIndicatorConfig config)
        {
            // 参数验证
            if (config == null || map == null)
                return;

            // 获取半径
            float radius = config.customRadius;
            if (radius <= 0f)
                return;

            // 使用原版 API 绘制圆形范围
            GenDraw.DrawRadiusRing(center, radius, config.color);
        }
    }
}
