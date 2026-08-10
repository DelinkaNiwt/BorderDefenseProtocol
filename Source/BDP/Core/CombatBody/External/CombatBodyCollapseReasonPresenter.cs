using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体崩解原因的玩家展示映射。
    /// 它只负责把内部原因码翻译成玩家可读文本，不承载任何业务判断。
    /// </summary>
    internal static class CombatBodyCollapseReasonPresenter
    {
        /// <summary>
        /// 把内部崩解原因码转成玩家可读文本。
        /// </summary>
        public static string Describe(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "BDP_Message_CombatBody_CollapseReasonUnknown".Translate();
            }

            switch (reason)
            {
                case "TrionAvailableDepleted":
                    return "BDP_Message_CombatBody_CollapseReasonTrionDepleted".Translate();
                default:
                    return "BDP_Message_CombatBody_CollapseReasonOther".Translate(reason);
            }
        }
    }
}
