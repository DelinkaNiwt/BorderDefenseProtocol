using System.Collections.Generic;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_IsResearchFinished : QuestNode
{
	[NoTranslate]
	public SlateRef<List<ResearchProjectDef>> researchProjectDefs;

	public SlateRef<bool> invert = false;

	protected override bool TestRunInt(Slate slate)
	{
		List<ResearchProjectDef> value = researchProjectDefs.GetValue(slate);
		if (invert.GetValue(slate))
		{
			foreach (ResearchProjectDef item in value)
			{
				if (item.IsFinished)
				{
					return false;
				}
			}
		}
		else
		{
			foreach (ResearchProjectDef item2 in value)
			{
				if (!item2.IsFinished)
				{
					return false;
				}
			}
		}
		Log.Message("QuestNode_ResearchFinished");
		return true;
	}

	protected override void RunInt()
	{
	}
}
