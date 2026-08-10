using UnityEngine;

namespace BDP.Core.Trion.External
{
    /// <summary>
    /// Trion 状态卡扩展徽标数据。
    /// </summary>
    public sealed class TrionGizmoExtensionBadge
    {
        /// <summary>
        /// 初始化扩展徽标。
        /// </summary>
        public TrionGizmoExtensionBadge(Texture2D icon, string tooltip, Color tint, string text = null, string glyphKey = null)
        {
            Icon = icon;
            Tooltip = tooltip ?? string.Empty;
            Tint = tint;
            Text = text;
            GlyphKey = glyphKey;
        }

        /// <summary>
        /// 徽标贴图。
        /// </summary>
        public Texture2D Icon { get; }

        /// <summary>
        /// 徽标提示文本。
        /// </summary>
        public string Tooltip { get; }

        /// <summary>
        /// 徽标绘制色。
        /// </summary>
        public Color Tint { get; }

        /// <summary>
        /// 无贴图时的备用标识文本。
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// 无贴图时的示意图形键。
        /// </summary>
        public string GlyphKey { get; }
    }
}
