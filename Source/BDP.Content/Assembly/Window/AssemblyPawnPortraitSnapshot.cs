using UnityEngine;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 装配台操作者的运行时正面肖像快照。
    /// </summary>
    internal sealed class AssemblyPawnPortraitSnapshot
    {
        /// <summary>
        /// 创建一张装配台专用肖像纹理快照。
        /// </summary>
        internal AssemblyPawnPortraitSnapshot(RenderTexture texture)
        {
            Texture = texture;
        }

        /// <summary>
        /// 包含透明背景的 Pawn 正面渲染纹理。
        /// </summary>
        internal RenderTexture Texture { get; }

        /// <summary>
        /// 释放快照持有的 Unity 纹理资源。
        /// </summary>
        internal void DestroyResources()
        {
            if (Texture != null)
            {
                Texture.DiscardContents();
                Object.Destroy(Texture);
            }
        }
    }
}
