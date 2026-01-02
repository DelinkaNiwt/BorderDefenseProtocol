using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompReadDiaLog : CompUsable
{
	public override void PostExposeData()
	{
		base.PostExposeData();
	}

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
	}

	public override void UsedBy(Pawn pawn)
	{
		base.UsedBy(pawn);
		CompProperties_ReadDiaLog props = (CompProperties_ReadDiaLog)base.props;
		DiaOption diaOption = new DiaOption(props.dialogOptionText);
		diaOption.action = delegate
		{
			Find.LetterStack.ReceiveLetter(props.letterLabel, props.letterText, props.letterDef);
		};
		DiaOption diaOption2 = new DiaOption("Close".Translate());
		diaOption2.action = delegate
		{
			Find.WindowStack.TryRemove(typeof(Dialog_NodeTree));
		};
		DiaNode diaNode = new DiaNode(props.dialogContent);
		diaNode.options.Add(diaOption);
		diaNode.options.Add(diaOption2);
		Find.WindowStack.Add(new Dialog_NodeTree(diaNode));
		if (props.oneUse)
		{
			parent.Destroy();
		}
	}
}
