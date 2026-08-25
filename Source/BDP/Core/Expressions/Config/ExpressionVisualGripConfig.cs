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
        /// 握持点相对主贴图中心的局部偏移。
        /// X 表示左右，Y 表示高度，Z 表示武器朝向上的前后。
        /// </summary>
        public Vector3 GripOffset = Vector3.zero;

        /// <summary>
        /// 是否把握持点作为姿态配置定位的原点。
        /// false 时姿态偏移继续定位主贴图中心，保证旧预设行为不变。
        /// </summary>
        public bool UseAsPoseOrigin = false;
    }
}
