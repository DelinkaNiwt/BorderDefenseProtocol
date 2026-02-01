using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace GD3
{
    public class ChoiceLetter_SavingMech : ChoiceLetter
    {
        public string signalReject;

        public string outSignal;

        public TaggedString subLetterText;

        public override bool CanDismissWithRightClick => false;

        public override bool CanShowInLetterStack
        {
            get
            {
                return base.CanShowInLetterStack;
            }
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (base.ArchivedOnly)
                {
                    yield return base.Option_Close;
                    yield break;
                }

                DiaOption diaOption = new DiaOption("Yes".Translate());
                DiaOption optionReject = new DiaOption("No".Translate());
                diaOption.action = delegate
                {
                    DiaNode diaNode = new DiaNode(subLetterText);
                    diaNode.options.Add(new DiaOption("Close".Translate())
                    {
                        resolveTree = true
                    });
                    Dialog_NodeTree window = new Dialog_NodeTree(diaNode, false, radioMode, title);
                    Find.WindowStack.Add(window);

                    Find.SignalManager.SendSignal(new Signal(outSignal));
                    Find.LetterStack.RemoveLetter(this);
                };
                diaOption.resolveTree = true;
                optionReject.action = delegate
                {
                    Find.SignalManager.SendSignal(new Signal(signalReject));
                    Find.SignalManager.SendSignal(new Signal(outSignal));
                    Find.LetterStack.RemoveLetter(this);
                };
                optionReject.resolveTree = true;

                yield return diaOption;
                yield return optionReject;
                if (lookTargets.IsValid())
                {
                    yield return base.Option_JumpToLocationAndPostpone;
                }

                yield return base.Option_Postpone;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref signalReject, "signalReject");
            Scribe_Values.Look(ref outSignal, "outSignal");
            Scribe_Values.Look(ref subLetterText, "subLetterText");
        }
    }
}
