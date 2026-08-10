using BDP.Core.Projectiles.Visual;
using Verse;

namespace BDP.Content.Projectiles.BeamTrail
{
    /// <summary>
    /// 光束拖尾定义扩展。
    /// 任意投射物来源 Def 都可以通过它引用一个正式拖尾预设。
    /// </summary>
    public sealed class BeamTrailExtension : DefModExtension, IProjectileVisualAttachmentProvider
    {
        /// <summary>当前来源使用的拖尾预设。</summary>
        public BeamTrailPresetDef preset;

        /// <summary>
        /// 为本次投射物创建独立的拖尾视觉附加件。
        /// </summary>
        /// <returns>预设有效时返回附加件；否则返回空。</returns>
        public IProjectileVisualAttachment CreateAttachment()
        {
            if (preset == null)
            {
                Log.WarningOnce(
                    "[BDP.BeamTrail] 拖尾扩展缺少有效 preset（预设），本次投射物不会生成拖尾。",
                    GetHashCode());
                return null;
            }

            return new BeamTrailAttachment(BeamTrailAppearanceSnapshot.CreateFrom(preset));
        }
    }
}
