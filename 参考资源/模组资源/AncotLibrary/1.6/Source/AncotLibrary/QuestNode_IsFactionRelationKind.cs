using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_IsFactionRelationKind : QuestNode
{
	[NoTranslate]
	public SlateRef<List<FactionDef>> factionDefs;

	[NoTranslate]
	public SlateRef<FactionRelationKind> factionRelationKind;

	[NoTranslate]
	public SlateRef<bool> invert = false;

	protected override bool TestRunInt(Slate slate)
	{
		List<FactionDef> value = factionDefs.GetValue(slate);
		foreach (FactionDef item in value)
		{
			Faction faction = Find.FactionManager.FirstFactionOfDef(item);
			if (faction == null)
			{
				return false;
			}
			if (invert.GetValue(slate))
			{
				if (faction.RelationKindWith(Faction.OfPlayer) == factionRelationKind.GetValue(slate))
				{
					return false;
				}
			}
			else if (faction.RelationKindWith(Faction.OfPlayer) != factionRelationKind.GetValue(slate))
			{
				return false;
			}
		}
		return true;
	}

	protected override void RunInt()
	{
	}
}
