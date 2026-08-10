using UnityEngine;
using Verse;

namespace BDP.Core.Trion.External
{
    /// <summary>
    /// Trion 状态卡右侧面板扩展提供器。
    /// 它只负责向 Trion Gizmo 提供可选的右侧绘制面板，不承载 Trion 结算或业务命令。
    /// </summary>
    public interface ITrionGizmoPanelExtensionProvider
    {
        /// <summary>
        /// 返回当前上下文需要的面板宽度。
        /// 返回 0 或负数表示当前不显示面板。
        /// </summary>
        float GetWidth(TrionGizmoExtensionContext context);

        /// <summary>
        /// 在 Trion Gizmo 分配的右侧区域内绘制面板并处理输入。
        /// </summary>
        GizmoResult DrawPanel(
            TrionGizmoExtensionContext context,
            Rect panelRect,
            GizmoRenderParms parms);
    }
}
