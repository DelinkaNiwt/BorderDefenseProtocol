#if BDP_RIMWORLD_1_7
using LudeonTK;
using Verse;

namespace BDP.Development.UI.ImGuiShowcase
{
    /// <summary>独立能力展示入口；不注册业务服务，不修改人物、地图或存档。</summary>
    public static class ImGuiShowcaseDebugAction
    {
        /// <summary>使用原版开发菜单的本地化动作节点打开窗口。</summary>
        [DebugAction("BDP", allowedGameStates = AllowedGameStates.Playing)]
        public static DebugActionNode OpenShowcase()
        {
            return new DebugActionNode("BDP_Dev_ImGui_Open".Translate(), action: () =>
            {
                if (!Find.WindowStack.IsOpen<Window_ImGuiShowcase>())
                    Find.WindowStack.Add(new Window_ImGuiShowcase());
            });
        }
    }
}
#endif
