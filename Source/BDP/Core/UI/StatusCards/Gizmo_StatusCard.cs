using System;
using UnityEngine;
using Verse;

namespace BDP.Core.UI.StatusCards
{
    /// <summary>标题与单条数值的通用只读状态卡；可由任何原版界面项入口返回。</summary>
    public sealed class Gizmo_StatusCard : Gizmo
    {
        /// <summary>紧凑状态卡宽度。</summary>
        private const float CardWidth = 140f;

        /// <summary>沿用原版操作按钮高度。</summary>
        private const float CardHeight = 75f;

        /// <summary>卡片四周留白。</summary>
        private const float Padding = 6f;

        /// <summary>每次绘制时读取显示数据，防止卡片缓存让数值和倒计时停留在创建时。</summary>
        private readonly Func<StatusCardSnapshot> readSnapshot;

        /// <summary>绑定只读取值函数；调用方通过继承自原版的 Order（排序）决定卡片位置。</summary>
        public Gizmo_StatusCard(Func<StatusCardSnapshot> readSnapshot)
        {
            this.readSnapshot = readSnapshot ?? throw new ArgumentNullException(nameof(readSnapshot));
            Order = -99f;
        }

        /// <summary>返回固定宽度，与参考状态卡保持相同尺寸。</summary>
        public override float GetWidth(float maxWidth) { return CardWidth; }

        /// <summary>绘制一份实时快照；返回空表示暂时无内容，显隐枚举仍由业务入口控制。</summary>
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            StatusCardSnapshot snapshot = readSnapshot();
            if (snapshot == null) return new GizmoResult(GizmoState.Clear);

            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;
            Color previousColor = GUI.color;
            try
            {
                GUI.color = Color.white;
                Rect card = new Rect(topLeft.x, topLeft.y, CardWidth, CardHeight);
                Rect inner = card.ContractedBy(Padding);
                Rect title = new Rect(inner.x, inner.y, inner.width, CardHeight * 0.5f - Padding);
                Rect bar = new Rect(inner.x, card.y + CardHeight * 0.5f,
                    inner.width, CardHeight * 0.5f - Padding);

                Widgets.DrawWindowBackground(card);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(title, snapshot.Title);

                // 直接画纯色条，避免每帧创建纹理，也避免多卡片共用固定即时窗口编号。
                Widgets.DrawBoxSolid(bar, Color.black);
                if (snapshot.FillFraction > 0f)
                    Widgets.DrawBoxSolid(new Rect(bar.x, bar.y, bar.width * snapshot.FillFraction, bar.height),
                        snapshot.FillColor);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(bar, snapshot.ValueText);
                if (!string.IsNullOrEmpty(snapshot.Tooltip))
                    TooltipHandler.TipRegion(card, snapshot.Tooltip);
            }
            finally
            {
                Text.Font = previousFont;
                Text.Anchor = previousAnchor;
                GUI.color = previousColor;
            }
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
