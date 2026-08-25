using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 主模组正式承认的单个芯片形态契约。
    /// 它只描述某个形态启用统一目录中的哪些条目。
    /// </summary>
    public sealed class ChipExpressionModeContract
    {
        /// <summary>
        /// 当前形态自己的稳定键。
        /// </summary>
        public string ModeKey;

        /// <summary>
        /// 当前形态面向玩家显示的名称。
        /// </summary>
        public string DisplayLabel;

        /// <summary>
        /// 当前形态面向玩家显示的可选语言包键。
        /// </summary>
        public string DisplayLabelKey;

        /// <summary>
        /// 当前形态 Gizmo（游戏操作按钮）使用的可选贴图路径。
        /// 为空时由上层内容回退芯片物品图标。
        /// </summary>
        public string GizmoIconTexPath;

        /// <summary>
        /// 当前形态按顺序启用的表达条目标识。
        /// </summary>
        public List<string> ActiveEntryIds;

        /// <summary>
        /// 当前形态没有运行中姿态时采用的默认姿态键。
        /// </summary>
        public string DefaultStanceKey;

        /// <summary>
        /// 当前形态正式承认的姿态契约集合。
        /// </summary>
        public List<ChipExpressionStanceContract> Stances;
    }
}
