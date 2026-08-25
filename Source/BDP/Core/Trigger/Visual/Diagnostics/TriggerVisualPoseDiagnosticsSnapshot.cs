using System.Collections.Generic;
using BDP.Core.Trigger;
using UnityEngine;
using Verse;

namespace BDP.Core.Trigger.Visual.Diagnostics
{
    /// <summary>
    /// Trigger 视觉姿态诊断快照。
    /// 它只承载一次读取结果，不持有运行时真值，也不参与正式绘制裁决。
    /// </summary>
    public sealed class TriggerVisualPoseDiagnosticsSnapshot
    {
        /// <summary>
        /// 当前快照是否找到可诊断的 Trigger 宿主。
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// 当前快照不可用或缺关键输入时的说明。
        /// </summary>
        public string UnavailableReason { get; set; }

        /// <summary>
        /// 当前被诊断 Pawn 的显示名。
        /// </summary>
        public string PawnLabel { get; set; }

        /// <summary>
        /// 当前 Pawn 或装备姿态样本的绘制朝向。
        /// </summary>
        public Rot4 Facing { get; set; }

        /// <summary>
        /// 当前表现投影版本号。
        /// </summary>
        public int ProjectionVersion { get; set; }

        /// <summary>
        /// 当前视觉运行时状态版本号。
        /// </summary>
        public int RuntimeProjectionVersion { get; set; }

        /// <summary>
        /// 当前视觉投影关系类型。
        /// </summary>
        public string RelationKind { get; set; }

        /// <summary>
        /// 当前宿主原装备贴图绘制策略。
        /// </summary>
        public string HostEquipmentRenderMode { get; set; }

        /// <summary>
        /// 当前执行焦点读取策略。
        /// </summary>
        public string ExecutionFocusPolicy { get; set; }

        /// <summary>
        /// 当前枪口焦点读取策略。
        /// </summary>
        public string MuzzleFollowPolicy { get; set; }

        /// <summary>
        /// 当前是否已采样到 DrawEquipmentAiming 的装备姿态。
        /// </summary>
        public bool HasPoseSample { get; set; }

        /// <summary>
        /// 当前装备姿态样本是否匹配表现投影版本。
        /// </summary>
        public bool PoseSampleMatchesProjection { get; set; }

        /// <summary>
        /// 当前被诊断 Pawn 的实时 DrawPos。
        /// 它对应旧版工具里的小人中心点，可用来对照 DrawLoc 与最终武器位置的偏移关系。
        /// </summary>
        public Vector3 PawnDrawPosition { get; set; }

        /// <summary>
        /// 原版 DrawEquipmentAiming 传入的装备持握绘制点。
        /// </summary>
        public Vector3 DrawLoc { get; set; }

        /// <summary>
        /// 原版 DrawEquipmentAiming 传入的瞄准角。
        /// </summary>
        public float AimAngle { get; set; }

        /// <summary>
        /// 当前装备姿态样本采集时的游戏 tick。
        /// </summary>
        public int SampleTick { get; set; }

        /// <summary>
        /// 当前宿主装备 ThingDef 上的 equippedAngleOffset。
        /// </summary>
        public float EquippedAngleOffset { get; set; }

        /// <summary>
        /// 当前是否缓存到最近一次正式发射原点。
        /// 该点位对应旧版枪口调试工具里的最终开枪位置点。
        /// </summary>
        public bool HasRecentLaunchOrigin { get; set; }

        /// <summary>
        /// 最近一次正式发射使用的最终世界原点。
        /// 这是兼容旧单点诊断的最后一条真实发射点；批量齐射应改看 RecentLaunchPoints。
        /// </summary>
        public Vector3 RecentLaunchOriginWorld { get; set; }

        /// <summary>
        /// 最近一次正式发射计划声明的世界偏移量。
        /// 这是兼容旧单点诊断的最后一条世界偏移；批量齐射应改看 RecentLaunchPoints。
        /// </summary>
        public Vector3 RecentLaunchOriginOffsetWorld { get; set; }

        /// <summary>
        /// 最近一次正式发射是否直接使用绝对世界原点。
        /// 这是兼容旧单点诊断的最后一条绝对原点标记；批量齐射应改看 RecentLaunchPoints。
        /// </summary>
        public bool RecentLaunchUsesAbsoluteOriginWorld { get; set; }

