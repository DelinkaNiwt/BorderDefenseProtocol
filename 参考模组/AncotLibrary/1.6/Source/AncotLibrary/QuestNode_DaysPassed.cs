using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_DaysPassed : QuestNode
{
	public SlateRef<FloatRange> dayRange = new FloatRange(0f, 5f);

	protected override bool TestRunInt(Slate slate)
	{
		return (float)GenDate.DaysPassed >= dayRange.GetValue(slate).min && (float)GenDate.DaysPassed <= dayRange.GetValue(slate).max;
	}

	protected override void RunInt()
	{
	}
}
