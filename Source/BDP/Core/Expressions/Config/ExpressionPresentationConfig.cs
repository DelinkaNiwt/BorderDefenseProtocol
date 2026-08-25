namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达条目的最小表现配置块。
    /// 它只承载轻量表现引用，不把大段视觉作者参数直接塞进表达条目。
    /// </summary>
    public sealed class ExpressionPresentationConfig
    {
        /// <summary>
        /// 当前手动攻击入口按钮要使用的贴图路径。
        /// 留空时由下游按既定回退规则解析。
        /// </summary>
        public string ManualEntryIconTexPath;

        /// <summary>
        /// 当前条目默认使用的单侧视觉预设 DefName。
        /// 留空表示该条目不主动提供手持视觉预设。
        /// </summary>
        public string VisualPresetDefName;

        /// <summary>
        /// 当前条目对基础视觉图层的局部覆盖预设 DefName。
        /// 主贴图和附加层取覆盖预设；姿态、握持和其它基础视觉字段继续取 VisualPresetDefName。
        /// </summary>
        public string VisualGraphicOverrideDefName;

        /// <summary>
        /// 当前条目参与双持或组合等复合表达时使用的视觉预设 DefName。
        /// 留空表示复合表象继续沿用单侧预设或下游默认规则。
        /// </summary>
        public string CompositeVisualPresetDefName;

        /// <summary>
        /// 当前条目是否强制压制宿主原装备贴图。
        /// 这是作者声明的静态表现意图，不是运行时执行态。
        /// </summary>
        public bool ForceSuppressHostEquipment = false;

        /// <summary>
        /// 当前条目的视觉优先级。
        /// 数值越大越靠后绘制。
        /// </summary>
        public int VisualPriority = 0;
    }
}
