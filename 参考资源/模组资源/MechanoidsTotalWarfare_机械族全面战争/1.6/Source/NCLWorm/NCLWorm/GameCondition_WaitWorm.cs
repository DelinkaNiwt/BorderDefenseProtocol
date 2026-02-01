using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NCLWorm;

public class GameCondition_WaitWorm : GameCondition
{
	public override void End()
	{
		base.End();
		base.SingleMap.mapPawns.AllPawnsSpawned.Where((Pawn x) => x.def.defName == "NCL_MechWorm").RandomElement()?.DeSpawn(DestroyMode.Refund);
		PawnKindDef named = DefDatabase<PawnKindDef>.GetNamed("NCL_MechWorm");
		Pawn pawn = PawnGenerator.GeneratePawn(named, Faction.OfPlayer);
		List<Thing> things = new List<Thing> { pawn };
		IntVec3 dropCenter = DropCellFinder.RandomDropSpot(base.SingleMap);
		DropPodUtility.DropThingsNear(dropCenter, base.SingleMap, things);
		ChoiceLetter choiceLetter = LetterMaker.MakeLetter(def.endMessage, def.letterText, LetterDefOf.NeutralEvent, pawn);
		Find.LetterStack.ReceiveLetter(choiceLetter);
	}
}
