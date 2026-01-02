using RimWorld;
using Verse;

namespace Milira;

public class ThoughtWorker_MiliraWearingBloodStained : ThoughtWorker
{
	protected override ThoughtState CurrentStateInternal(Pawn p)
	{
		string text = null;
		int num = 0;
		foreach (Apparel item in p.apparel.WornApparel)
		{
			if (item.def.MadeFromStuff && item.Stuff != null && item.Stuff.defName.Equals("Milira_BloodStainedFeather"))
			{
				if (text == null)
				{
					text = item.def.label;
				}
				num++;
			}
		}
		return (num >= 2) ? ThoughtState.ActiveAtStage(2, text) : ThoughtState.ActiveAtStage(num - 1, text);
	}
}
