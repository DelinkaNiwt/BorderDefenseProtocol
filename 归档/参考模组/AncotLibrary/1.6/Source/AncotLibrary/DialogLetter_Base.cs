using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class DialogLetter_Base : ChoiceLetter
{
	public string outSignal;

	public virtual Window dialog { get; set; }

	public virtual string diaOption_StartDialog { get; set; }

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
					if (dialog != null)
					{
						Find.WindowStack.Add(dialog);
					}
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

	public DialogLetter_Base()
	{
	}

	public DialogLetter_Base(Window dialog)
	{
		this.dialog = dialog;
	}
}
