#if BDP_RIMWORLD_1_7
using Brutal.ImGuiApi;
using Brutal.Numerics;
using UnityEngine;
using UnityStubs.Gui;
using Verse;

namespace BDP.Development.UI.ImGuiShowcase
{
    /// <summary>原版窗口仅管理打开、关闭与输入遮挡；内容由原生 ImGui 绘制。</summary>
    public sealed class Window_ImGuiShowcase : Window
    {
        /// <summary>全部示例状态仅存于本窗口，关闭即可丢弃。</summary>
        private readonly ImGuiShowcaseContent content = new ImGuiShowcaseContent();
        /// <summary>兼容事件不会自动填充原生 IO；输入转交和生命周期均局限于本窗口。</summary>
        private readonly ImGuiShowcaseInput input = new ImGuiShowcaseInput();
        /// <summary>原版多次界面事件不得在同一原生帧重复创建控件。</summary>
        private int lastFrame = -1;

        /// <summary>复用原版可关闭窗口，暂停地图但不暂停界面动画。</summary>
        public Window_ImGuiShowcase()
        {
            forcePause = true;
            absorbInputAroundWindow = true;
            doCloseX = true;
            closeOnClickedOutside = false;
            draggable = true;
        }

        /// <summary>优先提供宽阔展示区域，原版自动约束到当前屏幕。</summary>
        public override Vector2 InitialSize { get { return new Vector2(1120f, 800f); } }

        /// <summary>先转交输入事件，再仅于重绘时绘制；不另建上下文或接管游戏帧。</summary>
        public override void DoWindowContents(Rect inRect)
        {
            if (Event.current == null || ImGui.GetCurrentContext().IsNull()) return;
            float scale = Prefs.UIScale;
            Vector2 physicalMouse = Verse.UI.GUIToScreenPoint(Event.current.mousePosition) * scale;
            input.Forward(Event.current, inRect, physicalMouse, Find.WindowStack.GetsInput(this));
            if (Event.current.type != EventType.Repaint || GuiBackend.Current == null) return;
            int frame = ImGui.GetFrameCount();
            if (lastFrame == frame) return;
            lastFrame = frame;
            Vector2 origin = Verse.UI.GUIToScreenPoint(inRect.position) * scale;
            ImGui.SetNextWindowPos(new float2(origin.x, origin.y));
            ImGui.SetNextWindowSize(new float2(inRect.width * scale, inRect.height * scale));
            // 与原版壳窗口位置绑定；不保存独立布局，不使用前景列表破坏其他窗口裁切。
            bool visible = ImGui.Begin("##BDP_Dev_ImGui_Showcase", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings);
            try
            {
                if (!visible) return;
                ImGui.PushFont(ImGui.GetFont(), 17f * scale);
                try { content.Draw(scale); }
                finally { ImGui.PopFont(); }
            }
            finally { ImGui.End(); }
        }

        /// <summary>关闭窗口也应释放仍被接管的鼠标键。</summary>
        public override void PostClose()
        {
            input.Release();
            base.PostClose();
        }
    }
}
#endif
