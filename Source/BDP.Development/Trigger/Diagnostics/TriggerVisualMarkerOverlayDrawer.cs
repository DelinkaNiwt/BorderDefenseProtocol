using System.Collections.Generic;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Visual.Diagnostics;
using UnityEngine;
using Verse;

namespace BDP.Development.Trigger.Diagnostics
{
    /// <summary>
    /// Trigger 视觉关键点位地图 overlay 绘制器。
    /// 它把关键点位直接画到地图上，便于对照实际贴图表现。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class TriggerVisualMarkerOverlayDrawer
    {
        /// <summary>
        /// 所有诊断圆点统一使用的尺寸缩放。
        /// 这里把既有点径整体缩到 70%，便于更精细地观察重合和轻微偏差。
        /// </summary>
        private const float PointDiameterScale = 0.7f;

        /// <summary>
        /// Marker 使用的统一高度层。
        /// </summary>
        private static readonly float OverlayAltitude = AltitudeLayer.MetaOverlays.AltitudeFor();

        /// <summary>
        /// 圆点 marker 统一使用的平面网格。
        /// 它铺在地面 overlay 层上，用贴图 alpha 裁切出真正的圆点形状。
        /// </summary>
        private static readonly Mesh MarkerMesh = MeshPool.plane10;

        /// <summary>
        /// 圆点 marker 共享的运行时圆形贴图。
        /// 它只在内容程序集内生成一次，不依赖额外资源文件。
        /// </summary>
        private static readonly Texture2D MarkerTexture = CreateMarkerTexture();

        /// <summary>
        /// 小人 DrawPos marker 材质。
        /// </summary>
        private static readonly Material PawnMaterial = CreatePointMaterial(new Color(1f, 0.92f, 0.2f, 0.5f));

        /// <summary>
        /// DrawEquipmentAiming drawLoc marker 材质。
        /// </summary>
        private static readonly Material DrawLocMaterial = CreatePointMaterial(new Color(0.2f, 0.95f, 1f, 0.5f));

        /// <summary>
        /// 最终武器绘制位置 marker 材质。
        /// </summary>
        private static readonly Material WeaponMaterial = CreatePointMaterial(new Color(0.25f, 1f, 0.35f, 0.5f));

        /// <summary>
        /// 主手枪口 marker 材质。
        /// 与旧版右手红色枪口标记保持同类感知。
        /// </summary>
        private static readonly Material MainMuzzleMaterial = CreatePointMaterial(new Color(1f, 0.28f, 0.28f, 0.6f));

        /// <summary>
        /// 副手枪口 marker 材质。
        /// 与旧版左手蓝色枪口标记保持同类感知。
        /// </summary>
        private static readonly Material SubMuzzleMaterial = CreatePointMaterial(new Color(0.28f, 0.5f, 1f, 0.6f));

        /// <summary>
        /// 主手理论中心原点 marker 材质。
        /// 它与主手红色枪口点区分开，用于表达“散布前中心点”。
        /// </summary>
        private static readonly Material MainCenterMaterial = CreatePointMaterial(new Color(1f, 0.35f, 0.75f, 0.44f));

        /// <summary>
        /// 副手理论中心原点 marker 材质。
        /// 它与副手蓝色枪口点区分开，用于表达“散布前中心点”。
        /// </summary>
        private static readonly Material SubCenterMaterial = CreatePointMaterial(new Color(0.2f, 1f, 0.72f, 0.44f));

        /// <summary>
        /// 主手全部真实发射点 marker 材质。
        /// 同一批次主手发射出去的每颗子弹都使用这一个颜色。
        /// </summary>
        private static readonly Material MainLaunchMaterial = CreatePointMaterial(new Color(1f, 0.6f, 0.15f, 0.72f));

        /// <summary>
        /// 副手全部真实发射点 marker 材质。
        /// 同一批次副手发射出去的每颗子弹都使用这一个颜色。
        /// </summary>
        private static readonly Material SubLaunchMaterial = CreatePointMaterial(new Color(0.74f, 0.45f, 1f, 0.72f));

        /// <summary>
        /// 绘制小人中心点到 DrawLoc 的连线。
        /// </summary>
        private static readonly Material PawnToDrawLocMaterial =
            SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.85f, 0.95f, 0.35f, 0.82f));

        /// <summary>
        /// 绘制 DrawLoc 到最终武器位置的连线。
        /// </summary>
        private static readonly Material DrawLocToWeaponMaterial =
            SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.95f, 0.9f, 0.82f));

        /// <summary>
        /// 绘制最终武器位置到枪口的连线。
        /// </summary>
        private static readonly Material WeaponToMuzzleMaterial =
            SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.55f, 1f, 0.55f, 0.82f));

        /// <summary>
        /// 绘制枪口到理论中心原点的连线。
        /// 若配置正确且无额外源点偏移，这条线应退化为零长度。
        /// </summary>
        private static readonly Material MuzzleToCenterMaterial =
            SolidColorMaterials.SimpleSolidColorMaterial(new Color(1f, 1f, 1f, 0.7f));

        /// <summary>
        /// 绘制理论中心原点到真实发射点的连线。
        /// 它直观表达同批次齐射里每发 projectile 的源点散布。
        /// </summary>
        private static readonly Material CenterToLaunchMaterial =
            SolidColorMaterials.SimpleSolidColorMaterial(new Color(1f, 0.85f, 0.35f, 0.46f));

        /// <summary>
        /// 绘制单个 Pawn 的 Trigger 视觉 marker 集合。
        /// </summary>
        public static void DrawForPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            TriggerVisualPoseDiagnosticsSnapshot snapshot =
                TriggerVisualPoseDiagnosticsAccess.CaptureSnapshot(pawn);
            if (snapshot == null || !snapshot.IsAvailable)
            {
                return;
            }

            DrawPoint(snapshot.PawnDrawPosition, PawnMaterial, 0.22f);
            DrawPoint(snapshot.DrawLoc, DrawLocMaterial, 0.18f);
            DrawLink(snapshot.PawnDrawPosition, snapshot.DrawLoc, PawnToDrawLocMaterial, 0.035f);

            if (snapshot.Residents == null)
            {
                return;
            }

            Dictionary<string, TriggerVisualResidentPoseDiagnosticsSnapshot> residentIndex =
                BuildResidentIndex(snapshot);
            Dictionary<TriggerSide, Vector3> centerBySide = new Dictionary<TriggerSide, Vector3>();
            Dictionary<TriggerSide, TriggerVisualResidentPoseDiagnosticsSnapshot> anchorResidentBySide =
                new Dictionary<TriggerSide, TriggerVisualResidentPoseDiagnosticsSnapshot>();
            List<ResolvedLaunchMarker> launchMarkers = new List<ResolvedLaunchMarker>();

            for (int i = 0; i < snapshot.Residents.Count; i++)
            {
                TriggerVisualResidentPoseDiagnosticsSnapshot resident = snapshot.Residents[i];
                if (resident == null || !resident.HasResolvedPose)
                {
                    continue;
                }

                Material muzzleMaterial = ResolveMuzzleMaterial(resident.Side);
                DrawPoint(resident.ResolvedDrawPosition, WeaponMaterial, 0.16f);
                DrawLink(snapshot.DrawLoc, resident.ResolvedDrawPosition, DrawLocToWeaponMaterial, 0.03f);

                if (resident.HasMuzzleAnchor)
                {
                    DrawPoint(resident.MuzzleWorldPosition, muzzleMaterial, 0.19f);
                    DrawLink(resident.ResolvedDrawPosition, resident.MuzzleWorldPosition, WeaponToMuzzleMaterial, 0.03f);
                }
            }

            if (snapshot.RecentLaunchPoints == null || snapshot.RecentLaunchPoints.Count == 0)
            {
                return;
            }

            for (int i = 0; i < snapshot.RecentLaunchPoints.Count; i++)
            {
                TriggerVisualEmissionLaunchPointSnapshot point = snapshot.RecentLaunchPoints[i];
                if (point == null)
                {
                    continue;
                }

                TriggerVisualResidentPoseDiagnosticsSnapshot resident =
                    ResolveResidentForLaunchPoint(point, residentIndex);
                TriggerSide side = resident != null ? resident.Side : TriggerSide.Main;
                centerBySide[side] = point.TheoreticalCenterOriginWorld;
                if (!anchorResidentBySide.ContainsKey(side) && resident != null)
                {
                    anchorResidentBySide[side] = resident;
                }

                launchMarkers.Add(new ResolvedLaunchMarker
                {
                    Side = side,
                    TheoreticalCenterOriginWorld = point.TheoreticalCenterOriginWorld,
                    ActualLaunchOriginWorld = point.ActualLaunchOriginWorld
                });
            }

            DrawCenterPointForSide(TriggerSide.Main, centerBySide, anchorResidentBySide);
            DrawCenterPointForSide(TriggerSide.Sub, centerBySide, anchorResidentBySide);
            DrawLaunchMarkers(launchMarkers);
        }

        /// <summary>
        /// 构建按 ResultId 索引的 resident 字典。
        /// 发射点记录只携带 ResultId，overlay 需要靠它回溯主副侧归属与枪口点。
        /// </summary>
        private static Dictionary<string, TriggerVisualResidentPoseDiagnosticsSnapshot> BuildResidentIndex(
            TriggerVisualPoseDiagnosticsSnapshot snapshot)
        {
            Dictionary<string, TriggerVisualResidentPoseDiagnosticsSnapshot> result =
                new Dictionary<string, TriggerVisualResidentPoseDiagnosticsSnapshot>();
            if (snapshot == null || snapshot.Residents == null)
            {
                return result;
            }

            for (int i = 0; i < snapshot.Residents.Count; i++)
            {
                TriggerVisualResidentPoseDiagnosticsSnapshot resident = snapshot.Residents[i];
                if (resident == null || string.IsNullOrWhiteSpace(resident.ResultId))
                {
                    continue;
                }

                result[resident.ResultId] = resident;
            }

            return result;
        }

        /// <summary>
        /// 按发射点记录回溯所属 resident。
        /// 当前优先使用 ResultId 精确匹配，不走模糊推断。
        /// </summary>
        private static TriggerVisualResidentPoseDiagnosticsSnapshot ResolveResidentForLaunchPoint(
            TriggerVisualEmissionLaunchPointSnapshot point,
            Dictionary<string, TriggerVisualResidentPoseDiagnosticsSnapshot> residentIndex)
        {
            if (point == null
                || residentIndex == null
                || string.IsNullOrWhiteSpace(point.ResultId))
            {
                return null;
            }

            residentIndex.TryGetValue(point.ResultId, out TriggerVisualResidentPoseDiagnosticsSnapshot resident);
            return resident;
        }

        /// <summary>
        /// 为指定主副侧绘制理论中心原点，并把它与枪口点连起来。
        /// 这样可直观看到“理论中心是否和枪口重合”。
        /// </summary>
        private static void DrawCenterPointForSide(
            TriggerSide side,
            Dictionary<TriggerSide, Vector3> centerBySide,
            Dictionary<TriggerSide, TriggerVisualResidentPoseDiagnosticsSnapshot> anchorResidentBySide)
        {
            if (centerBySide == null || !centerBySide.TryGetValue(side, out Vector3 center))
            {
                return;
            }

            Material centerMaterial = ResolveCenterMaterial(side);
            DrawPoint(center, centerMaterial, 0.24f);

            if (anchorResidentBySide != null
                && anchorResidentBySide.TryGetValue(side, out TriggerVisualResidentPoseDiagnosticsSnapshot resident)
                && resident != null
                && resident.HasMuzzleAnchor)
            {
                DrawLink(resident.MuzzleWorldPosition, center, MuzzleToCenterMaterial, 0.025f);
            }
        }

        /// <summary>
        /// 根据侧别选择枪口 marker 材质。
        /// 当前约定主手用红色，副手用蓝色，以贴近旧版调试习惯。
        /// </summary>
        private static Material ResolveMuzzleMaterial(TriggerSide side)
        {
            return side == TriggerSide.Sub ? SubMuzzleMaterial : MainMuzzleMaterial;
        }

        /// <summary>
        /// 根据侧别选择理论中心原点 marker 材质。
        /// 主副侧中心点要与枪口点和真实发射点都区分开。
        /// </summary>
        private static Material ResolveCenterMaterial(TriggerSide side)
        {
            return side == TriggerSide.Sub ? SubCenterMaterial : MainCenterMaterial;
        }

        /// <summary>
        /// 根据侧别选择真实发射点 marker 材质。
        /// 同侧全部 projectile 共用同一颜色，便于一眼看出同枪齐射散布。
        /// </summary>
        private static Material ResolveLaunchMaterial(TriggerSide side)
        {
            return side == TriggerSide.Sub ? SubLaunchMaterial : MainLaunchMaterial;
        }

        /// <summary>
        /// 按收集好的主副侧发射点列表绘制真实发射点。
        /// 这里故意把真实发射点放到最后绘制，确保与理论中心重合时仍能看见最上层的真实点。
        /// </summary>
        private static void DrawLaunchMarkers(List<ResolvedLaunchMarker> launchMarkers)
        {
            if (launchMarkers == null || launchMarkers.Count == 0)
            {
                return;
            }

            for (int i = 0; i < launchMarkers.Count; i++)
            {
                ResolvedLaunchMarker point = launchMarkers[i];
                Material launchMaterial = ResolveLaunchMaterial(point.Side);
                DrawLink(point.TheoreticalCenterOriginWorld, point.ActualLaunchOriginWorld, CenterToLaunchMaterial, 0.02f);
                DrawPoint(point.ActualLaunchOriginWorld, launchMaterial, 0.16f);
            }
        }

        /// <summary>
        /// 绘制单个半透明圆点 marker。
        /// 半透明是为了让完全重合或近重合的点位仍能通过叠色关系辨认出来。
        /// </summary>
        private static void DrawPoint(Vector3 position, Material material, float diameter)
        {
            Graphics.DrawMesh(
                MarkerMesh,
                Matrix4x4.TRS(
                    LiftToOverlay(position),
                    Quaternion.identity,
                    new Vector3(diameter * PointDiameterScale, 1f, diameter * PointDiameterScale)),
                material,
                0);
        }

        /// <summary>
        /// 绘制两个关键点之间的连线。
        /// 连线本身不表达正式逻辑，只帮助快速理解偏移链条。
        /// </summary>
        private static void DrawLink(Vector3 start, Vector3 end, Material material, float lineWidth)
        {
            GenDraw.DrawLineBetween(
                LiftToOverlay(start),
                LiftToOverlay(end),
                OverlayAltitude,
                material,
                lineWidth);
        }

        /// <summary>
        /// 把任意世界坐标提升到 overlay 高度层。
        /// 避免 marker 与地面或物体贴图发生 Z fight。
        /// </summary>
        private static Vector3 LiftToOverlay(Vector3 position)
        {
            position.y = OverlayAltitude;
            return position;
        }

        /// <summary>
        /// 创建所有圆点 marker 共享的半透明圆形贴图。
        /// 圆边缘做轻微软化，避免地面上出现生硬像素锯齿。
        /// </summary>
        private static Texture2D CreateMarkerTexture()
        {
            const int textureSize = 64;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false)
            {
                name = "BDP_TriggerVisualMarkerCircle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[textureSize * textureSize];
            float center = (textureSize - 1) * 0.5f;
            float radius = center - 1f;
            float edgeSoftness = 3f;
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float deltaX = x - center;
                    float deltaY = y - center;
                    float distance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
                    float alpha = Mathf.Clamp01((radius - distance) / edgeSoftness);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return texture;
        }

        /// <summary>
        /// 创建单个点位专用材质。
        /// 颜色 alpha 直接决定圆点透明度，所有点位共享同一张圆形纹理。
        /// </summary>
        private static Material CreatePointMaterial(Color color)
        {
            Material material = new Material(ShaderDatabase.Transparent)
            {
                name = $"BDP_TriggerVisualMarkerPoint_{ColorUtility.ToHtmlStringRGBA(color)}",
                color = color,
                mainTexture = MarkerTexture,
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetTexture("_MainTex", MarkerTexture);
            material.SetColor("_Color", color);
            return material;
        }

        /// <summary>
        /// 叠加层里的一条已归属发射点记录。
        /// 它把主副侧信息与理论中心、真实源点打包，便于分两段绘制。
        /// </summary>
        private sealed class ResolvedLaunchMarker
        {
            /// <summary>
            /// 当前发射点所属主副侧。
            /// </summary>
            public TriggerSide Side { get; set; }

            /// <summary>
            /// 当前发射点对应的理论中心原点。
            /// </summary>
            public Vector3 TheoreticalCenterOriginWorld { get; set; }

            /// <summary>
            /// 当前发射点最终真实使用的发射原点。
            /// </summary>
            public Vector3 ActualLaunchOriginWorld { get; set; }
        }
    }
}
