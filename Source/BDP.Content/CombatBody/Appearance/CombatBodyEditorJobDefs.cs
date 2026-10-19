using Verse;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 战斗体编辑台入口使用的 JobDef 读取口。
    /// </summary>
    internal static class CombatBodyEditorJobDefs
    {
        /// <summary>
        /// 使用战斗体编辑台的 JobDef 名称。
        /// </summary>
        private const string UseCombatBodyEditorDefName =
            "BDP_UseCombatBodyAppearanceEditor";

        /// <summary>
        /// 读取使用战斗体编辑台的 JobDef。
        /// </summary>
        internal static JobDef UseCombatBodyEditor
        {
            get
            {
                return DefDatabase<JobDef>.GetNamedSilentFail(UseCombatBodyEditorDefName);
            }
        }
    }
}
