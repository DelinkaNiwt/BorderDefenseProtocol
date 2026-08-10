namespace BDP.Core.Trigger
{
    /// <summary>
    /// 提供给上层读取的单个芯片形态选项快照。
    /// 它只复制显示所需元数据，不暴露可被改写的定义配置。
    /// </summary>
    public sealed class ChipModeOptionSnapshot
    {
        /// <summary>
        /// 当前形态的稳定内部键。
        /// </summary>
        public string ModeKey { get; internal set; }

        /// <summary>
        /// 当前形态面向玩家显示的名称。
        /// </summary>
        public string DisplayLabel { get; internal set; }

        /// <summary>
        /// 当前形态按钮使用的可选贴图路径。
        /// 为空时由上层回退芯片物品图标。
        /// </summary>
        public string GizmoIconTexPath { get; internal set; }
    }
}
