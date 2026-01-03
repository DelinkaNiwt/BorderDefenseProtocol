using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_Colonists : QuestNode
{
	public SlateRef<FloatRange> colonistRange = new FloatRange(0f, 5f);

	public SlateRef<ThingDef> race;

	protected override bool TestRunInt(Slate slate)
	{
		int count = Colonists(slate).Count;
		return (float)count >= colonistRange.GetValue(slate).min && (float)count <= colonistRange.GetValue(slate).max;
	}

	protected override void RunInt()
	{
	}

	public virtual List<Pawn> Colonists(Slate slate)
	{
		List<Pawn> allMaps_FreeColonists = PawnsFinder.AllMaps_FreeColonists;
		ThingDef raceDef = race.GetValue(slate);
		if (raceDef != null)
		{
			allMaps_FreeColonists.RemoveAll((Pawn p) => p.def != raceDef);
		}
		return allMaps_FreeColonists;
	}
}
