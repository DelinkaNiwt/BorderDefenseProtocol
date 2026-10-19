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
        /// 只对短采样段启用绘制阶段合并。
        /// 超过该长度的线段保持原有一段一绘制，避免改变高速弹的既有观感。
        /// </summary>
        private const float MaxCoalescedSegmentLength = 1.25f;

        /// <summary>
        /// 单次绘制最多合并的原始线段数量。
        /// </summary>
        private const int MaxVisualCoalesceSegments = 3;

        /// <summary>
        /// 允许合并的相邻线段最大转角。
        /// </summary>
        private const float VisualCoalesceAngleThreshold = 4f;

        /// <summary>
        /// 允许合并的相邻线段透明度最大差异。
        /// </summary>
        private const float VisualCoalesceOpacityDifference = 0.12f;

        /// <summary>
        /// 当前地图组件已经分配的拖尾顺序号。
        /// </summary>
        private int nextSequenceId;

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
        /// 绘制阶段按投射物分组的历史线段缓存。
        /// 它只用于当前帧的可视合并，不进入存档。
        /// </summary>
        private readonly Dictionary<string, List<BeamTrailSegment>> drawHistoryByProjectileId =
            new Dictionary<string, List<BeamTrailSegment>>();

        /// <summary>
        /// 当前活体头段在渲染帧中的显示终点。
        /// 它只服务视觉插值，不进入存档，也不参与投射物真实位置。
        /// </summary>
        private readonly Dictionary<string, Vector3> liveRenderEnds =
            new Dictionary<string, Vector3>();

        /// <summary>
        /// 当前活体头段上一次执行渲染插值的帧号。
        /// 防止同一帧被重复绘制时重复推进显示位置。
        /// </summary>
        private readonly Dictionary<string, int> liveRenderFrames =
            new Dictionary<string, int>();

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
            segment.OwnerProjectileThingId = projectileThingId;
            if (liveRenderEnds.TryGetValue(projectileThingId, out Vector3 previousRenderEnd))
            {
                liveRenderEnds[projectileThingId] = ProjectPointOntoSegment(
                    previousRenderEnd,
                    segment.Start,
                    segment.End);
            }
            else
            {
                liveRenderEnds[projectileThingId] = segment.Start;
            }

            liveRenderFrames.Remove(projectileThingId);
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
            liveRenderEnds.Remove(projectileThingId);
            liveRenderFrames.Remove(projectileThingId);
            segment.SequenceId = ++nextSequenceId;
            activeSegments.Add(segment);
        }

        /// <summary>
        /// 在投射物终止时固化最后活动头段。
        /// 终点来自宿主提供的视觉终点，不从目标语义重新构造线段。
        /// </summary>
        /// <param name="projectileThingId">目标投射物实体标识。</param>
        /// <param name="terminalPosition">宿主提供的视觉终止位置。</param>
        internal void FinalizeLiveSegment(string projectileThingId, Vector3 terminalPosition)
        {
            if (string.IsNullOrWhiteSpace(projectileThingId)
                || !liveSegmentsByProjectileId.TryGetValue(projectileThingId, out BeamTrailSegment segment)
                || segment == null)
            {
                return;
            }

            Vector3 normalizedTerminalPosition = terminalPosition.Yto0();
            if ((normalizedTerminalPosition - segment.Start).MagnitudeHorizontal() <= 0.0001f)
            {
                liveSegmentsByProjectileId.Remove(projectileThingId);
                liveRenderEnds.Remove(projectileThingId);
                liveRenderFrames.Remove(projectileThingId);
                pool.Push(segment);
                return;
            }

            segment.End = normalizedTerminalPosition;
            liveSegmentsByProjectileId.Remove(projectileThingId);
            liveRenderEnds.Remove(projectileThingId);
            liveRenderFrames.Remove(projectileThingId);
            segment.SequenceId = ++nextSequenceId;
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
            liveRenderEnds.Remove(projectileThingId);
            liveRenderFrames.Remove(projectileThingId);
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
            drawHistoryByProjectileId.Clear();
            for (int i = 0; i < activeSegments.Count; i++)
            {
                BeamTrailSegment segment = activeSegments[i];
                if (segment == null || string.IsNullOrWhiteSpace(segment.OwnerProjectileThingId))
                {
                    DrawSegmentIfVisible(segment, visibleRect);
                    continue;
                }

                if (!drawHistoryByProjectileId.TryGetValue(
                    segment.OwnerProjectileThingId,
                    out List<BeamTrailSegment> historySegments))
                {
                    historySegments = new List<BeamTrailSegment>();
                    drawHistoryByProjectileId.Add(segment.OwnerProjectileThingId, historySegments);
                }

                historySegments.Add(segment);
            }

            foreach (List<BeamTrailSegment> historySegments in drawHistoryByProjectileId.Values)
            {
                DrawHistorySegments(historySegments, visibleRect);
            }

            foreach (KeyValuePair<string, BeamTrailSegment> liveEntry in liveSegmentsByProjectileId)
            {
                DrawLiveSegmentIfVisible(liveEntry.Key, liveEntry.Value, visibleRect);
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
                nextSequenceId = 0;
                drawHistoryByProjectileId.Clear();
                liveRenderEnds.Clear();
                liveRenderFrames.Clear();
                for (int i = 0; i < activeSegments.Count; i++)
                {
                    BeamTrailSegment segment = activeSegments[i];
                    if (segment != null && segment.SequenceId > nextSequenceId)
                    {
                        nextSequenceId = segment.SequenceId;
                    }
                }
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
            drawHistoryByProjectileId.Clear();
            liveRenderEnds.Clear();
            liveRenderFrames.Clear();
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
            if (segment == null)
            {
                return false;
            }

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
        /// 判断指定绘制端点的覆盖范围是否与当前视口相交。
        /// </summary>
        /// <param name="start">绘制起点。</param>
        /// <param name="end">绘制终点。</param>
        /// <param name="width">绘制宽度。</param>
        /// <param name="visibleRect">当前可见范围。</param>
        /// <returns>为真表示值得继续绘制。</returns>
        private static bool IsPotentiallyVisible(
            Vector3 start,
            Vector3 end,
            float width,
            CellRect visibleRect)
        {
            float halfWidth = Mathf.Max(0.01f, width) * 0.5f;
            float minX = Mathf.Min(start.x, end.x) - halfWidth;
            float maxX = Mathf.Max(start.x, end.x) + halfWidth;
            float minZ = Mathf.Min(start.z, end.z) - halfWidth;
            float maxZ = Mathf.Max(start.z, end.z) + halfWidth;

            return maxX >= visibleRect.minX
                && minX <= visibleRect.maxX + 1f
                && maxZ >= visibleRect.minZ
                && minZ <= visibleRect.maxZ + 1f;
        }

        /// <summary>
        /// 按投射物和时间顺序绘制历史拖尾，并把近共线的短段临时合并。
        /// 原始线段仍独立推进寿命，合并只存在于当前绘制帧。
        /// </summary>
        /// <param name="historySegments">同一投射物的历史线段。</param>
        /// <param name="visibleRect">当前可见范围。</param>
        private void DrawHistorySegments(
            List<BeamTrailSegment> historySegments,
            CellRect visibleRect)
        {
            if (historySegments == null || historySegments.Count == 0)
            {
                return;
            }

            if (historySegments.Count > 1)
            {
                historySegments.Sort(
                    (left, right) => left.SequenceId.CompareTo(right.SequenceId));
            }

            int groupStartIndex = 0;
            while (groupStartIndex < historySegments.Count)
            {
                BeamTrailSegment first = historySegments[groupStartIndex];
                BeamTrailSegment last = first;
                float groupOpacity = first.ResolveOpacity();
                float previousOpacity = groupOpacity;
                int groupEndIndex = groupStartIndex + 1;

                while (groupEndIndex < historySegments.Count
                    && groupEndIndex - groupStartIndex < MaxVisualCoalesceSegments)
                {
                    BeamTrailSegment next = historySegments[groupEndIndex];
                    if (!CanCoalesce(last, next, previousOpacity, out float nextOpacity))
                    {
                        break;
                    }

                    last = next;
                    groupOpacity = Mathf.Min(groupOpacity, nextOpacity);
                    previousOpacity = nextOpacity;
                    groupEndIndex++;
                }

                if (groupEndIndex - groupStartIndex == 1)
                {
                    DrawSegmentIfVisible(first, visibleRect);
                }
                else
                {
                    DrawCoalescedSegmentIfVisible(
                        first,
                        last,
                        groupOpacity,
                        visibleRect);
                }

                groupStartIndex = groupEndIndex;
            }
        }

        /// <summary>
        /// 判断两个相邻历史段是否可以在当前帧合并绘制。
        /// </summary>
        /// <param name="previous">前一条历史线段。</param>
        /// <param name="next">后一条历史线段。</param>
        /// <param name="previousOpacity">前一条线段的透明度。</param>
        /// <param name="nextOpacity">后一条线段的透明度。</param>
        /// <returns>为真表示两条线段可以临时合并。</returns>
        private static bool CanCoalesce(
            BeamTrailSegment previous,
            BeamTrailSegment next,
            float previousOpacity,
            out float nextOpacity)
        {
            nextOpacity = 0f;
            if (previous == null
                || next == null
                || previous.SequenceId <= 0
                || next.SequenceId != previous.SequenceId + 1
                || previous.OwnerProjectileThingId != next.OwnerProjectileThingId)
            {
                return false;
            }

            Vector3 previousDirection = previous.End - previous.Start;
            Vector3 nextDirection = next.End - next.Start;
            float previousLength = previousDirection.MagnitudeHorizontal();
            float nextLength = nextDirection.MagnitudeHorizontal();
            if (previousLength <= 0.0001f
                || nextLength <= 0.0001f
                || previousLength > MaxCoalescedSegmentLength
                || nextLength > MaxCoalescedSegmentLength
                || (next.Start - previous.End).MagnitudeHorizontal() > 0.05f)
            {
                return false;
            }

            float angle = Vector3.Angle(previousDirection.Yto0(), nextDirection.Yto0());
            if (angle > VisualCoalesceAngleThreshold)
            {
                return false;
            }

            nextOpacity = next.ResolveOpacity();
            return Mathf.Abs(nextOpacity - previousOpacity)
                <= VisualCoalesceOpacityDifference;
        }

        /// <summary>
        /// 绘制一组已通过安全条件的临时合并线段。
        /// 合并透明度取组内最低值，确保不会比最旧原始段更亮或更难消失。
        /// </summary>
        /// <param name="first">合并组首段。</param>
        /// <param name="last">合并组末段。</param>
        /// <param name="opacity">合并组使用的透明度。</param>
        /// <param name="visibleRect">当前可见范围。</param>
        private void DrawCoalescedSegmentIfVisible(
            BeamTrailSegment first,
            BeamTrailSegment last,
            float opacity,
            CellRect visibleRect)
        {
            if (first == null
                || last == null
                || !IsPotentiallyVisible(first.Start, last.End, first.TrailWidth, visibleRect))
            {
                return;
            }

            Material outerMaterial = ResolveMaterial(first.TrailTexPath, false);
            if (outerMaterial == null)
            {
                return;
            }

            first.DrawTransient(outerMaterial, first.Start, last.End, opacity);
            if (first.HasTrailCore)
            {
                int outerRenderQueue = Mathf.Max(3000, outerMaterial.renderQueue);
                Material coreMaterial = ResolveMaterial(
                    first.TrailTexPath,
                    true,
                    outerRenderQueue + 1);
                first.DrawCoreTransient(coreMaterial, first.Start, last.End, opacity);
            }
        }

        /// <summary>
        /// 绘制一个经过渲染插值的活体头段。
        /// 插值只改变当前帧的显示终点，不改变投射物真实终点。
        /// </summary>
        /// <param name="projectileThingId">目标投射物实体标识。</param>
        /// <param name="segment">当前活体头段。</param>
        /// <param name="visibleRect">当前可见范围。</param>
        private void DrawLiveSegmentIfVisible(
            string projectileThingId,
            BeamTrailSegment segment,
            CellRect visibleRect)
        {
            if (segment == null)
            {
                return;
            }

            Vector3 drawEnd = ResolveLiveDrawEnd(projectileThingId, segment);
            if (!IsPotentiallyVisible(segment.Start, drawEnd, segment.TrailWidth, visibleRect))
            {
                return;
            }

            Material outerMaterial = ResolveMaterial(segment.TrailTexPath, false);
            if (outerMaterial == null)
            {
                return;
            }

            float opacity = segment.ResolveOpacity();
            segment.DrawTransient(outerMaterial, segment.Start, drawEnd, opacity);
            if (segment.HasTrailCore)
            {
                int outerRenderQueue = Mathf.Max(3000, outerMaterial.renderQueue);
                Material coreMaterial = ResolveMaterial(
                    segment.TrailTexPath,
                    true,
                    outerRenderQueue + 1);
                segment.DrawCoreTransient(coreMaterial, segment.Start, drawEnd, opacity);
            }
        }

        /// <summary>
        /// 计算活体头段当前帧的显示终点。
        /// 使用原版实时帧间隔推进到最近的真实样本，不把插值结果写回飞行状态。
        /// </summary>
        /// <param name="projectileThingId">目标投射物实体标识。</param>
        /// <param name="segment">当前活体头段。</param>
        /// <returns>当前帧实际绘制的终点。</returns>
        private Vector3 ResolveLiveDrawEnd(
            string projectileThingId,
            BeamTrailSegment segment)
        {
            if (!liveRenderEnds.TryGetValue(projectileThingId, out Vector3 renderEnd))
            {
                renderEnd = segment.Start;
            }

            int frameCount = RealTime.frameCount;
            if (liveRenderFrames.TryGetValue(projectileThingId, out int lastFrame)
                && lastFrame == frameCount)
            {
                return renderEnd;
            }

            liveRenderFrames[projectileThingId] = frameCount;
            float tickRateMultiplier = Find.TickManager != null
                ? Mathf.Max(0f, Find.TickManager.TickRateMultiplier)
                : 1f;
            float frameProgress = Mathf.Clamp01(RealTime.deltaTime * 60f * tickRateMultiplier);
            if (Find.TickManager != null && Find.TickManager.TicksThisFrame > 1)
            {
                frameProgress = 1f;
            }

            Vector3 targetEnd = segment.End;
            float sampleLength = (targetEnd - segment.Start).MagnitudeHorizontal();
            renderEnd = Vector3.MoveTowards(
                renderEnd,
                targetEnd,
                sampleLength * frameProgress);
            renderEnd = ProjectPointOntoSegment(renderEnd, segment.Start, targetEnd);
            liveRenderEnds[projectileThingId] = renderEnd;
            return renderEnd;
        }

        /// <summary>
        /// 把上一帧活体头段终点投影到本次真实样本线段上。
        /// 这样追踪方向变化时不会把旧方向的插值点带进新线段。
        /// </summary>
        /// <param name="point">待投影点。</param>
        /// <param name="start">当前线段起点。</param>
        /// <param name="end">当前线段终点。</param>
        /// <returns>位于当前线段上的投影点。</returns>
        private static Vector3 ProjectPointOntoSegment(
            Vector3 point,
            Vector3 start,
            Vector3 end)
        {
            Vector3 direction = (end - start).Yto0();
            float lengthSquared = direction.sqrMagnitude;
            if (lengthSquared <= 0.0001f)
            {
                return start;
            }

            float progress = Vector3.Dot(
                (point - start).Yto0(),
                direction) / lengthSquared;
            return Vector3.Lerp(start, end, Mathf.Clamp01(progress));
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
