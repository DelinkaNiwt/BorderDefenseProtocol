using System.Collections.Generic;
using BDP.Support.Diagnostics;
using UnityEngine;
using Verse;

namespace BDP.Content.Projectiles.BeamTrail
{
    /// <summary>
    /// 光束拖尾地图组件。
    /// 它统一负责当前地图上全部活动拖尾线段的保存、推进、绘制与存读档恢复。
    /// </summary>
    public sealed class BeamTrailMapComponent : MapComponent
    {
        /// <summary>
        /// 当前地图上的全部活动线段。
        /// 它们都是已经沉淀成历史痕迹的线段，会正常淡出并进入存档。
        /// </summary>
        private List<BeamTrailSegment> activeSegments = new List<BeamTrailSegment>();

        /// <summary>
        /// 当前地图上仍跟随活体投射物显示的临时头段。
        /// 它们只在投射物存活期内绘制，不参与淡出，也不进入存档。
        /// </summary>
        private readonly Dictionary<string, BeamTrailSegment> liveSegmentsByProjectileId = new Dictionary<string, BeamTrailSegment>();

        /// <summary>
        /// 当前地图上的线段对象池。
        /// </summary>
        private readonly Stack<BeamTrailSegment> pool = new Stack<BeamTrailSegment>();

        /// <summary>
        /// 当前地图的材质缓存。
        /// key 按贴图路径和绘制层区分；颜色由线段绘制时通过属性块注入。
        /// </summary>
        private readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        /// <summary>
        /// 当前地图已经警告过的贴图路径。
        /// 它用于避免贴图缺失时日志刷屏。
        /// </summary>
        private readonly HashSet<string> warnedTexturePaths = new HashSet<string>();

        /// <summary>
        /// 地图级组件静态缓存。
        /// </summary>
        private static readonly Dictionary<int, BeamTrailMapComponent> cache = new Dictionary<int, BeamTrailMapComponent>();

        /// <summary>
        /// 用当前地图初始化拖尾地图组件。
        /// </summary>
        /// <param name="map">当前地图实例。</param>
        public BeamTrailMapComponent(Map map) : base(map)
        {
            RegisterSelfToCache();
        }

        /// <summary>
        /// 通过地图快速取得拖尾地图组件。
        /// </summary>
        /// <param name="map">目标地图。</param>
        /// <returns>目标地图上的拖尾地图组件；取不到时返回空。</returns>
        public static BeamTrailMapComponent GetOrCreate(Map map)
        {
            if (map == null)
            {
                return null;
            }

            if (cache.TryGetValue(map.uniqueID, out BeamTrailMapComponent component) && component != null)
            {
                return component;
            }

            component = map.GetComponent<BeamTrailMapComponent>();
            if (component != null)
            {
                cache[map.uniqueID] = component;
            }

            return component;
        }

        /// <summary>
        /// 追加一条新的拖尾线段。
        /// </summary>
        /// <param name="start">当前线段起点。</param>
        /// <param name="end">当前线段终点。</param>
        /// <param name="appearance">当前线段外观快照。</param>
        internal void AppendSegment(Vector3 start, Vector3 end, BeamTrailAppearanceSnapshot appearance)
        {
            if (appearance == null || (end - start).MagnitudeHorizontal() <= 0.0001f)
            {
                return;
            }

            BeamTrailSegment segment = pool.Count > 0
                ? pool.Pop()
                : new BeamTrailSegment();
            segment.Reset(start, end, appearance);
            activeSegments.Add(segment);
        }

        /// <summary>
        /// 用当前样本覆盖指定投射物的活体头段。
        /// 该线段只代表“当前最新一段仍活着的拖尾头部”，不会立即沉淀为历史段。
        /// </summary>
        /// <param name="projectileThingId">目标投射物实体标识。</param>
        /// <param name="start">头段起点。</param>
        /// <param name="end">头段终点。</param>
        /// <param name="appearance">头段外观快照。</param>
        internal void SetLiveSegment(string projectileThingId, Vector3 start, Vector3 end, BeamTrailAppearanceSnapshot appearance)
        {
            if (string.IsNullOrWhiteSpace(projectileThingId)
                || appearance == null
                || (end - start).MagnitudeHorizontal() <= 0.0001f)
            {
                return;
            }

            if (!liveSegmentsByProjectileId.TryGetValue(projectileThingId, out BeamTrailSegment segment) || segment == null)
            {
                segment = pool.Count > 0
                    ? pool.Pop()
                    : new BeamTrailSegment();
                liveSegmentsByProjectileId[projectileThingId] = segment;
            }

            segment.Reset(start, end, appearance);
        }

