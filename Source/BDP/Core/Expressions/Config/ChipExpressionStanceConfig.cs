using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片某个形态内部的单个姿态配置。
    /// 它只声明姿态自己的附加条目，不重复形态公共条目。
    /// </summary>
    public sealed class ChipExpressionStanceConfig
    {
        /// <summary>
        /// 当前姿态在所属形态内的稳定键。
        /// </summary>
        public string StanceKey;

        /// <summary>
        /// 当前姿态面向玩家显示的名称。
        /// 与 DisplayLabelKey 至少填写一项。
        /// </summary>
        public string DisplayLabel;

        /// <summary>
        /// 当前姿态面向玩家显示的可选语言包键。
        /// 填写后由内容层优先使用它。
        /// </summary>
        public string DisplayLabelKey;

        /// <summary>
        /// 当前姿态 Gizmo（游戏操作按钮）使用的可选贴图路径。
        /// 为空时由内容层回退芯片物品图标。
        /// </summary>
        public string GizmoIconTexPath;

        /// <summary>
        /// 当前姿态在所属形态公共条目之后追加启用的表达条目标识。
        /// </summary>
        public List<string> ActiveEntryIds;
    }
}
