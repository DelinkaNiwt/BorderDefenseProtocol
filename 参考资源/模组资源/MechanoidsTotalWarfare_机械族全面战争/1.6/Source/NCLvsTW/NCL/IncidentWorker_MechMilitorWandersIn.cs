using RimWorld;
using Verse;

namespace NCL;

public class IncidentWorker_MechMilitorWandersIn : IncidentWorker
{
	protected override bool CanFireNowSub(IncidentParms parms)
	{
		if (!base.CanFireNowSub(parms))
		{
			return false;
		}
		Map map = (Map)parms.target;
		IntVec3 intVec;
		return TryFindEntryCell(map, out intVec);
	}

	protected override bool TryExecuteWorker(IncidentParms parms)
	{
		Map map = (Map)parms.target;
		if (!TryFindEntryCell(map, out var loc))
		{
			return false;
		}
		PawnKindDef mechKind = DefDatabase<PawnKindDef>.GetNamed("TW_Mech_Capitalistic_Militor");
		if (mechKind == null)
		{
			Log.Error("[Mod] TW_Mech_Capitalistic_Militor PawnKindDef not found!");
			return false;
		}
		PawnGenerationRequest request = new PawnGenerationRequest(mechKind, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true, 0f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: false, allowAddictions: false, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: true);
		Pawn pawn = PawnGenerator.GeneratePawn(request);
		GenSpawn.Spawn(pawn, loc, map);
		string unitName = pawn.KindLabel;
		TaggedString baseLetterLabel = def.letterLabel.Formatted(unitName, pawn.Named("PAWN")).CapitalizeFirst();
		TaggedString baseLetterText = def.letterText.Formatted(pawn.NameShortColored, unitName, pawn.Named("PAWN")).CapitalizeFirst();
		SendStandardLetter(baseLetterLabel, baseLetterText, def.letterDef, parms, pawn);
		return true;
	}

	private bool TryFindEntryCell(Map map, out IntVec3 cell)
	{
		return CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => map.reachability.CanReachColony(c), map, CellFinder.EdgeRoadChance_Neutral, out cell);
	}
}