        /// <summary>
        /// 最近一次正式发射记录的 ResultId。
        /// 这是兼容旧单点诊断的最后一条结果标识；批量齐射应改看 RecentLaunchPoints。
        /// </summary>
        public string RecentLaunchResultId { get; set; }

        /// <summary>
        /// 最近一次正式发射记录采用的根原点。
        /// 它用于核对“理论中心是否真的是从同一个根点偏出来的”。
        /// </summary>
        public Vector3 RecentLaunchRootOriginWorld { get; set; }

        /// <summary>
        /// 最近一次正式发射记录采用的根原点来源类型。
        /// </summary>
        public string RecentLaunchRootSourceKind { get; set; }

        /// <summary>
        /// 最近一次正式发射记录的根原点回退原因。
        /// 若当前直接用了冻结绝对原点或实时枪口，则为 None。
        /// </summary>
        public string RecentLaunchRootFailureKind { get; set; }

        /// <summary>
        /// 最近一次正式发射记录的游戏 tick。
        /// 它用于判断最后一条单点诊断是否仍属于最近一次可诊断发射。
        /// </summary>
        public int RecentLaunchTick { get; set; }

        /// <summary>
        /// 最近一次正式发射批次的全部发射点快照。
        /// 每一项都同时携带理论中心原点与真实发射原点，用于齐射源点的细粒度可视化。
        /// </summary>
        public List<TriggerVisualEmissionLaunchPointSnapshot> RecentLaunchPoints { get; set; } =
            new List<TriggerVisualEmissionLaunchPointSnapshot>();

        /// <summary>
        /// 当前快照包含的常驻视觉条目诊断列表。
        /// </summary>
        public List<TriggerVisualResidentPoseDiagnosticsSnapshot> Residents { get; set; } =
            new List<TriggerVisualResidentPoseDiagnosticsSnapshot>();
    }

    /// <summary>
    /// 单个常驻视觉条目的姿态诊断快照。
    /// 它把条目身份、预设选择、最终姿态和配置原值放在同一行，便于盲验截图定位。
    /// </summary>
    public sealed class TriggerVisualResidentPoseDiagnosticsSnapshot
    {
        /// <summary>
        /// 当前常驻视觉条目对应的正式结果标识。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前条目所属 Trigger 侧别。
        /// </summary>
        public TriggerSide Side { get; set; }

        /// <summary>
        /// 当前条目所属侧内槽位索引。
        /// </summary>
        public int SlotIndex { get; set; }

        /// <summary>
        /// 当前条目单侧视觉预设 DefName。
        /// </summary>
        public string VisualPresetDefName { get; set; }

        /// <summary>
        /// 当前条目视觉图层局部覆盖预设 DefName。
        /// </summary>
        public string VisualGraphicOverrideDefName { get; set; }

        /// <summary>
        /// 当前条目复合视觉预设 DefName。
        /// </summary>
        public string CompositeVisualPresetDefName { get; set; }

        /// <summary>
        /// 当前关系下实际选用的视觉预设 DefName。
        /// </summary>
        public string ResolvedPresetDefName { get; set; }

        /// <summary>
        /// 当前预设是否成功从 DefDatabase 解析。
        /// </summary>
        public bool HasPreset { get; set; }

        /// <summary>
        /// 当前条目回溯到的芯片 ThingDef 名称。
        /// </summary>
        public string SourceThingDefName { get; set; }

        /// <summary>
        /// 当前条目回溯到的芯片显示名。
        /// </summary>
        public string SourceThingLabel { get; set; }

        /// <summary>
        /// 当前条目是否命中执行焦点。
        /// </summary>
        public bool IsExecutionActive { get; set; }

        /// <summary>
        /// 当前条目是否命中枪口 emit 源焦点。
        /// </summary>
        public bool IsMuzzleActive { get; set; }

        /// <summary>
        /// 当前条目解析出的武器动作阶段名称。
        /// </summary>
        public string WeaponActionStage { get; set; }

        /// <summary>
        /// 当前武器动作阶段的归一化进度。
        /// </summary>
        public float WeaponStageProgress01 { get; set; }

        /// <summary>
        /// 当前武器动作阶段剩余的原版游戏刻数。
        /// </summary>
        public int WeaponStageTicksRemaining { get; set; }

        /// <summary>
        /// 当前预设在该武器动作阶段是否可见。
        /// </summary>
        public bool WeaponStageVisible { get; set; }

        /// <summary>
        /// 当前条目是否成功解析出最终绘制姿态。
        /// </summary>
        public bool HasResolvedPose { get; set; }

