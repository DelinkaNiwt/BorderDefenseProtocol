using BDP.Core.Expressions;
using BDP.Core.Trigger.Runtime;
using Verse;

namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 视觉姿态解析请求。
    /// 它把视觉条目、已校验的实时攻击姿态真值和原版装备姿态样本收束成一次纯计算输入。
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
        /// 当前 Pawn 是否处于身份匹配的 BDP 实时攻击姿态。
        /// 它继续控制原有 Pawn 级待机镜像规则，不以仍未清理的执行记录推断攻击正在进行。
        /// </summary>
        public bool IsMatchingAttackStanceActive { get; set; }

        /// <summary>
        /// 当前视觉条目是否参与这次 BDP 攻击；未显式提供时沿用 Pawn 级状态。
        /// 它只控制逐条目的静止专用装饰角，不改变既有的 Pawn 级镜像规则。
        /// </summary>
        public bool? IsMatchingEntryAttackStanceActive { get; set; }

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
