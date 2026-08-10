namespace BDP.Core.Expressions
{
    /// <summary>
    /// East/West（东西朝向）下的手持视觉姿态配置。
    /// 它使用侧身基准、前景手和背景手的分离量来复刻旧 BDP 的四朝向表象。
    /// </summary>
    public sealed class ExpressionVisualEastWestPoseConfig
    {
        /// <summary>
        /// 侧身时两手共同的 X 基准。
        /// East 使用正值，West 自动取反。
        /// </summary>
        public float SideBaseX = 0f;

        /// <summary>
        /// 前景手/背景手相对 SideBaseX 的 X 分离量。
        /// 前景手靠近原点，背景手远离原点。
        /// </summary>
        public float SideDeltaX = 0f;

        /// <summary>
        /// 前景手/背景手的 Z 分离量。
        /// 前景手靠近玩家视角，背景手远离玩家视角。
        /// </summary>
        public float SideDeltaZ = 0f;

        /// <summary>
        /// 前景手高度偏移。
        /// </summary>
        public float FrontAltitudeOffset = 0.1f;

        /// <summary>
        /// 背景手高度偏移。
        /// </summary>
        public float BackAltitudeOffset = -0.1f;

        /// <summary>
        /// 主贴图默认装饰角度。
        /// </summary>
        public float DefaultAngle = 0f;

        /// <summary>
        /// 副侧额外装饰角度。
        /// 默认不附加角度；作者可在视觉预设中显式配置主副侧角度差。
        /// </summary>
        public float SubHandAngleOffset = 0f;

        /// <summary>
        /// 是否应用手侧镜像。
        /// 旧版东西朝向默认不手侧翻转，但这里保留显式开关。
        /// </summary>
        public bool HandMirror = false;
    }
}
