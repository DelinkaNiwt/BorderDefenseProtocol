using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Requirements;
using RimWorld;
using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 单个 Trigger 会话内的 Combo 受阻提示锁存器。
    /// 每次从可用进入受阻只提示一次，恢复或消失时静默清除记录。
    /// </summary>
    internal sealed class ComboUseRequirementNoticeTracker
    {
        /// <summary>当前已经提示过且仍处于受阻阶段的 ComboDefName。</summary>
        private readonly HashSet<string> blockedComboDefs = new HashSet<string>();

        /// <summary>按最新正式快照同步受阻集合，并提示本轮新出现的受阻 Combo。</summary>
        internal void Sync(Pawn pawn, ExpressionSnapshot snapshot)
        {
            HashSet<string> currentBlocked = new HashSet<string>();
            IReadOnlyList<FormalExpressionResult> results = snapshot?.Results;
            if (results != null)
            {
                for (int i = 0; i < results.Count; i++)
                {
                    FormalExpressionResult result = results[i];
                    PawnRequirementCheckResult check = result?.UseRequirementCheck;
                    if (result == null
                        || string.IsNullOrWhiteSpace(result.ComboDefName)
                        || check == null
                        || check.Satisfied
                        || !currentBlocked.Add(result.ComboDefName))
                    {
                        continue;
                    }

                    if (blockedComboDefs.Add(result.ComboDefName))
                    {
                        ShowBlockedMessage(pawn, result.ComboDefName, check);
                    }
                }
            }

            List<string> cleared = new List<string>();
            foreach (string comboDefName in blockedComboDefs)
            {
                if (!currentBlocked.Contains(comboDefName))
                {
                    cleared.Add(comboDefName);
                }
            }

            for (int i = 0; i < cleared.Count; i++)
            {
                blockedComboDefs.Remove(cleared[i]);
            }
        }

        /// <summary>用黄色注意消息展示 Combo 名称和全部失败原因。</summary>
        private static void ShowBlockedMessage(
            Pawn pawn,
            string comboDefName,
            PawnRequirementCheckResult check)
        {
            if (pawn == null || check?.Failures == null)
            {
                return;
            }

            ComboDef comboDef = DefDatabase<ComboDef>.GetNamedSilentFail(comboDefName);
            string label = comboDef != null && !string.IsNullOrWhiteSpace(comboDef.label)
                ? comboDef.LabelCap.ToString()
                : comboDefName;
            string failures = ComboUseRequirementService.Instance.BuildFailureText(check);
            if (string.IsNullOrWhiteSpace(failures))
            {
                failures = "BDP_Message_Combo_RequirementsFailure".Translate();
            }

            Messages.Message(
                "BDP_Message_Combo_UseRejected".Translate(label, failures),
                pawn,
                MessageTypeDefOf.CautionInput,
                false);
        }
    }
}
