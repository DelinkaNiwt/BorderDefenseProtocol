namespace BDP.Core.Trigger
{
    /// <summary>
    /// 提供给上层读取的单个芯片姿态选项快照。
    /// 它只复制当前形态内的显示元数据，不暴露可改写配置。
    /// </summary>
    public sealed class ChipStanceOptionSnapshot
    {
        /// <summary>
        /// 当前姿态在所属形态内的稳定键。
        /// </summary>
        public string StanceKey { get; internal set; }

        /// <summary>
        /// 当前姿态面向玩家显示的名称。
        /// </summary>
        public string DisplayLabel { get; internal set; }

        /// <summary>
        /// 当前姿态按钮使用的可选贴图路径。
        /// </summary>
        public string GizmoIconTexPath { get; internal set; }
    }
}
