namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 投射物路径快照支持的路径类型。
    /// 第一版只开放直线与三次贝塞尔曲线。
    /// </summary>
    public enum ProjectileFlightPathKind
    {
        /// <summary>
        /// 线性路径。
        /// </summary>
        Linear = 0,

        /// <summary>
        /// 三次贝塞尔路径。
        /// </summary>
        CubicBezier = 1
    }
}
