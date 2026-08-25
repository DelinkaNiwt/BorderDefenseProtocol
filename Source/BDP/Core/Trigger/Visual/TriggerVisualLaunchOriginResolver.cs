using BDP.Core.Expressions;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using UnityEngine;
using Verse;

namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 发射根原点实际采用的来源类型。
    /// 它用于诊断“这一次理论中心到底是从哪条根链出来的”。
    /// </summary>
    internal enum TriggerVisualLaunchOriginSourceKind
    {
        /// <summary>
        /// 当前还没有确定任何来源类型。
        /// </summary>
        None = 0,

        /// <summary>
        /// 当前直接采用了协议阶段已冻结好的绝对根原点。
        /// </summary>
        FrozenPlanAbsolute = 1,

        /// <summary>
        /// 当前在真正开火边界实时解出了视觉枪口根原点。
        /// </summary>
        LiveVisualMuzzle = 2,

        /// <summary>
        /// 当前只能退回到宿主 Pawn.DrawPos 作为根原点。
        /// </summary>
        CasterDrawPosFallback = 3,

        /// <summary>
        /// 当前 Pawn 上没有可用 Trigger 宿主。
        /// </summary>
        MissingTriggerBody = 4,

        /// <summary>
        /// 当前没有可用的视觉投影或 resident 条目。
        /// </summary>
        MissingVisualProjection = 5,

        /// <summary>
        /// 当前没有可用的装备姿态样本。
        /// </summary>
        MissingPoseSample = 6,

        /// <summary>
        /// 当前姿态样本与已发布视觉投影版本不一致。
        /// </summary>
        PoseSampleProjectionMismatch = 7,

        /// <summary>
        /// 当前 source result 无法回溯到 resident 条目。
        /// </summary>
        MissingResidentEntry = 8,

        /// <summary>
        /// 当前 resident 条目无法解析到视觉预设。
        /// </summary>
        MissingVisualPreset = 9,

        /// <summary>
        /// 当前视觉姿态可解，但枪口锚点解算失败。
        /// </summary>
        ResolveMuzzleFailed = 10
    }

    /// <summary>
    /// 一次发射根原点解析结果。
    /// 它把真正采用的根坐标、来源类型和失败原因统一打包，便于宿主与诊断共用。
    /// </summary>
    internal sealed class TriggerVisualLaunchOriginResolution
    {
        /// <summary>
        /// 当前是否已经得到可用的根原点。
        /// </summary>
        public bool HasRootOrigin { get; set; }

        /// <summary>
        /// 当前最终采用的发射根原点世界坐标。
        /// </summary>
        public Vector3 RootOriginWorld { get; set; }

        /// <summary>
        /// 当前解析链路对应的 source result 标识。
        /// </summary>
        public string SourceResultId { get; set; }

        /// <summary>
        /// 当前真正采用的根原点来源。
        /// </summary>
        public TriggerVisualLaunchOriginSourceKind SourceKind { get; set; }

        /// <summary>
        /// 当未能使用视觉枪口根原点时，记录导致回退的具体失败原因。
        /// 成功使用冻结绝对原点或实时枪口时为 None。
        /// </summary>
        public TriggerVisualLaunchOriginSourceKind VisualFailureKind { get; set; }

        /// <summary>
        /// 本次解析链命中的视觉投影版本号。
        /// 仅用于诊断，不参与后续裁决。
        /// </summary>
        public int ProjectionVersion { get; set; }

        /// <summary>
        /// 本次解析链命中的姿态样本 tick。
        /// 仅用于诊断，不参与后续裁决。
        /// </summary>
        public int PoseSampleTick { get; set; }
    }

    /// <summary>
    /// Trigger 视觉发射根原点解析器。
    /// 它统一负责把“视觉枪口根点”从表现运行时状态里稳定解出来，避免协议层和宿主层各写一套。
    /// </summary>
    internal static class TriggerVisualLaunchOriginResolver
    {
        /// <summary>
        /// 解析真正开火时应采用的发射根原点。
        /// 规则是：优先用已冻结绝对根点，其次实时解视觉枪口，最后才退回 Pawn.DrawPos。
        /// </summary>
        public static TriggerVisualLaunchOriginResolution ResolveLaunchRoot(
            Pawn pawn,
            string sourceResultId,
            bool hasFrozenAbsoluteOriginWorld,
            Vector3 frozenAbsoluteOriginWorld,
            Vector3 casterDrawPos)
        {
            if (hasFrozenAbsoluteOriginWorld)
            {
                return new TriggerVisualLaunchOriginResolution
                {
                    HasRootOrigin = true,
                    RootOriginWorld = frozenAbsoluteOriginWorld,
                    SourceResultId = sourceResultId,
                    SourceKind = TriggerVisualLaunchOriginSourceKind.FrozenPlanAbsolute,
                    VisualFailureKind = TriggerVisualLaunchOriginSourceKind.None,
                    ProjectionVersion = 0,
                    PoseSampleTick = 0
                };
            }

            if (TryResolveVisualMuzzleRoot(pawn, sourceResultId, out TriggerVisualLaunchOriginResolution liveResolution))
            {
                return liveResolution;
            }

            return new TriggerVisualLaunchOriginResolution
            {
                HasRootOrigin = true,
                RootOriginWorld = casterDrawPos,
                SourceResultId = sourceResultId,
                SourceKind = TriggerVisualLaunchOriginSourceKind.CasterDrawPosFallback,
                VisualFailureKind = liveResolution != null
                    ? liveResolution.VisualFailureKind
                    : TriggerVisualLaunchOriginSourceKind.MissingTriggerBody,
                ProjectionVersion = liveResolution != null ? liveResolution.ProjectionVersion : 0,
                PoseSampleTick = liveResolution != null ? liveResolution.PoseSampleTick : 0
            };
        }

        /// <summary>
        /// 仅尝试从当前视觉运行时状态实时解出枪口根原点。
        /// 成功时返回 true，失败时把失败原因写入 resolution，供上层决定是否回退。
        /// </summary>
        public static bool TryResolveVisualMuzzleRoot(
            Pawn pawn,
            string sourceResultId,
            out TriggerVisualLaunchOriginResolution resolution)
        {
            resolution = CreateEmpty(sourceResultId);
            if (pawn == null)
            {
                resolution.VisualFailureKind = TriggerVisualLaunchOriginSourceKind.MissingTriggerBody;
                return false;
            }

            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            if (triggerBody == null)
            {
                resolution.VisualFailureKind = TriggerVisualLaunchOriginSourceKind.MissingTriggerBody;
                return false;
            }

            TriggerPresentationState presentation = triggerBody.PublishedPresentationProjection;
            VisualExpressionProjection visualProjection = presentation != null ? presentation.VisualProjection : null;
            if (visualProjection == null
                || visualProjection.ResidentEntries == null
                || visualProjection.ResidentEntries.Count == 0)
            {
                resolution.VisualFailureKind = TriggerVisualLaunchOriginSourceKind.MissingVisualProjection;
                resolution.ProjectionVersion = presentation != null ? presentation.ProjectionVersion : 0;
                return false;
            }

            TriggerVisualRuntimeState runtimeState = triggerBody.PublishedVisualRuntimeState;
            EquipmentPoseSample sample = runtimeState != null ? runtimeState.EquipmentPoseSample : null;
            resolution.ProjectionVersion = presentation != null ? presentation.ProjectionVersion : 0;
            resolution.PoseSampleTick = sample != null ? sample.SampleTick : 0;
            if (sample == null || !sample.IsValid)
            {
                resolution.VisualFailureKind = TriggerVisualLaunchOriginSourceKind.MissingPoseSample;
                return false;
            }

            if (!sample.IsValidForProjection(resolution.ProjectionVersion))
            {
                resolution.VisualFailureKind = TriggerVisualLaunchOriginSourceKind.PoseSampleProjectionMismatch;
                return false;
            }

            VisualResidentEntry residentEntry = FindResidentEntry(visualProjection, sourceResultId);
            if (residentEntry == null)
            {
                resolution.VisualFailureKind = TriggerVisualLaunchOriginSourceKind.MissingResidentEntry;
                return false;
            }

            ExpressionVisualPresetDef preset = ResolvePreset(visualProjection, residentEntry);
            if (preset == null)
            {
                resolution.VisualFailureKind = TriggerVisualLaunchOriginSourceKind.MissingVisualPreset;
                return false;
            }

            WeaponVisualStageSnapshot weaponStageSnapshot = new WeaponVisualStageResolver().Resolve(
                pawn,
                residentEntry,
                triggerBody.PublishedCombatProjection,
                runtimeState);
            VisualPoseResolver resolver = new VisualPoseResolver();
            VisualPoseRequest request = new VisualPoseRequest
            {
                Entry = residentEntry,
                Preset = preset,
                GraphicOverridePreset = ResolveGraphicOverridePreset(residentEntry),
                RuntimeState = runtimeState,
                WeaponStageSnapshot = weaponStageSnapshot,
                PoseSample = sample,
                SourceThing = ResolveSourceThing(triggerBody, residentEntry),
                EquippedAngleOffset = ResolveEquippedAngleOffset(pawn),
                IsExecutionActive = ResolveExecutionActive(visualProjection, runtimeState, residentEntry),
                IsMuzzleActive = ResolveMuzzleActive(visualProjection, runtimeState, residentEntry)
            };
            bool resolved = visualProjection.HostEquipmentRenderMode == HostEquipmentRenderMode.ReplaceTextureOnly
                ? resolver.TryResolveTextureOnlyMuzzleAnchor(request, out ResolvedMuzzleAnchor anchor)
                : resolver.TryResolveMuzzleAnchor(request, out anchor);
            if (!resolved)
            {
                resolution.VisualFailureKind = TriggerVisualLaunchOriginSourceKind.ResolveMuzzleFailed;
                return false;
            }

            resolution.HasRootOrigin = true;
            resolution.RootOriginWorld = anchor.WorldPosition;
            resolution.SourceKind = TriggerVisualLaunchOriginSourceKind.LiveVisualMuzzle;
            resolution.VisualFailureKind = TriggerVisualLaunchOriginSourceKind.None;
            return true;
        }

        /// <summary>
        /// 构造一份空的解析结果。
        /// </summary>
        private static TriggerVisualLaunchOriginResolution CreateEmpty(string sourceResultId)
        {
            return new TriggerVisualLaunchOriginResolution
            {
                HasRootOrigin = false,
                RootOriginWorld = Vector3.zero,
                SourceResultId = sourceResultId,
                SourceKind = TriggerVisualLaunchOriginSourceKind.None,
                VisualFailureKind = TriggerVisualLaunchOriginSourceKind.None,
                ProjectionVersion = 0,
                PoseSampleTick = 0
            };
        }

        /// <summary>
        /// 在已发布视觉 resident 条目里按正式结果标识查找来源条目。
        /// </summary>
        private static VisualResidentEntry FindResidentEntry(
            VisualExpressionProjection visualProjection,
            string sourceResultId)
        {
            if (visualProjection?.ResidentEntries == null || string.IsNullOrWhiteSpace(sourceResultId))
            {
                return null;
            }

            for (int i = 0; i < visualProjection.ResidentEntries.Count; i++)
            {
                VisualResidentEntry entry = visualProjection.ResidentEntries[i];
                if (entry != null && string.Equals(entry.ResultId, sourceResultId, System.StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// 解析当前 resident 条目在当前视觉关系下应使用的预设。
        /// </summary>
        private static ExpressionVisualPresetDef ResolvePreset(
            VisualExpressionProjection visualProjection,
            VisualResidentEntry entry)
        {
            if (entry == null)
            {
                return null;
            }

            string presetDefName = visualProjection != null
                && visualProjection.RelationKind != VisualExpressionRelationKind.SingleSide
                && !string.IsNullOrWhiteSpace(entry.CompositeVisualPresetDefName)
                    ? entry.CompositeVisualPresetDefName
                    : entry.VisualPresetDefName;
            return string.IsNullOrWhiteSpace(presetDefName)
                ? null
                : DefDatabase<ExpressionVisualPresetDef>.GetNamed(presetDefName, false);
        }

        /// <summary>
        /// 解析当前 resident 条目的视觉图层局部覆盖预设。
        /// </summary>
        private static ExpressionVisualPresetDef ResolveGraphicOverridePreset(
            VisualResidentEntry entry)
        {
            return entry == null || string.IsNullOrWhiteSpace(entry.VisualGraphicOverrideDefName)
                ? null
                : DefDatabase<ExpressionVisualPresetDef>.GetNamed(
                    entry.VisualGraphicOverrideDefName,
                    false);
        }

        /// <summary>
        /// 根据 resident 条目的槽位回到芯片实例。
        /// </summary>
        private static Thing ResolveSourceThing(CompTriggerBody triggerBody, VisualResidentEntry entry)
        {
            if (triggerBody == null || entry == null)
            {
                return null;
            }

            foreach (ITriggerSlotState slot in triggerBody.GetAllSlots())
            {
                if (slot == null || slot.LoadedChip == null)
                {
                    continue;
                }

                if (slot.Side == entry.Side && slot.Index == entry.SlotIndex)
                {
                    return slot.LoadedChip;
                }
            }

            return null;
        }

        /// <summary>
        /// 判断当前 resident 条目是否命中执行焦点。
        /// </summary>
        private static bool ResolveExecutionActive(
            VisualExpressionProjection visualProjection,
            TriggerVisualRuntimeState runtimeState,
            VisualResidentEntry entry)
        {
            if (visualProjection == null || runtimeState == null || entry == null)
            {
                return false;
            }

            switch (visualProjection.ExecutionFocusPolicy)
            {
                case VisualExecutionFocusPolicy.HostResult:
                    return string.Equals(runtimeState.ActiveHostResultId, entry.ResultId, System.StringComparison.Ordinal);
                case VisualExecutionFocusPolicy.CastResult:
                    return runtimeState.ContainsActiveCastResult(entry.ResultId);
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判断当前 resident 条目是否命中枪口焦点。
        /// </summary>
        private static bool ResolveMuzzleActive(
            VisualExpressionProjection visualProjection,
            TriggerVisualRuntimeState runtimeState,
            VisualResidentEntry entry)
        {
            if (visualProjection == null || runtimeState == null || entry == null)
            {
                return false;
            }

            switch (visualProjection.MuzzleFollowPolicy)
            {
                case VisualMuzzleFollowPolicy.HostResult:
                    return string.Equals(runtimeState.ActiveHostResultId, entry.ResultId, System.StringComparison.Ordinal);
                case VisualMuzzleFollowPolicy.CastResult:
                    return runtimeState.ContainsActiveCastResult(entry.ResultId);
                case VisualMuzzleFollowPolicy.EmitSourceResult:
                    return runtimeState.ContainsActiveEmitSourceResult(entry.ResultId);
                default:
                    return false;
            }
        }

        /// <summary>
        /// 读取宿主主装备上的 equippedAngleOffset。
        /// </summary>
        private static float ResolveEquippedAngleOffset(Pawn pawn)
        {
            return pawn?.equipment?.Primary?.def != null
                ? pawn.equipment.Primary.def.equippedAngleOffset
                : 0f;
        }
    }
}