        /// <summary>
        /// 把指定投射物当前活体头段晋升为历史段。
        /// 只有在下一次真实样本到来后，上一段头部才允许沉淀到历史拖尾里。
        /// </summary>
        /// <param name="projectileThingId">目标投射物实体标识。</param>
        internal void PromoteLiveSegment(string projectileThingId)
        {
            if (string.IsNullOrWhiteSpace(projectileThingId)
                || !liveSegmentsByProjectileId.TryGetValue(projectileThingId, out BeamTrailSegment segment)
                || segment == null)
            {
                return;
            }

            liveSegmentsByProjectileId.Remove(projectileThingId);
            activeSegments.Add(segment);
        }

        /// <summary>
        /// 清理指定投射物的活体头段。
        /// 投射物一旦终止，这一段必须立刻停绘，不允许再沉淀成历史段。
        /// </summary>
        /// <param name="projectileThingId">目标投射物实体标识。</param>
        internal void ClearLiveSegment(string projectileThingId)
        {
            if (string.IsNullOrWhiteSpace(projectileThingId)
                || !liveSegmentsByProjectileId.TryGetValue(projectileThingId, out BeamTrailSegment segment))
            {
                return;
            }

            liveSegmentsByProjectileId.Remove(projectileThingId);
            if (segment != null)
            {
                pool.Push(segment);
            }
        }

        /// <summary>
        /// 在地图级 FinalizeInit 阶段重新注册缓存。
        /// </summary>
        public override void FinalizeInit()
        {
            RegisterSelfToCache();
        }

        /// <summary>
        /// 每 tick 推进全部活动线段寿命。
        /// </summary>
        public override void MapComponentTick()
        {
            if (activeSegments == null || activeSegments.Count == 0)
            {
                return;
            }

            for (int i = activeSegments.Count - 1; i >= 0; i--)
            {
                BeamTrailSegment segment = activeSegments[i];
                if (segment != null && segment.Tick())
                {
                    continue;
                }

                if (segment != null)
                {
                    pool.Push(segment);
                }

                activeSegments[i] = activeSegments[activeSegments.Count - 1];
                activeSegments.RemoveAt(activeSegments.Count - 1);
            }
        }

        /// <summary>
        /// 在地图绘制阶段绘制全部可见拖尾线段。
        /// 材质解析保持在这里惰性发生，避免读档线程触碰 Unity 材质系统。
        /// </summary>
        public override void MapComponentDraw()
        {
            bool hasHistorySegments = activeSegments != null && activeSegments.Count > 0;
            bool hasLiveSegments = liveSegmentsByProjectileId.Count > 0;
            if ((!hasHistorySegments && !hasLiveSegments)
                || map == null
                || Find.CurrentMap != map
                || Find.CameraDriver == null)
            {
                return;
            }

            CellRect visibleRect = Find.CameraDriver.CurrentViewRect.ExpandedBy(2);
            for (int i = 0; i < activeSegments.Count; i++)
            {
                BeamTrailSegment segment = activeSegments[i];
                DrawSegmentIfVisible(segment, visibleRect);
            }

            foreach (BeamTrailSegment segment in liveSegmentsByProjectileId.Values)
            {
                DrawSegmentIfVisible(segment, visibleRect);
            }
        }

        /// <summary>
        /// 存读档当前地图上的活动拖尾线段。
        /// 这里不再主动预建材质缓存，避免读档线程调用 MaterialPool。
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref activeSegments, "activeSegments", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (activeSegments == null)
                {
                    activeSegments = new List<BeamTrailSegment>();
                }

