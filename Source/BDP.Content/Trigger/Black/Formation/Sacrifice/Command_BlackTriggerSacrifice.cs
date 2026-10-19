using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>个人危机中由玩家确认的主动燃尽生命命令。</summary>
    public static class Command_BlackTriggerSacrifice
    {
        /// <summary>复核危机后显示不可逆确认框。</summary>
        public static void TrySacrifice(Pawn pawn)
        {
            string reasonKey;
            if (!BlackTriggerPersonalCrisisEvaluator.IsCrisis(pawn, out reasonKey))
            {
                Messages.Message("BDP_BlackTrigger_Sacrifice_NoLongerAvailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "BDP_BlackTrigger_Sacrifice_Confirm".Translate(pawn.LabelShort),
                delegate { Execute(pawn); },
                destructive: true));
        }

        /// <summary>先确认有可形成结果，再执行不可逆死亡。</summary>
        public static void Execute(Pawn pawn)
        {
            string crisisReasonKey;
            if (!BlackTriggerPersonalCrisisEvaluator.IsCrisis(pawn, out crisisReasonKey))
            {
                Messages.Message("BDP_BlackTrigger_Sacrifice_NoLongerAvailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            string failureReasonKey;
            if (!BlackTriggerSacrificeService.TryPrepare(pawn, false, out failureReasonKey))
            {
                string key = failureReasonKey.NullOrEmpty() ? "BDP_BlackTrigger_Sacrifice_NoCompatibleTrigger" : failureReasonKey;
                Messages.Message(key.Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            try
            {
                pawn.Kill(null, null);
            }
            finally
            {
                BlackTriggerSacrificeService.EndContext(pawn);
            }
        }
    }
}
