using BDP.Core.Expressions;
using BDP.Core.Trigger.Runtime;
using Verse;

namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 视觉姿态解析请求。
    /// 它把已发布静态投影、运行时动态状态和原版装备姿态样本收束成一次纯计算输入。
    /// </summary>
    internal sealed class VisualPoseRequest
    {
        /// <summary>
        /// 当前要解析的常驻视觉条目。
        /// </summary>
        public VisualResidentEntry Entry { get; set; }

        /// <summary>
        /// 当前条目实际使用的视觉预设。
        /// </summary>
        public ExpressionVisualPresetDef Preset { get; set; }

        /// <summary>
        /// 当前条目的视觉图层局部覆盖预设。
        /// 姿态解析仍以 Preset 为准，主贴图和附加层从该预设读取。
        /// </summary>
        public ExpressionVisualPresetDef GraphicOverridePreset { get; set; }

        /// <summary>
        /// 当前 Trigger 已发布的视觉运行时状态。
        /// </summary>
        public TriggerVisualRuntimeState RuntimeState { get; set; }

        /// <summary>
        /// 当前条目从原版攻击时序解析出的武器动作阶段快照。
        /// 留空时姿态解析按 Idle（空闲）处理，保持旧调用方行为。
        /// </summary>
        public WeaponVisualStageSnapshot WeaponStageSnapshot { get; set; }

        /// <summary>
        /// 当前装备姿态样本。
        /// 它必须与请求所属投影版本一致。
        /// </summary>
        public EquipmentPoseSample PoseSample { get; set; }

        /// <summary>
        /// 当前绘制用来取色的芯片实例。
        /// </summary>
        public Thing SourceThing { get; set; }

        /// <summary>
        /// 当前宿主装备自身的 equippedAngleOffset。
        /// 视觉绘制要保持和原版 DrawEquipmentAiming 一致。
        /// </summary>
        public float EquippedAngleOffset { get; set; }

        /// <summary>
        /// 当前条目是否命中执行焦点。
        /// </summary>
        public bool IsExecutionActive { get; set; }

        /// <summary>
        /// 当前条目是否命中 emit 源焦点。
        /// </summary>
        public bool IsMuzzleActive { get; set; }
    }
}
