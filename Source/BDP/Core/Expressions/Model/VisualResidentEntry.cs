using BDP.Core.Trigger;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 已发布视觉投影中的单个常驻视觉条目。
    /// 它把正式表达结果映射到可绘制预设，但不携带当轮执行状态。
    /// </summary>
    internal sealed class VisualResidentEntry
    {
        /// <summary>
        /// 当前常驻视觉条目对应的正式结果标识。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前结果来自的芯片槽位追踪。
        /// 视觉层只用它回到芯片实例和侧别，不重新解释表达条件。
        /// </summary>
        public ExpressionSourceReference SourceReference { get; set; }

        /// <summary>
        /// 当前视觉条目所属的 Trigger 侧别。
        /// Main 固定按右手处理，Sub 固定按左手处理。
        /// </summary>
        public TriggerSide Side { get; set; }

        /// <summary>
        /// 当前视觉条目所属侧内槽位索引。
        /// </summary>
        public int SlotIndex { get; set; }

        /// <summary>
        /// 当前单侧结果使用的视觉预设 DefName。
        /// 留空表示该结果不主动绘制手持贴图。
        /// </summary>
        public string VisualPresetDefName { get; set; }

        /// <summary>
        /// 当前结果参与复合表达时使用的视觉预设 DefName。
        /// 它用于组合/双持表象覆写，不改变单侧结果自身身份。
        /// </summary>
        public string CompositeVisualPresetDefName { get; set; }

        /// <summary>
        /// 当前视觉条目对应的主副攻击身份。
        /// 单武器贴图替换用它优先选择主攻击贴图。
        /// </summary>
        public VerbAttackRole VerbAttackRole { get; set; }

        /// <summary>
        /// 当前结果是否强制压制宿主原装备贴图。
        /// 这是作者声明的静态偏好，最终是否跳过原版绘制由投影策略统一裁决。
        /// </summary>
        public bool ForceSuppressHostEquipment { get; set; }

        /// <summary>
        /// 当前视觉条目的绘制优先级。
        /// 数值越大越靠后绘制，便于前景层覆盖背景层。
        /// </summary>
        public int VisualPriority { get; set; }

        /// <summary>
        /// 当前视觉条目是否有可解析的单侧视觉预设。
        /// </summary>
        public bool HasVisualPreset
        {
            get { return !string.IsNullOrWhiteSpace(VisualPresetDefName); }
        }
    }
}
