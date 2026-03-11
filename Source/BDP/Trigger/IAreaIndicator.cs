using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 范围指示器接口，定义绘制范围指示器的标准方法。
    /// 为未来扩展不同类型的指示器（扇形、弧形等）预留接口。
    /// </summary>
    public interface IAreaIndicator
    {
        /// <summary>
        /// 在指定位置绘制范围指示器。
        /// </summary>
        /// <param name="center">指示器中心位置。</param>
        /// <param name="map">当前地图。</param>
        /// <param name="config">指示器配置。</param>
        void Draw(IntVec3 center, Map map, AreaIndicatorConfig config);
    }
}
