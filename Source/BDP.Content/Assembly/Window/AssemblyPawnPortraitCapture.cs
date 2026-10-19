using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 使用 RimWorld 原版人物缓存渲染器捕获装配台操作者肖像。
    /// </summary>
    internal static class AssemblyPawnPortraitCapture
    {
        /// <summary>装配台肖像的独立纹理宽度。</summary>
        private const int PortraitWidth = 192;

        /// <summary>装配台肖像的独立纹理高度。</summary>
        private const int PortraitHeight = 384;

        /// <summary>
        /// 当前正在捕获肖像的 Pawn；只在同步的原版 RenderPawn 调用期间有效。
        /// </summary>
        private static Pawn capturingPawn;

        /// <summary>
        /// 判断指定 Pawn 当前是否正在进行无伤口肖像捕获。
        /// </summary>
        internal static bool IsCapturingWithoutWounds(Pawn pawn)
        {
            return capturingPawn != null && capturingPawn == pawn;
        }

        /// <summary>
        /// 捕获指定 Pawn 当前外观的正面肖像，不写入图片文件。
        /// </summary>
        internal static AssemblyPawnPortraitSnapshot Capture(Pawn pawn)
        {
            if (!CanCapture(pawn))
            {
                return null;
            }

            RenderTexture texture = new RenderTexture(
                PortraitWidth,
                PortraitHeight,
                24,
                RenderTextureFormat.ARGB32)
            {
                name = "BDP_AssemblyPawnPortrait",
                useMipMap = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            AssemblyPawnPortraitSnapshot snapshot = new AssemblyPawnPortraitSnapshot(texture);
            Camera cacheCamera = Find.PawnCacheCamera;
            Rect previousCameraRect = cacheCamera.rect;
            Vector3 previousCameraPosition = cacheCamera.transform.position;
            float previousOrthographicSize = cacheCamera.orthographicSize;
            RenderTexture previousTargetTexture = cacheCamera.targetTexture;
            bool captured = false;
            capturingPawn = pawn;
            try
            {
                texture.Create();
                cacheCamera.rect = new Rect(0f, 0f, 1f, 1f);
                pawn.Drawer.renderer.renderTree.SetDirty();
                Find.PawnCacheRenderer.RenderPawn(
                    pawn,
                    texture,
                    Vector3.zero,
                    1f,
                    0f,
                    Rot4.South,
                    renderHead: true,
                    renderHeadgear: true,
                    renderClothes: true,
                    portrait: true);
                captured = true;
                return snapshot;
            }
            catch (Exception ex)
            {
                Log.Error("[BDP] Failed to capture assembly operator pawn portrait without wounds.\n" + ex);
                return null;
            }
            finally
            {
                capturingPawn = null;
                cacheCamera.rect = previousCameraRect;
                cacheCamera.transform.position = previousCameraPosition;
                cacheCamera.orthographicSize = previousOrthographicSize;
                cacheCamera.targetTexture = previousTargetTexture;
                if (!captured)
                {
                    snapshot.DestroyResources();
                }
                else
                {
                    // 捕获期间使用了 Portrait 参数；让下一次世界绘制主动重建原版请求。
                    pawn.Drawer.renderer.renderTree.SetDirty();
                }
            }
        }

        /// <summary>
        /// 判断当前 Pawn 与原版缓存相机是否满足装配台肖像捕获条件。
        /// </summary>
        private static bool CanCapture(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && pawn.RaceProps != null
                && pawn.RaceProps.Humanlike
                && pawn.Drawer?.renderer?.renderTree != null
                && pawn.GetPosture() == PawnPosture.Standing
                && Find.PawnCacheCamera != null
                && Find.PawnCacheRenderer != null
                && (Find.UIRoot == null || !Find.UIRoot.HideMotes);
        }
    }
}
