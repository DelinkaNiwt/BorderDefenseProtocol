using BDP.Core.CombatBody;
using UnityEngine;
using Verse;

namespace BDP.Core.Hediffs
{
    /// <summary>
    /// 战斗体延时崩解显示 `Hediff`。
    /// 它只负责在 90 tick 崩解表现期向玩家显示倒计时与原因，不承载业务真值。
    /// </summary>
    public sealed class Hediff_BdpCombatBodyCollapsePending : HediffWithComps
    {
        /// <summary>
        /// 健康页中显示的基础名称。
        /// 这里直接把剩余时间拼进标签，保证玩家一眼就能看到倒计时。
        /// </summary>
        public override string LabelBase
        {
            get
            {
                ICombatBodyReader reader = ResolveCombatBodyReader();
                int remainingTicks = reader != null ? reader.GetCollapseRemaining() : 0;
                return "BDP_Hediff_CombatBody_CollapsePendingLabel".Translate(
                    FormatTicksAsSeconds(remainingTicks));
            }
        }

        /// <summary>
        /// 鼠标悬停时追加的提示文本。
        /// 这里显示当前剩余时间和本次直接崩解原因。
        /// </summary>
        public override string TipStringExtra
        {
            get
            {
                ICombatBodyReader reader = ResolveCombatBodyReader();
                int remainingTicks = reader != null ? reader.GetCollapseRemaining() : 0;
                string collapseReason = reader != null ? reader.CollapseReason : null;
                string reasonText = CombatBodyCollapseReasonPresenter.Describe(collapseReason);

                return "BDP_Hediff_CombatBody_CollapsePendingTip".Translate(
                    FormatTicksAsSeconds(remainingTicks),
                    reasonText);
            }
        }

        /// <summary>
        /// 解析当前宿主 Pawn 的 CombatBody 正式读口。
        /// </summary>
        private ICombatBodyReader ResolveCombatBodyReader()
        {
            return pawn != null ? CombatBodySurfaceAccess.ResolveReader(pawn) : null;
        }

        /// <summary>
        /// 把 tick 换算成秒文本。
        /// </summary>
        private static string FormatTicksAsSeconds(int ticks)
        {
            return "BDP_Tick_Seconds".Translate(
                (Mathf.Max(0, ticks) / 60f).ToString("F1"));
        }
    }
}
