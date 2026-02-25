using UnityEngine;

namespace BDP.Trigger
{
    /// <summary>
    /// 路径解析数据包——每tick决定"飞向哪"。
    /// 模块通过修改Destination来改变弹道目标（如追踪）。
    /// </summary>
    public struct PathContext
    {
        // ── 输入（只读）──
        /// <summary>当前弹道起点。</summary>
        public readonly Vector3 Origin;

        // ── 可修改 ──
        /// <summary>当前弹道目标点（模块可修改以实现追踪等）。</summary>
        public Vector3 Destination;

        /// <summary>是否已被某模块修改过。</summary>
        public bool Modified;

        public PathContext(Vector3 origin, Vector3 destination)
        {
            Origin = origin;
            Destination = destination;
            Modified = false;
        }
    }

    /// <summary>
    /// 路径解析管线接口——每tick修改destination（追踪/锁定目标）。
    /// 执行顺序：管线第1阶段。
    /// </summary>
    public interface IBDPPathResolver
    {
        /// <summary>修改路径目标。</summary>
        void ResolvePath(Bullet_BDP host, ref PathContext ctx);
    }
}
