using UnityEngine;
using Verse;

namespace BDP.Core.Trion.External
{
    /// <summary>
    /// Trion 状态条扩展区上下文。
    /// </summary>
    public sealed class TrionGizmoExtensionContext
    {
        /// <summary>
        /// 初始化扩展区上下文。
        /// </summary>
        public TrionGizmoExtensionContext(Thing owner, ITrionReader reader, Rect extensionRect)
        {
            Owner = owner;
            Reader = reader;
            ExtensionRect = extensionRect;
        }

        /// <summary>
        /// 当前 Trion 资源宿主。
        /// </summary>
        public Thing Owner { get; }

        /// <summary>
        /// 当前 Trion 正式只读口。
        /// </summary>
        public ITrionReader Reader { get; }

        /// <summary>
        /// 右上角扩展区的绘制矩形。
        /// </summary>
        public Rect ExtensionRect { get; }
    }
}
