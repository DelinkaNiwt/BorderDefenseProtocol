using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// BDP拖尾效果管理器——MapComponent，管理当前地图上所有BDP拖尾线段。
    ///
    /// 架构：
    ///   投射物每tick调用CreateSegment() → 从对象池获取或新建线段
    ///   MapComponentTick() → 逐个Tick线段，过期的回收到对象池
    ///   MapComponentUpdate() → 逐个Draw线段（带视口裁剪）
    ///
    /// 对象池：过期线段不销毁，回收到Stack复用，消除GC压力。
    /// 静态缓存：提供GetInstance(Map)快速访问。
    /// </summary>
    public class BDPEffectMapComponent : MapComponent
    {
        /// <summary>当前活跃的拖尾线段列表。</summary>
        private readonly List<BDPTrailSegment> segments
            = new List<BDPTrailSegment>();

        /// <summary>对象池：回收过期线段供复用，避免GC。</summary>
        private readonly Stack<BDPTrailSegment> pool
            = new Stack<BDPTrailSegment>();

        /// <summary>静态缓存：Map→实例映射。</summary>
        private static readonly Dictionary<int, BDPEffectMapComponent> cache
            = new Dictionary<int, BDPEffectMapComponent>();

        public BDPEffectMapComponent(Map map) : base(map)
        {
            cache[map.uniqueID] = this;
        }

        /// <summary>快速获取指定地图的实例。</summary>
        public static BDPEffectMapComponent GetInstance(Map map)
        {
            if (map == null) return null;
            if (cache.TryGetValue(map.uniqueID, out var inst)) return inst;
            inst = map.GetComponent<BDPEffectMapComponent>();
            if (inst != null) cache[map.uniqueID] = inst;
            return inst;
        }

        /// <summary>
        /// 创建一段拖尾线段（优先从对象池复用）。
        /// 由投射物在TickInterval中调用。
        /// </summary>
        public void CreateSegment(
            Vector3 origin, Vector3 destination,
            Material material, Color baseColor, float width,
            int duration, float startOpacity,
            float decayTime, float decaySharpness)
        {
            BDPTrailSegment seg = pool.Count > 0
                ? pool.Pop()
                : new BDPTrailSegment();
            seg.Reset(origin, destination, material, baseColor,
                width, duration, startOpacity, decayTime, decaySharpness);
            segments.Add(seg);
        }

        /// <summary>
        /// 每tick：遍历所有线段，过期的回收到对象池。
        /// </summary>
        public override void MapComponentTick()
        {
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                if (!segments[i].Tick())
                {
                    // 回收到对象池
                    pool.Push(segments[i]);
                    // O(1)删除：用末尾元素覆盖
                    segments[i] = segments[segments.Count - 1];
                    segments.RemoveAt(segments.Count - 1);
                }
            }
        }

        /// <summary>
        /// 每帧：遍历所有线段，带视口裁剪渲染。
        /// </summary>
        public override void MapComponentUpdate()
        {
            if (segments.Count == 0) return;

            CellRect visibleRect = Find.CameraDriver.CurrentViewRect.ExpandedBy(2);

            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                Vector3 mid = (seg.origin + seg.destination) / 2f;
                if (visibleRect.Contains(mid.ToIntVec3()))
                    seg.Draw();
            }
        }

        /// <summary>地图卸载时清理缓存。</summary>
        public override void MapRemoved()
        {
            cache.Remove(map.uniqueID);
        }
    }
}
