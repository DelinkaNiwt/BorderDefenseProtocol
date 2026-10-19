using System;
using UnityEngine;

namespace BDP.Core.UI.StatusCards
{
    /// <summary>通用只读状态卡的显示数据；业务负责取值、本地化和决定是否显示。</summary>
    public sealed class StatusCardSnapshot
    {
        /// <summary>已本地化的标题。</summary>
        public string Title { get; }

        /// <summary>数值条中央的已本地化正文，可表达资源量、进度或倒计时。</summary>
        public string ValueText { get; }

        /// <summary>填充比例，始终限定在零到一之间。</summary>
        public float FillFraction { get; }

        /// <summary>数值条填充颜色。</summary>
        public Color FillColor { get; }

        /// <summary>可选的已本地化悬停说明。</summary>
        public string Tooltip { get; }

        /// <summary>复制显示数据；不持有业务对象，不承担计时、存档或状态变更。</summary>
        public StatusCardSnapshot(string title, string valueText, float fillFraction, Color fillColor,
            string tooltip = null)
        {
            Title = title ?? string.Empty;
            ValueText = valueText ?? string.Empty;
            FillFraction = float.IsNaN(fillFraction) ? 0f : Math.Max(0f, Math.Min(1f, fillFraction));
            FillColor = fillColor;
            Tooltip = tooltip;
        }
    }
}
