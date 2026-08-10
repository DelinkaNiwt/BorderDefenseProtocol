using UnityEngine;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// South/North（南北朝向）下的手持视觉姿态配置。
    /// 所有偏移以”主侧 South 朝向”为基准定义，副侧由 VisualPoseResolver 运行时自动镜像。
    /// </summary>
    public sealed class ExpressionVisualSouthNorthPoseConfig
    {
        /// <summary>
        /// 主侧 South 基准偏移。
        /// X 控制主副手左右分离，Z 控制相对持握点前后位置。
        /// 副侧装载时 X 自动取反。
        /// </summary>
        public Vector3 DefaultOffset = Vector3.zero;

        /// <summary>
        /// 主贴图默认装饰角度。
        /// 该角度只影响贴图旋转，不参与枪口偏移旋转。
        /// </summary>
        public float DefaultAngle = 0f;

        /// <summary>
        /// 主贴图默认高度偏移。
        /// South 时正值靠前，North 时会按旧版规则取反到角色身后。
        /// </summary>
        public float DefaultAltitudeOffset = 0.1f;

        /// <summary>
        /// South 朝向额外 Z 微调。
        /// </summary>
        public float SouthZAdjust = 0f;

        /// <summary>
        /// North 朝向额外 Z 微调。
        /// </summary>
        public float NorthZAdjust = 0f;

        /// <summary>
        /// 副侧额外装饰角度。
        /// 默认不附加角度；作者可在视觉预设中显式配置主副侧角度差。
        /// </summary>
        public float SubHandAngleOffset = 0f;

        /// <summary>
        /// 是否应用手侧镜像。
        /// 手侧镜像由主/副手和南北朝向共同决定，独立于瞄准半区。
        /// </summary>
        public bool HandMirror = true;

        /// <summary>
        /// North 朝向是否额外做一次整枪最终镜像。
        /// 它服务那些作者贴图自身带明显斜向基准的情况：背面时希望整把枪与正面形成左右相反的外在斜率。
        /// 开启后会抑制旧式按主副手裁决的 South/North 手侧镜像，避免北面出现双重翻转。
        /// </summary>
        public bool MirrorOnNorth = false;
    }
}