        /// <summary>
        /// 当前条目最终主贴图世界绘制位置。
        /// </summary>
        public Vector3 ResolvedDrawPosition { get; set; }

        /// <summary>
        /// 当前条目最终主贴图绘制角度。
        /// </summary>
        public float ResolvedDrawAngle { get; set; }

        /// <summary>
        /// 当前条目最终使用的 Mesh 种类。
        /// </summary>
        public string MeshKind { get; set; }

        /// <summary>
        /// 当前条目最终主贴图绘制缩放。
        /// </summary>
        public float DrawScale { get; set; }

        /// <summary>
        /// 当前姿态是否由 aimAngle 触发原版瞄准镜像。
        /// </summary>
        public bool AimMirror { get; set; }

        /// <summary>
        /// 当前姿态是否由主副手规则触发手侧镜像。
        /// </summary>
        public bool HandMirror { get; set; }

        /// <summary>
        /// 当前条目是否解析出有效握持锚点。
        /// </summary>
        public bool HasGripAnchor { get; set; }

        /// <summary>
        /// 当前条目握持锚点世界坐标。
        /// </summary>
        public Vector3 GripWorldPosition { get; set; }

        /// <summary>
        /// 当前条目握持锚点局部偏移。
        /// </summary>
        public Vector3 GripLocalOffset { get; set; }

        /// <summary>
        /// 当前条目是否解析出有效枪口锚点。
        /// </summary>
        public bool HasMuzzleAnchor { get; set; }

        /// <summary>
        /// 当前条目枪口锚点世界坐标。
        /// </summary>
        public Vector3 MuzzleWorldPosition { get; set; }

        /// <summary>
        /// 当前条目枪口锚点局部偏移。
        /// </summary>
        public Vector3 MuzzleLocalOffset { get; set; }

        /// <summary>
        /// 当前预设 South/North 姿态默认偏移。
        /// </summary>
        public Vector3 SouthNorthDefaultOffset { get; set; }

        /// <summary>
        /// 当前预设 South/North 姿态默认装饰角。
        /// </summary>
        public float SouthNorthDefaultAngle { get; set; }

        /// <summary>
        /// 当前预设 South/North 姿态默认高度偏移。
        /// </summary>
        public float SouthNorthDefaultAltitudeOffset { get; set; }

        /// <summary>
        /// 当前预设 South 朝向额外 Z 微调。
        /// </summary>
        public float SouthZAdjust { get; set; }

        /// <summary>
        /// 当前预设 North 朝向额外 Z 微调。
        /// </summary>
        public float NorthZAdjust { get; set; }

        /// <summary>
        /// 当前预设 South/North 副手额外装饰角。
        /// </summary>
        public float SouthNorthSubHandAngleOffset { get; set; }

        /// <summary>
        /// 当前预设 South/North 是否允许手侧镜像。
        /// </summary>
        public bool SouthNorthHandMirror { get; set; }

        /// <summary>
        /// 当前预设 South/North 是否开启 North 整枪镜像。
        /// </summary>
        public bool SouthNorthMirrorOnNorth { get; set; }

        /// <summary>
        /// 当前预设 East/West 侧身 X 基准。
        /// </summary>
        public float SideBaseX { get; set; }

        /// <summary>
        /// 当前预设 East/West 侧身 Z 共同基准。
        /// </summary>
        public float SideBaseZ { get; set; }

        /// <summary>
        /// 当前预设 East/West 前后手 X 分离量。
        /// </summary>
        public float SideDeltaX { get; set; }

        /// <summary>
        /// 当前预设 East/West 前后手 Z 分离量。
        /// </summary>
        public float SideDeltaZ { get; set; }

        /// <summary>
        /// 当前预设 East/West 前景手高度偏移。
        /// </summary>
        public float FrontAltitudeOffset { get; set; }

        /// <summary>
        /// 当前预设 East/West 背景手高度偏移。
        /// </summary>
        public float BackAltitudeOffset { get; set; }

        /// <summary>
        /// 当前预设 East/West 默认装饰角。
        /// </summary>
        public float EastWestDefaultAngle { get; set; }

        /// <summary>
        /// 当前预设 East/West 副手额外装饰角。
        /// </summary>
        public float EastWestSubHandAngleOffset { get; set; }

        /// <summary>
        /// 当前预设 East/West 是否允许手侧镜像。
        /// </summary>
        public bool EastWestHandMirror { get; set; }
    }
}
