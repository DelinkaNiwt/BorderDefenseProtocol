#if BDP_RIMWORLD_1_7
using Brutal.ImGuiApi;
using UnityEngine;

namespace BDP.Development.UI.ImGuiShowcase
{
    /// <summary>仅在展示窗口存活时，把原版兼容事件转交原生输入队列；不添加全局输入补丁。</summary>
    internal sealed class ImGuiShowcaseInput
    {
        /// <summary>由本窗口接管的鼠标键，保证拖出窗口后仍能收到释放。</summary>
        private int heldButtons;

        /// <summary>游戏兼容派发器把一格滚轮转换为三个事件单位；此处按同一规则还原。</summary>
        private const float ScrollUnitsPerNotch = 3f;

        /// <summary>输入阶段在原生 NewFrame 之前运行；重绘阶段也更新位置以支持无按键悬停。</summary>
        internal void Forward(Event current, Rect localBounds, Vector2 physicalPoint, bool acceptsInput)
        {
            var io = ImGui.GetIO();
            if (!acceptsInput)
            {
                Release();
                io.AddMousePosEvent(-float.MaxValue, -float.MaxValue);
                return;
            }

            io.AddMousePosEvent(physicalPoint.x, physicalPoint.y);
            bool overContent = localBounds.Contains(current.mousePosition);
            bool ownsMouse = overContent || heldButtons != 0 || io.WantCaptureMouse;
            if (!ownsMouse) return;

            if ((current.type == EventType.MouseDown || current.type == EventType.MouseUp)
                && current.button >= 0 && current.button < 3)
            {
                bool down = current.type == EventType.MouseDown;
                int mask = 1 << current.button;
                heldButtons = down ? heldButtons | mask : heldButtons & ~mask;
                io.AddMouseButtonEvent(current.button, down);
                // 不让外壳的 GUI.DragWindow 再次处理同一个原生控件点击。
                current.Use();
            }
            else if (current.type == EventType.ScrollWheel)
            {
                io.AddMouseWheelEvent(-current.delta.x / ScrollUnitsPerNotch, -current.delta.y / ScrollUnitsPerNotch);
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && heldButtons != 0)
            {
                current.Use();
            }
        }

        /// <summary>关闭或失去输入权时只释放本展示接管的键，避免遗留按住状态。</summary>
        internal void Release()
        {
            if (!ImGui.GetCurrentContext().IsNull())
            {
                var io = ImGui.GetIO();
                for (int button = 0; button < 3; button++)
                    if ((heldButtons & (1 << button)) != 0) io.AddMouseButtonEvent(button, false);
            }
            heldButtons = 0;
        }
    }
}
#endif
