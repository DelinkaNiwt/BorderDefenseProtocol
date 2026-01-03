using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompReadDiaLog_Nested : CompUsable
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
		CompProperties_ReadDiaLog_Nested compProperties_ReadDiaLog_Nested = (CompProperties_ReadDiaLog_Nested)props;
		List<CustomDiaLog_Nested> list = new List<CustomDiaLog_Nested>();
		foreach (CustomDiaLog_Nested item in compProperties_ReadDiaLog_Nested.customDiaLog)
		{
			list.Add(item);
		}
		OpenNestedDialog(0, list);
	}

	public void OpenNestedDialog(int dialogNumber, List<CustomDiaLog_Nested> dialogs)
	{
		if (dialogNumber >= dialogs.Count)
		{
			AncotUtility_Dialog.CloseDialog();
			return;
		}
		CustomDiaLog_Nested customDiaLog_Nested = dialogs[dialogNumber];
		DiaOption diaOption = new DiaOption(customDiaLog_Nested.optionText);
		diaOption.action = delegate
		{
			OpenNestedDialog(dialogNumber + 1, dialogs);
		};
		DiaOption diaOption2 = new DiaOption("Close".Translate());
		diaOption2.action = delegate
		{
			AncotUtility_Dialog.CloseDialog();
		};
		DiaNode diaNode = new DiaNode(customDiaLog_Nested.content);
		diaNode.options.Add(diaOption);
		if (dialogNumber + 1 < dialogs.Count)
		{
			diaNode.options.Add(diaOption2);
		}
		Find.WindowStack.Add(new Dialog_NodeTree(diaNode));
	}
}
