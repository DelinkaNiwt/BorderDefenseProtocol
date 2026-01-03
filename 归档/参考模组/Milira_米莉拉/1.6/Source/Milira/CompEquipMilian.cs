using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class CompEquipMilian : CompUsable
{
	public CompProperties_EquipMilian Props_EquipMilian => (CompProperties_EquipMilian)props;

	public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
	{
		Pawn milian = parent as Pawn;
		if (!selPawn.RaceProps.ToolUser || !milian.IsColonyMechPlayerControlled || !selPawn.CanReserveAndReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
		{
			yield break;
		}
		foreach (FloatMenuOption item in base.CompFloatMenuOptions(selPawn))
		{
			yield return item;
		}
	}

	protected override string FloatMenuOptionLabel(Pawn pawn)
	{
		return "Milira_EquipMilian".Translate().Formatted(parent);
	}
}
