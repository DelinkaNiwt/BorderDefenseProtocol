using BDP.Core.Expressions;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using BDP.Core.Trigger.Visual;
using BDP.Core.VerbHosting;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// PawnRenderUtility.DrawEquipmentAiming 的 BDP 视觉桥接补丁。
    /// 它在原版装备绘制边界采样姿态，并按已发布视觉投影替换或附加绘制芯片武器贴图。
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
    public static class Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual
    {
        /// <summary>
        /// 当前补丁复用的视觉姿态解析器。
        /// </summary>
        private static readonly VisualPoseResolver PoseResolver = new VisualPoseResolver();

        /// <summary>
        /// 当前补丁复用的武器动作阶段解析器。
        /// </summary>
        private static readonly WeaponVisualStageResolver WeaponStageResolver =
            new WeaponVisualStageResolver();

        /// <summary>
        /// 在原版装备绘制前采样姿态并尝试绘制 BDP 视觉条目。
        /// 返回 false 时跳过原版装备贴图。
        /// </summary>
        public static bool Prefix(Thing eq, Vector3 drawLoc, float aimAngle)
        {
            if (eq == null)
            {
                return true;
            }

            CompTriggerBody triggerBody = eq.TryGetComp<CompTriggerBody>();
            if (triggerBody == null)
            {
                return true;
            }

            Pawn pawn = ResolveOwnerPawn(eq, triggerBody);
            Rot4 facing = pawn != null ? pawn.Rotation : Rot4.South;
            int projectionVersion = ResolveProjectionVersion(triggerBody);
            EquipmentPoseSample sample = EquipmentPoseSample.Create(
                projectionVersion,
                drawLoc,
                aimAngle,
                facing,
                Find.TickManager != null ? Find.TickManager.TicksGame : 0);
            triggerBody.RuntimeServices?.TriggerVisualRuntimeStateOwner?.PublishPoseSample(sample);

            VisualExpressionProjection visualProjection =
                triggerBody.PublishedPresentationProjection != null
                    ? triggerBody.PublishedPresentationProjection.VisualProjection
                    : null;
            if (visualProjection == null
                || visualProjection.ResidentEntries == null
                || visualProjection.ResidentEntries.Count == 0)
            {
                return visualProjection == null
                    || visualProjection.HostEquipmentRenderMode != HostEquipmentRenderMode.Suppress;
            }

            TriggerVisualRuntimeState runtimeState = triggerBody.PublishedVisualRuntimeState;
            switch (visualProjection.HostEquipmentRenderMode)
            {
                case HostEquipmentRenderMode.Keep:
                    DrawResidentEntries(eq, triggerBody, visualProjection, runtimeState, sample);
                    return true;
                case HostEquipmentRenderMode.Suppress:
                    DrawResidentEntries(eq, triggerBody, visualProjection, runtimeState, sample);
                    return false;
                case HostEquipmentRenderMode.ReplaceTextureOnly:
                    return !TryHandleSingleWeaponTextureReplacement(
                        pawn,
                        eq,
                        triggerBody,
                        visualProjection,
                        sample);
                case HostEquipmentRenderMode.Replace:
                default:
                    bool handledAnyEntry = DrawResidentEntries(
                        eq,
                        triggerBody,
                        visualProjection,
                        runtimeState,
                        sample);
                    return !handledAnyEntry;
            }
        }

        /// <summary>
        /// 绘制当前视觉投影中的全部常驻条目。
        /// </summary>
        private static bool DrawResidentEntries(
            Thing equipment,
            CompTriggerBody triggerBody,
            VisualExpressionProjection visualProjection,
            TriggerVisualRuntimeState runtimeState,
            EquipmentPoseSample sample)
        {
            bool handledAnyEntry = false;
            for (int i = 0; i < visualProjection.ResidentEntries.Count; i++)
            {
                VisualResidentEntry entry = visualProjection.ResidentEntries[i];
                ExpressionVisualPresetDef preset = ResolvePreset(visualProjection, entry, runtimeState);
                if (entry == null || preset == null)
                {
                    continue;
                }

                Thing sourceThing = ResolveSourceThing(triggerBody, entry);
                WeaponVisualStageSnapshot weaponStageSnapshot = WeaponStageResolver.Resolve(
                    triggerBody.OwnerPawn,
                    entry,
                    triggerBody.PublishedCombatProjection,
                    runtimeState);
                ResolvedVisualPose pose = PoseResolver.Resolve(new VisualPoseRequest
                {
                    Entry = entry,
                    Preset = preset,
                    GraphicOverridePreset = ResolveGraphicOverridePreset(entry),
                    RuntimeState = runtimeState,
                    WeaponStageSnapshot = weaponStageSnapshot,
                    PoseSample = sample,
                    SourceThing = sourceThing,
                    EquippedAngleOffset = equipment != null && equipment.def != null
                        ? equipment.def.equippedAngleOffset
                        : 0f,
                    IsExecutionActive = ResolveExecutionActive(visualProjection, runtimeState, entry),
                    IsMuzzleActive = ResolveMuzzleActive(visualProjection, runtimeState, entry)
                });
                if (pose == null || !pose.IsValid)
                {
                    continue;
                }

                handledAnyEntry = true;
                if (!preset.ResolveStageVisibility(weaponStageSnapshot.Stage))
                {
                    continue;
                }

                ApplyVanillaRecoil(equipment, triggerBody, entry, sample, pose);
                ApplyExpressionVisualImpulse(triggerBody, entry, pose);
                DrawPose(pose);
            }

            return handledAnyEntry;
        }

        /// <summary>
        /// 按当前视觉条目的来源正式 Verb 应用原版装备后坐力。
        /// 这里只改变主贴图与附加层绘制姿态，不改变枪口锚点和发射原点。
        /// </summary>
        private static void ApplyVanillaRecoil(
            Thing equipment,
            CompTriggerBody triggerBody,
            VisualResidentEntry entry,
            EquipmentPoseSample sample,
            ResolvedVisualPose pose)
        {
            if (equipment?.def == null
                || triggerBody?.VerbHostManager == null
                || entry == null
                || sample == null
                || pose == null
                || !triggerBody.VerbHostManager.TryGetByResultId(
                    entry.ResultId,
                    out BdpFormalVerbBinding binding)
                || binding?.RangedVerb == null)
            {
                return;
            }

            EquipmentUtility.Recoil(
                equipment.def,
                binding.RangedVerb,
                out Vector3 drawOffset,
                out float angleOffset,
                sample.AimAngle);

            pose.DrawPosition += drawOffset;
            pose.DrawAngle += angleOffset;
            if (pose.OverlayPoses == null)
            {
                return;
            }

            for (int i = 0; i < pose.OverlayPoses.Count; i++)
            {
                ResolvedVisualOverlayPose overlay = pose.OverlayPoses[i];
                if (overlay == null || !overlay.IsValid)
                {
                    continue;
                }

                overlay.DrawPosition += drawOffset;
                overlay.DrawAngle += angleOffset;
            }
        }

        /// <summary>
        /// 单枚激活武器芯片沿用原版手持姿态，只替换视觉图层。
        /// 这里不进入 VisualPoseResolver，避免触发双武器偏移和枪口处理。
        /// </summary>
        private static bool TryHandleSingleWeaponTextureReplacement(
            Pawn pawn,
            Thing equipment,
            CompTriggerBody triggerBody,
            VisualExpressionProjection visualProjection,
            EquipmentPoseSample sample)
        {
            if (equipment == null || visualProjection == null || sample == null || !sample.IsValid)
            {
                return false;
            }

            VisualResidentEntry entry = SelectTextureOnlyEntry(visualProjection);
            ExpressionVisualPresetDef preset = ResolveTextureOnlyPreset(entry);
            if (entry == null || preset == null)
            {
                return false;
            }

            Thing sourceThing = ResolveSourceThing(triggerBody, entry);
            TriggerVisualRuntimeState runtimeState = triggerBody.PublishedVisualRuntimeState;
            WeaponVisualStageSnapshot weaponStageSnapshot = WeaponStageResolver.Resolve(
                pawn,
                entry,
                triggerBody.PublishedCombatProjection,
                runtimeState);
            ResolvedVisualPose pose = PoseResolver.ResolveTextureOnly(new VisualPoseRequest
            {
                Entry = entry,
                Preset = preset,
                RuntimeState = runtimeState,
                WeaponStageSnapshot = weaponStageSnapshot,
                PoseSample = sample,
                SourceThing = sourceThing,
                EquippedAngleOffset = equipment.def != null ? equipment.def.equippedAngleOffset : 0f,
                IsExecutionActive = false,
                IsMuzzleActive = false
            });
            if (pose == null || !pose.IsValid)
            {
                return false;
            }

            if (!preset.ResolveStageVisibility(weaponStageSnapshot.Stage))
            {
                return true;
            }

            DrawTextureOnlyReplacement(
                equipment,
                triggerBody,
                entry,
                sourceThing,
                sample,
                pose);
            return true;
        }

        /// <summary>
        /// 从单武器视觉条目中选择实际用于替换的贴图条目。
        /// 同一芯片有主副攻击时优先主攻击，缺失主攻击时回退到第一条有贴图的条目。
        /// </summary>
        private static VisualResidentEntry SelectTextureOnlyEntry(VisualExpressionProjection visualProjection)
        {
            if (visualProjection?.ResidentEntries == null)
            {
                return null;
            }

            VisualResidentEntry fallback = null;
            for (int i = 0; i < visualProjection.ResidentEntries.Count; i++)
            {
                VisualResidentEntry entry = visualProjection.ResidentEntries[i];
                if (entry == null || !entry.HasVisualPreset)
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = entry;
                }

                if (entry.VerbAttackRole == VerbAttackRole.Primary)
                {
                    return entry;
                }
            }

            return fallback;
        }

        /// <summary>
        /// 解析单武器贴图替换使用的视觉预设。
        /// 单武器不读取 CompositeVisualPresetDefName，避免误入双武器表象。
        /// </summary>
        private static ExpressionVisualPresetDef ResolveTextureOnlyPreset(VisualResidentEntry entry)
        {
            return entry == null || string.IsNullOrWhiteSpace(entry.VisualPresetDefName)
                ? null
                : DefDatabase<ExpressionVisualPresetDef>.GetNamed(entry.VisualPresetDefName, false);
        }

        /// <summary>
        /// 按原版 DrawEquipmentAiming 姿态绘制替换贴图。
        /// 除材质和贴图尺寸来自芯片视觉预设外，角度、镜像和后坐力都沿用原版规则。
        /// </summary>
        private static void DrawTextureOnlyReplacement(
            Thing equipment,
            CompTriggerBody triggerBody,
            VisualResidentEntry entry,
            Thing sourceThing,
            EquipmentPoseSample sample,
            ResolvedVisualPose pose)
        {
            ApplyVanillaRecoil(equipment, triggerBody, entry, sample, pose);
            ApplyExpressionVisualImpulse(triggerBody, entry, pose);
            DrawTextureOnlyGraphic(
                sourceThing,
                pose.Graphic,
                ResolveTextureOnlyMesh(pose.MeshKind),
                pose.DrawPosition,
                pose.DrawAngle,
                pose.DrawScale);
            DrawTextureOnlyOverlayPoses(sourceThing, pose.OverlayPoses);
        }

        /// <summary>
        /// 把当前表达结果的短暂视觉冲量叠加到主贴图及其附加层。
        /// 枪口、握持锚点和 Pawn 本体不读取该位移。
        /// </summary>
        private static void ApplyExpressionVisualImpulse(
            CompTriggerBody triggerBody,
            VisualResidentEntry entry,
            ResolvedVisualPose pose)
        {
            if (triggerBody?.RuntimeServices?.TriggerVisualRuntimeStateOwner == null
                || entry == null
                || pose == null)
            {
                return;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            Vector3 offset = triggerBody.RuntimeServices.TriggerVisualRuntimeStateOwner
                .ResolveExpressionVisualImpulseOffset(entry.ResultId, currentTick);
            if (offset == Vector3.zero)
            {
                return;
            }

            pose.DrawPosition += offset;
            if (pose.OverlayPoses == null)
            {
                return;
            }

            for (int index = 0; index < pose.OverlayPoses.Count; index++)
            {
                ResolvedVisualOverlayPose overlay = pose.OverlayPoses[index];
                if (overlay != null && overlay.IsValid)
                {
                    overlay.DrawPosition += offset;
                }
            }
        }

        /// <summary>
        /// 使用单武器已解析的原版姿态绘制全部附加层。
        /// </summary>
        private static void DrawTextureOnlyOverlayPoses(
            Thing sourceThing,
            System.Collections.Generic.IReadOnlyList<ResolvedVisualOverlayPose> overlayPoses)
        {
            if (overlayPoses == null)
            {
                return;
            }

            for (int i = 0; i < overlayPoses.Count; i++)
            {
                ResolvedVisualOverlayPose overlay = overlayPoses[i];
                if (overlay == null || !overlay.IsValid)
                {
                    continue;
                }

                DrawTextureOnlyGraphic(
                    sourceThing,
                    overlay.Graphic,
                    ResolveTextureOnlyMesh(overlay.MeshKind),
                    overlay.DrawPosition,
                    overlay.DrawAngle,
                    overlay.DrawScale);
            }
        }

        /// <summary>
        /// 把已解析网格类型转换为 RimWorld 原版装备绘制网格。
        /// </summary>
        private static Mesh ResolveTextureOnlyMesh(VisualMeshKind meshKind)
        {
            return meshKind == VisualMeshKind.PlaneFlipped ? MeshPool.plane10Flip : MeshPool.plane10;
        }

        /// <summary>
        /// 使用指定原版姿态绘制单张替换贴图。
        /// </summary>
        private static void DrawTextureOnlyGraphic(
            Thing sourceThing,
            Graphic graphic,
            Mesh mesh,
            Vector3 drawPosition,
            float drawAngle,
            float drawScale)
        {
            Material material = ResolveTextureOnlyMaterial(graphic, sourceThing);
            float safeScale = drawScale > 0f ? drawScale : 1f;
            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPosition,
                Quaternion.AngleAxis(drawAngle, Vector3.up),
                new Vector3(
                    graphic.drawSize.x * safeScale,
                    0f,
                    graphic.drawSize.y * safeScale));
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }

        /// <summary>
        /// 按原版材质选择方式解析替换贴图材质。
        /// </summary>
        private static Material ResolveTextureOnlyMaterial(Graphic graphic, Thing sourceThing)
        {
            if (graphic == null)
            {
                return null;
            }

            Graphic_StackCount stackGraphic = graphic as Graphic_StackCount;
            if (stackGraphic != null && sourceThing != null && sourceThing.def != null)
            {
                return stackGraphic.SubGraphicForStackCount(1, sourceThing.def).MatSingleFor(sourceThing);
            }

            return sourceThing != null ? graphic.MatSingleFor(sourceThing) : graphic.MatSingle;
        }

        /// <summary>
        /// 按当前视觉关系解析条目应使用的视觉预设。
        /// </summary>
        private static ExpressionVisualPresetDef ResolvePreset(
            VisualExpressionProjection visualProjection,
            VisualResidentEntry entry,
            TriggerVisualRuntimeState runtimeState)
        {
            if (entry == null)
            {
                return null;
            }

            string presetDefName = ResolvePresetDefName(visualProjection, entry, runtimeState);
            return string.IsNullOrWhiteSpace(presetDefName)
                ? null
                : DefDatabase<ExpressionVisualPresetDef>.GetNamed(presetDefName, false);
        }

        /// <summary>
        /// 解析当前条目应使用的视觉预设 DefName。
        /// 复合关系成立时优先使用 CompositeVisualPresetDefName。
        /// </summary>
        private static string ResolvePresetDefName(
            VisualExpressionProjection visualProjection,
            VisualResidentEntry entry,
            TriggerVisualRuntimeState runtimeState)
        {
            if (visualProjection != null
                && visualProjection.RelationKind != VisualExpressionRelationKind.SingleSide
                && !string.IsNullOrWhiteSpace(entry.CompositeVisualPresetDefName))
            {
                return entry.CompositeVisualPresetDefName;
            }

            return entry.VisualPresetDefName;
        }

        /// <summary>
        /// 解析当前条目的视觉图层局部覆盖预设。
        /// 它只提供 GraphicData/OverlayLayers，不参与姿态、握持或枪口锚点解析。
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
        /// 判断当前条目是否命中执行焦点。
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
        /// 判断当前条目是否命中枪口 emit 源焦点。
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
        /// 绘制主姿态和附加层姿态。
        /// </summary>
        private static void DrawPose(ResolvedVisualPose pose)
        {
            DrawGraphicPose(
                pose.Graphic,
                pose.DrawMaterial,
                pose.DrawPosition,
                pose.DrawAngle,
                pose.MeshKind,
                pose.DrawScale);
            if (pose.OverlayPoses == null)
            {
                return;
            }

            for (int i = 0; i < pose.OverlayPoses.Count; i++)
            {
                ResolvedVisualOverlayPose overlay = pose.OverlayPoses[i];
                if (overlay == null || !overlay.IsValid)
                {
                    continue;
                }

                DrawGraphicPose(
                    overlay.Graphic,
                    overlay.DrawMaterial,
                    overlay.DrawPosition,
                    overlay.DrawAngle,
                    overlay.MeshKind,
                    overlay.DrawScale);
            }
        }

        /// <summary>
        /// 绘制单张贴图姿态。
        /// </summary>
        private static void DrawGraphicPose(
            Graphic graphic,
            Material material,
            Vector3 drawPosition,
            float drawAngle,
            VisualMeshKind meshKind,
            float drawScale)
        {
            if (graphic == null || material == null)
            {
                return;
            }

            Mesh mesh = meshKind == VisualMeshKind.PlaneFlipped ? MeshPool.plane10Flip : MeshPool.plane10;
            Vector2 drawSize = graphic.drawSize;
            float safeScale = drawScale > 0f ? drawScale : 1f;
            Graphics.DrawMesh(
                mesh,
                Matrix4x4.TRS(
                    drawPosition,
                    Quaternion.AngleAxis(drawAngle, Vector3.up),
                    new Vector3(drawSize.x * safeScale, 0f, drawSize.y * safeScale)),
                material,
                0);
        }

        /// <summary>
        /// 解析当前 Trigger owner 的已发布投影版本号。
        /// </summary>
        private static int ResolveProjectionVersion(CompTriggerBody triggerBody)
        {
            if (triggerBody == null)
            {
                return 0;
            }

            if (triggerBody.PublishedPresentationProjection != null)
            {
                return triggerBody.PublishedPresentationProjection.ProjectionVersion;
            }

            return triggerBody.PublishedCombatProjection != null
                ? triggerBody.PublishedCombatProjection.ProjectionVersion
                : 0;
        }

        /// <summary>
        /// 解析当前装备所属 Pawn。
        /// </summary>
        private static Pawn ResolveOwnerPawn(Thing equipment, CompTriggerBody triggerBody)
        {
            if (triggerBody != null && triggerBody.OwnerPawn != null)
            {
                return triggerBody.OwnerPawn;
            }

            return equipment?.TryGetComp<CompEquippable>()?.PrimaryVerb?.CasterPawn;
        }

        /// <summary>
        /// 根据视觉条目的来源追踪回到芯片实例。
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
    }
}