                materialCache.Clear();
                warnedTexturePaths.Clear();
                liveSegmentsByProjectileId.Clear();
                RegisterSelfToCache();
                if (Prefs.DevMode)
                {
                    BdpDiagnostics.Throttled(
                        "beamtrail.map.restore." + (map != null ? map.uniqueID.ToString() : "<null>"),
                        "光束拖尾地图组件已恢复活动线段。count=" + activeSegments.Count,
                        30);
                }
            }
        }

        /// <summary>
        /// 当前地图移除时清理静态缓存。
        /// </summary>
        public override void MapRemoved()
        {
            if (map != null)
            {
                cache.Remove(map.uniqueID);
            }

            liveSegmentsByProjectileId.Clear();
        }

        /// <summary>
        /// 解析指定贴图路径对应的材质。
        /// </summary>
        /// <param name="trailTexPath">目标贴图路径。</param>
        /// <param name="core">是否解析拖尾内芯材质。</param>
        /// <param name="renderQueue">显式渲染队列；零表示使用 Shader 默认队列。</param>
        /// <returns>可用于拖尾绘制的材质；失败时返回空。</returns>
        private Material ResolveMaterial(string trailTexPath, bool core, int renderQueue = 0)
        {
            if (string.IsNullOrWhiteSpace(trailTexPath))
            {
                WarnMissingTextureOnce("<empty>");
                return null;
            }

            string cacheKey = (core ? "core|" : "outer|")
                + trailTexPath
                + "|queue="
                + renderQueue;
            if (materialCache.TryGetValue(cacheKey, out Material cachedMaterial))
            {
                return cachedMaterial;
            }

            Shader shader = core ? ShaderDatabase.Transparent : ShaderDatabase.MoteGlow;
            Material material = renderQueue > 0
                ? MaterialPool.MatFrom(trailTexPath, shader, Color.white, renderQueue)
                : MaterialPool.MatFrom(trailTexPath, shader, Color.white);
            if (material == null)
            {
                WarnMissingTextureOnce(trailTexPath);
                return null;
            }

            materialCache[cacheKey] = material;
            return material;
        }

        /// <summary>
        /// 判断当前线段的整体覆盖范围是否与当前视口相交。
        /// </summary>
        /// <param name="segment">待判断的线段。</param>
        /// <param name="visibleRect">当前可见范围。</param>
        /// <returns>为真表示值得继续绘制。</returns>
        private static bool IsPotentiallyVisible(BeamTrailSegment segment, CellRect visibleRect)
        {
            float halfWidth = Mathf.Max(0.01f, segment.TrailWidth) * 0.5f;
            float minX = Mathf.Min(segment.Start.x, segment.End.x) - halfWidth;
            float maxX = Mathf.Max(segment.Start.x, segment.End.x) + halfWidth;
            float minZ = Mathf.Min(segment.Start.z, segment.End.z) - halfWidth;
            float maxZ = Mathf.Max(segment.Start.z, segment.End.z) + halfWidth;

            return maxX >= visibleRect.minX
                && minX <= visibleRect.maxX + 1f
                && maxZ >= visibleRect.minZ
                && minZ <= visibleRect.maxZ + 1f;
        }

        /// <summary>
        /// 在当前视口内绘制一条线段。
        /// 该入口同时服务历史段与活体头段，保持它们的渲染表现一致。
        /// </summary>
        /// <param name="segment">待绘制线段。</param>
        /// <param name="visibleRect">当前可见范围。</param>
        private void DrawSegmentIfVisible(BeamTrailSegment segment, CellRect visibleRect)
        {
            if (segment == null || !IsPotentiallyVisible(segment, visibleRect))
            {
                return;
            }

            Material outerMaterial = ResolveMaterial(segment.TrailTexPath, false);
            if (outerMaterial == null)
            {
                return;
            }

            segment.Draw(outerMaterial);
            if (segment.HasTrailCore)
            {
                int outerRenderQueue = Mathf.Max(3000, outerMaterial.renderQueue);
                Material coreMaterial = ResolveMaterial(
                    segment.TrailTexPath,
                    true,
                    outerRenderQueue + 1);
                segment.DrawCore(coreMaterial);
            }
        }

        /// <summary>
        /// 对同一路径只警告一次贴图缺失。
        /// </summary>
        /// <param name="trailTexPath">缺失的贴图路径。</param>
        private void WarnMissingTextureOnce(string trailTexPath)
        {
            string safePath = string.IsNullOrWhiteSpace(trailTexPath) ? "<empty>" : trailTexPath;
            if (!warnedTexturePaths.Add(safePath))
            {
                return;
            }

            Log.Warning("[BDP.BeamTrail] material resolve failed: " + safePath);
        }

        /// <summary>
        /// 把当前实例注册到地图组件静态缓存。
        /// </summary>
        private void RegisterSelfToCache()
        {
            if (map != null)
            {
                cache[map.uniqueID] = this;
            }
        }
    }
}
