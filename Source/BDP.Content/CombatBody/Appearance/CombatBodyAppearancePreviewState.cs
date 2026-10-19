using BDP.Content.Assembly;
using BDP.Core.CombatBody;
using UnityEngine;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 战斗体编辑台当前一次预览的纹理和缓存键。
    /// </summary>
    public sealed class CombatBodyAppearancePreviewState
    {
        /// <summary>当前预览对应的模板编号。</summary>
        public int TemplateId { get; }

        /// <summary>当前预览对应的镜像/预设模式。</summary>
        public CombatBodyFrontMode Mode { get; }

        /// <summary>当前预览纹理。</summary>
        public RenderTexture Texture
        {
            get { return portrait?.Texture; }
        }

        /// <summary>原版 Pawn 肖像快照；内部持有 Unity 资源释放逻辑。</summary>
        private readonly AssemblyPawnPortraitSnapshot portrait;

        /// <summary>建立一次预览状态。</summary>
        internal CombatBodyAppearancePreviewState(
            int templateId,
            CombatBodyFrontMode mode,
            AssemblyPawnPortraitSnapshot portrait)
        {
            TemplateId = templateId;
            Mode = mode;
            this.portrait = portrait;
        }

        /// <summary>释放当前预览持有的 RenderTexture。</summary>
        public void DestroyResources()
        {
            portrait?.DestroyResources();
        }
    }
}
