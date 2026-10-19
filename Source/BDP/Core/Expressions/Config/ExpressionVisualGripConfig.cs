using UnityEngine;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达视觉预设中的握持锚点配置。
    /// 它描述握持点相对主贴图中心的位置，不直接改变武器绘制姿态。
    /// </summary>
    public sealed class ExpressionVisualGripConfig
    {
        /// <summary>
        /// 握持点相对主贴图中心的局部坐标。
        /// X、Z 分别按贴图纵向和横向画布比例填写；Y 是不随贴图缩放的世界高度偏移。
        /// </summary>
        public Vector3 GripOffset = Vector3.zero;

        /// <summary>
        /// 是否把握持点作为姿态配置定位的原点。
        /// false 时姿态偏移继续定位主贴图中心，保证旧预设行为不变。
        /// </summary>
        public bool UseAsPoseOrigin = false;
    }
}
