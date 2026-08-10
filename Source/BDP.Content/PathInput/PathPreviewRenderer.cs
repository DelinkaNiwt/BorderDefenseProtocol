using BDP.Core.AttackExecution;
using BDP.Core.PathInput;
using UnityEngine;
using Verse;

namespace BDP.Content.PathInput
{
    /// <summary>
    /// 路径预览渲染器 — 把 PathPreviewData 的中性线段数据落成实际绘制。
    ///
    /// 提供两种渲染模式：
    ///   1. Immediate：直接调用 GenDraw 画线/标记（用于世界空间渲染上下文，如 DrawHighlight）
    ///   2. Deferred：产出 PreviewDrawItem 追加到 PreviewRecord（用于 IMGUI 上下文，如 IPreviewStageModule）
    ///
    /// 毒蛇（IMGUI 阶段 → Deferred）与蚱蜢（世界空间 → Immediate）使用不同模式，
    /// 但共享同一套线段生成逻辑，消除重复。
    /// </summary>
    public static class PathPreviewRenderer
    {
        /// <summary>
        /// Immediate 模式：在世界空间直接绘制路径预览（默认白色线段）。
        /// 必须在 RimWorld 渲染阶段（DrawHighlight 等）调用，严禁在 OnGUI/IMGUI 中调用。
        /// </summary>
        public static void DrawPreview(PathPreviewData preview, BDP.Core.PathInput.PathInputState state)
        {
            DrawPreview(preview, state, SimpleColor.White);
        }

        /// <summary>
        /// Immediate 模式：在世界空间直接绘制路径预览（自定义线段颜色）。
        /// </summary>
        public static void DrawPreview(PathPreviewData preview, BDP.Core.PathInput.PathInputState state, SimpleColor lineColor)
        {
            if (preview == null) return;

            DrawSegments(preview, lineColor);
            DrawAnchorMarkers(state);
        }

        /// <summary>
        /// Deferred 模式：把预览数据转为 PreviewDrawItem 追加到 PreviewRecord。
        /// 用于 IMGUI 阶段（IPreviewStageModule.Contribute），由 BDP 管线在渲染时落成。
        /// </summary>
        public static void AppendToRecord(PreviewRecord record, PathPreviewData preview)
        {
            if (record == null || preview == null) return;

            for (int i = 0; i < preview.Segments.Count; i++)
            {
                var seg = preview.Segments[i];
                Color color = seg.isBlocked ? Color.red : Color.white;
                record.DrawItems.Add(new PreviewDrawItem
                {
                    Kind = PreviewDrawItemKind.Line,
                    Start = seg.from,
                    End = seg.to,
                    Color = color
                });
            }
        }

        private static void DrawSegments(PathPreviewData preview, SimpleColor defaultColor)
        {
            for (int i = 0; i < preview.Segments.Count; i++)
            {
                var seg = preview.Segments[i];
                SimpleColor color = seg.isBlocked ? SimpleColor.Red : defaultColor;
                GenDraw.DrawLineBetween(seg.from, seg.to, color);
            }
        }

        private static void DrawAnchorMarkers(BDP.Core.PathInput.PathInputState state)
        {
            if (state == null || state.Anchors == null) return;

            for (int i = 0; i < state.Anchors.Count; i++)
            {
                BDP.Core.PathInput.PathAnchor anchor = state.Anchors[i];
                if (anchor != null)
                {
                    GenDraw.DrawTargetHighlight(new LocalTargetInfo(anchor.ToCell()));
                }
            }
        }
    }
}
