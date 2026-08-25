using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片表达配置中的单个形态块。
    /// 它只从统一表达目录中选择本形态启用的条目。
    /// </summary>
    public sealed class ChipExpressionModeConfig
    {
        /// <summary>
        /// 当前形态自己的稳定键。
        /// </summary>
        public string ModeKey;

        /// <summary>
        /// 当前形态面向玩家显示的名称。
        /// 多形态芯片必须填写。
        /// </summary>
        public string DisplayLabel;

        /// <summary>
        /// 当前形态面向玩家显示的可选语言包键。
        /// 填写后由内容层优先使用它；为空时回退 DisplayLabel。
        /// </summary>
        public string DisplayLabelKey;

        /// <summary>
        /// 当前形态 Gizmo（游戏操作按钮）使用的可选贴图路径。
        /// 为空时由上层内容回退芯片物品图标。
        /// </summary>
        public string GizmoIconTexPath;

        /// <summary>
        /// 当前形态按顺序启用的表达条目标识。
        /// 书写顺序就是正式结果的稳定顺序。
        /// </summary>
        public List<string> ActiveEntryIds;

        /// <summary>
        /// 当前形态尚无运行中姿态时采用的默认姿态键。
        /// 没有姿态列表时必须留空。
        /// </summary>
        public string DefaultStanceKey;

        /// <summary>
        /// 当前形态内部可切换的姿态集合。
        /// 姿态条目会追加在形态公共 ActiveEntryIds 之后。
        /// </summary>
        public List<ChipExpressionStanceConfig> Stances;
    }
}
