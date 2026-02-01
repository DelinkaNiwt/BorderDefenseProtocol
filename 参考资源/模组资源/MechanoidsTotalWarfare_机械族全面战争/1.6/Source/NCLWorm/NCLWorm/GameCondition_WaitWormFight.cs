using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NCLWorm;

public class GameCondition_WaitWormFight : GameCondition
{
	public override void End()
	{
		base.End();
		base.SingleMap.mapPawns.AllPawnsSpawned.Where((Pawn x) => x.def.defName == "NCL_MechWorm").RandomElement()?.DeSpawn(DestroyMode.Refund);
		PawnKindDef named = DefDatabase<PawnKindDef>.GetNamed("NCL_MechWorm");
		Pawn pawn = PawnGenerator.GeneratePawn(named, Find.FactionManager.FirstFactionOfDef(NCLWormDefOf.NCL_factionEnemy));
		pawn.SetFaction(Find.FactionManager.FirstFactionOfDef(NCLWormDefOf.NCL_factionEnemy));
		List<Thing> things = new List<Thing> { pawn };
		IntVec3 dropCenter = DropCellFinder.FindRaidDropCenterDistant(base.SingleMap);
		DropPodUtility.DropThingsNear(dropCenter, base.SingleMap, things);
		ChoiceLetter choiceLetter = LetterMaker.MakeLetter(def.endMessage, def.letterText, LetterDefOf.ThreatBig, pawn);
		Find.LetterStack.ReceiveLetter(choiceLetter);
		Current.Game.GetComponent<GameComp_NCLWorm>().inWormWar = true;
		base.SingleMap.weatherManager.curWeather = DefDatabase<WeatherDef>.GetNamed("DryThunderstorm");
		IncidentDef eclipse = IncidentDefOf.Eclipse;
		IncidentParms parms = StorytellerUtility.DefaultParmsNow(eclipse.category, base.SingleMap);
		eclipse.durationDays.min = 5f;
		eclipse.durationDays.max = 5f;
		eclipse.Worker.TryExecute(parms);
	}
}
