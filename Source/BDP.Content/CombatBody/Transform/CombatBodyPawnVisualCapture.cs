using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.CombatBody.Transform
{
    /// <summary>
    /// 复用 RimWorld 原版人物缓存渲染器，冻结当前完整人物最终画面。
    /// </summary>
    internal static class CombatBodyPawnVisualCapture
    {
        /// <summary>
        /// 沿用原版 PawnTextureAtlas 的单帧尺寸。
        /// </summary>
        private const int SnapshotSize = 128;

        /// <summary>
        /// 最多保留的空闲快照资源数量。
        /// </summary>
        private const int MaxRetainedSnapshots = 8;

        /// <summary>
        /// 已释放、等待下一次变换复用的快照资源。
        /// </summary>
        private static readonly Stack<CombatBodyPawnVisualSnapshot> SnapshotPool =
            new Stack<CombatBodyPawnVisualSnapshot>();

        /// <summary>
        /// 捕获指定 Pawn 当前由原版渲染树合成出的完整人物画面。
        /// </summary>
        internal static CombatBodyPawnVisualSnapshot Capture(Pawn pawn)
        {
            if (!CanCapture(pawn))
            {
                return null;
            }

            CombatBodyPawnVisualSnapshot snapshot = Rent();
            Camera cacheCamera = Find.PawnCacheCamera;
            Rect previousCameraRect = cacheCamera.rect;
            Vector3 previousCameraPosition = cacheCamera.transform.position;
            float previousOrthographicSize = cacheCamera.orthographicSize;
            RenderTexture previousTargetTexture = cacheCamera.targetTexture;
            bool captured = false;
            try
            {
                if (!snapshot.Texture.IsCreated())
                {
                    snapshot.Texture.Create();
                }

                cacheCamera.rect = new Rect(0f, 0f, 1f, 1f);
                pawn.Drawer.renderer.renderTree.SetDirty();
                float bodyAngle = pawn.Drawer.renderer.BodyAngle(PawnRenderFlags.None);
                Find.PawnCacheRenderer.RenderPawn(
                    pawn,
                    snapshot.Texture,
                    Vector3.zero,
                    1f,
                    bodyAngle,
                    pawn.Rotation,
                    renderHead: true,
                    renderHeadgear: true,
                    renderClothes: true,
                    portrait: false);
                captured = true;
                return snapshot;
            }
            catch (Exception ex)
            {
                Log.Error("[BDP] Failed to capture complete combat-body pawn visual.\n" + ex);
                return null;
            }
            finally
            {
                cacheCamera.rect = previousCameraRect;
                cacheCamera.transform.position = previousCameraPosition;
                cacheCamera.orthographicSize = previousOrthographicSize;
                cacheCamera.targetTexture = previousTargetTexture;
                if (!captured)
                {
                    Release(snapshot);
                }
            }
        }

        /// <summary>
        /// 将不再使用的快照归还有限资源池。
        /// </summary>
        internal static void Release(CombatBodyPawnVisualSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            if (SnapshotPool.Count < MaxRetainedSnapshots)
            {
                SnapshotPool.Push(snapshot);
                return;
            }

            snapshot.DestroyResources();
        }

        /// <summary>
        /// 判断当前 Pawn 与原版缓存相机是否适合完整画面捕获。
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

        /// <summary>
        /// 从池中租用一张快照；池为空时创建一组纹理与材质。
        /// </summary>
        private static CombatBodyPawnVisualSnapshot Rent()
        {
            if (SnapshotPool.Count > 0)
            {
                return SnapshotPool.Pop();
            }

            RenderTexture texture = new RenderTexture(
                SnapshotSize,
                SnapshotSize,
                24,
                RenderTextureFormat.ARGB32)
            {
                name = "BDP_CombatBodyPawnSnapshot",
                useMipMap = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Material material = new Material(ShaderDatabase.Cutout)
            {
                name = "BDP_CombatBodyPawnSnapshotMaterial",
                mainTexture = texture
            };
            return new CombatBodyPawnVisualSnapshot(texture, material);
        }
    }
}
