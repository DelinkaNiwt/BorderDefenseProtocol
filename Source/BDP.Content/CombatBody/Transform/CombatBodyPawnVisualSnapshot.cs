using UnityEngine;

namespace BDP.Content.CombatBody.Transform
{
    /// <summary>
    /// 一张由 RimWorld 原版人物缓存渲染器生成的完整人物最终画面。
    /// </summary>
    internal sealed class CombatBodyPawnVisualSnapshot
    {
        /// <summary>
        /// 创建一张可反复捕获的完整人物快照资源。
        /// </summary>
        internal CombatBodyPawnVisualSnapshot(RenderTexture texture, Material material)
        {
            Texture = texture;
            Material = material;
        }

        /// <summary>
        /// 包含透明背景的完整人物渲染纹理。
        /// </summary>
        internal RenderTexture Texture { get; }

        /// <summary>
        /// 用于在世界中重绘该快照的透明裁切材质。
        /// </summary>
        internal Material Material { get; }

        /// <summary>
        /// 销毁不再进入复用池的底层 Unity 资源。
        /// </summary>
        internal void DestroyResources()
        {
            if (Texture != null)
            {
                Texture.DiscardContents();
                Object.Destroy(Texture);
            }

            if (Material != null)
            {
                Object.Destroy(Material);
            }
        }
    }
}
