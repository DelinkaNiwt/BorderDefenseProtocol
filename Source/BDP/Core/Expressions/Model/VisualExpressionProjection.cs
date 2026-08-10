using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 视觉读取投影结果。
    /// 它只保存视觉层当前应如何理解表达结果。
    /// </summary>
    internal sealed class VisualExpressionProjection
    {
        /// <summary>
        /// 当前视觉层关系类型。
        /// </summary>
        public VisualExpressionRelationKind RelationKind { get; set; }

        /// <summary>
        /// 当前常驻视觉条目集合。
        /// 这里只保存静态发布结果，不承载任何攻击执行动态真值。
        /// </summary>
        public IReadOnlyList<VisualResidentEntry> ResidentEntries { get; set; }

        /// <summary>
        /// 当前正式表达中可用的激活武器芯片实例数量。
        /// 一枚芯片声明主副两个 Verb 时仍只算一个实例。
        /// </summary>
        public int ActiveWeaponChipInstanceCount { get; set; }

        /// <summary>
        /// 当前宿主原装备贴图的绘制策略。
        /// </summary>
        public HostEquipmentRenderMode HostEquipmentRenderMode { get; set; }

        /// <summary>
        /// 当前视觉焦点读取政策。
        /// 真正的焦点结果标识由运行时动态状态提供。
        /// </summary>
        public VisualExecutionFocusPolicy ExecutionFocusPolicy { get; set; }

        /// <summary>
        /// 当前枪口锚点读取政策。
        /// 真正的源结果标识由运行时动态状态提供。
        /// </summary>
        public VisualMuzzleFollowPolicy MuzzleFollowPolicy { get; set; }
    }
}
