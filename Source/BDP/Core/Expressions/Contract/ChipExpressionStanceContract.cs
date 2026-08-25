using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 主模组正式承认的单个芯片姿态契约。
    /// 姿态只在所属形态内部有效。
    /// </summary>
    public sealed class ChipExpressionStanceContract
    {
        /// <summary>
        /// 当前姿态在所属形态内的稳定键。
        /// </summary>
        public string StanceKey;

        /// <summary>
        /// 当前姿态面向玩家显示的名称。
        /// </summary>
        public string DisplayLabel;

        /// <summary>
        /// 当前姿态面向玩家显示的可选语言包键。
        /// </summary>
        public string DisplayLabelKey;

        /// <summary>
        /// 当前姿态 Gizmo（游戏操作按钮）使用的可选贴图路径。
        /// </summary>
        public string GizmoIconTexPath;

        /// <summary>
        /// 当前姿态在形态公共条目之后追加启用的表达条目标识。
        /// </summary>
        public List<string> ActiveEntryIds;
    }
}
