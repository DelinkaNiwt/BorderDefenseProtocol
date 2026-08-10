using UnityEngine;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达视觉预设中的枪口锚点配置。
    /// 枪口偏移始终按 aimAngle（瞄准角）旋转，而不是按贴图装饰角旋转。
    /// </summary>
    public sealed class ExpressionVisualMuzzleConfig
    {
        /// <summary>
        /// 当前预设是否允许作为远程武器枪口来源。
        /// false 时绘制仍可生效，但投射物发射点不从本预设解算。
        /// </summary>
        public bool IsRangedWeapon = false;

        /// <summary>
        /// 主侧默认枪口局部偏移。
        /// X 表示左右，Y 表示高度，Z 表示朝向目标的前后。
        /// </summary>
        public Vector3 MuzzleOffset = Vector3.zero;

        /// <summary>
        /// 副侧是否使用专门的枪口偏移覆盖。
        /// false 时副侧使用默认偏移并按瞄准镜像自动修正 X。
        /// </summary>
        public bool HasSubHandMuzzleOffsetOverride = false;

        /// <summary>
        /// 副侧专用枪口局部偏移。
        /// 只有 HasSubHandMuzzleOffsetOverride 为 true 时才生效。
        /// </summary>
        public Vector3 SubHandMuzzleOffsetOverride = Vector3.zero;

        /// <summary>
        /// 枪口解算完成后额外叠加的世界空间偏移。
        /// 它只服务少量特效对齐，不参与手侧镜像。
        /// </summary>
        public Vector3 ExtraWorldOffset = Vector3.zero;
    }
}
