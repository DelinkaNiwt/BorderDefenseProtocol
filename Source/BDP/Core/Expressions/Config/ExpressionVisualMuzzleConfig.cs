using UnityEngine;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达视觉预设中的枪口锚点配置。
    /// 枪口点按贴图画布比例记录，再随实际贴图尺寸、缩放、角度和镜像一起变换。
    /// </summary>
    public sealed class ExpressionVisualMuzzleConfig
    {
        /// <summary>
        /// 当前预设是否允许作为远程武器枪口来源。
        /// false 时绘制仍可生效，但投射物发射点不从本预设解算。
        /// </summary>
        public bool IsRangedWeapon = false;

        /// <summary>
        /// 主侧默认枪口贴图局部坐标。
        /// X、Z 分别按贴图纵向和横向画布比例填写；Y 是不随贴图缩放的世界高度偏移。
        /// </summary>
        public Vector3 MuzzleOffset = Vector3.zero;

        /// <summary>
        /// 副侧是否使用专门的枪口偏移覆盖。
        /// false 时副侧使用默认偏移，并跟随最终贴图镜像。
        /// </summary>
        public bool HasSubHandMuzzleOffsetOverride = false;

        /// <summary>
        /// 副侧专用枪口贴图局部坐标，单位与 MuzzleOffset 相同。
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
