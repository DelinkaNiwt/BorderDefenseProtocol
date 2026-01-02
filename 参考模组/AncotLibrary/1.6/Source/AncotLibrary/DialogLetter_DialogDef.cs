using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class DialogLetter_DialogDef : ChoiceLetter
{
	public string outSignal;

	public string optionSignalA;

	public string optionSignalB;

	public string optionSignalC;

	public string optionSignalD;

	public virtual Window dialog { get; set; }

	public virtual string diaOption_StartDialog => def.label;

	public virtual DialogDef dialogDef { get; set; }

	public override IEnumerable<DiaOption> Choices
	{
		get
		{
			if (base.ArchivedOnly)
			{
				yield return base.Option_Close;
				yield break;
			}
			yield return new DiaOption(diaOption_StartDialog)
			{
				action = delegate
				{
					DialogUtility.OpenAssembledDialog(dialogDef, optionSignalA, optionSignalB, optionSignalC, optionSignalD, outSignal);
					Find.LetterStack.RemoveLetter(this);
				},
				resolveTree = true
			};
			if (lookTargets.IsValid())
			{
				yield return base.Option_JumpToLocationAndPostpone;
			}
			yield return base.Option_Postpone;
		}
	}

	public DialogLetter_DialogDef()
	{
	}

	public DialogLetter_DialogDef(Window dialog)
	{
		this.dialog = dialog;
	}
}
