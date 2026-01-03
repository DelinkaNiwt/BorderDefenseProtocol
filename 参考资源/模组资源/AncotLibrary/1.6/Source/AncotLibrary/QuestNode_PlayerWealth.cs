using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_PlayerWealth : QuestNode
{
	public SlateRef<FloatRange> wealthRange = new FloatRange(0f, 100000f);

	protected override bool TestRunInt(Slate slate)
	{
		float playerWealth = WealthUtility.PlayerWealth;
		return playerWealth > wealthRange.GetValue(slate).min && playerWealth < wealthRange.GetValue(slate).max;
	}

	protected override void RunInt()
	{
	}
}
