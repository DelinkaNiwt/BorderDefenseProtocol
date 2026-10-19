using LudeonTK;
using Verse;

namespace BDP.Development.UI.Charts
{
    /// <summary>Development 图表工具视觉预览调试入口。</summary>
    public static class ChartToolsDebugActions
    {
        /// <summary>打开中性图表工具预览窗口。</summary>
        [DebugAction(
            "Mods",
            "BDP：打开中性图表工具预览",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing,
            displayPriority = -260)]
        public static void OpenPreview()
        {
            Find.WindowStack.Add(new Window_ChartToolsPreview());
        }
    }
}
