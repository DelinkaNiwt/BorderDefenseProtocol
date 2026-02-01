using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.QuestGen;

namespace GD3
{
    public class QuestPart_ReceiveLetter : QuestPart
    {
        public string inSignal;

        public string signalRejected;

        public string outSignal;

        public TaggedString title;

        public TaggedString letterText;

        public TaggedString subLetterText;

        public int delayTick = 0;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                if (signalRejected != null && outSignal != null)
                {
                    ChoiceLetter_SavingMech choiceLetter_AcceptJoiner = (ChoiceLetter_SavingMech)LetterMaker.MakeLetter(title, letterText, GDDefOf.SavingMech);
                    choiceLetter_AcceptJoiner.signalReject = signalRejected;
                    choiceLetter_AcceptJoiner.outSignal = outSignal;
                    choiceLetter_AcceptJoiner.quest = quest;
                    choiceLetter_AcceptJoiner.subLetterText = subLetterText;
                    choiceLetter_AcceptJoiner.StartTimeout(60000);
                    Find.LetterStack.ReceiveLetter(choiceLetter_AcceptJoiner);
                }
                else
                {
                    Find.LetterStack.ReceiveLetter(title, letterText, LetterDefOf.NeutralEvent, null, delayTick);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref signalRejected, "signalRejected");
            Scribe_Values.Look(ref outSignal, "outSignal");
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref letterText, "letterText");
            Scribe_Values.Look(ref subLetterText, "subLetterText");
            Scribe_Values.Look(ref delayTick, "delayTick");
        }
    }
}