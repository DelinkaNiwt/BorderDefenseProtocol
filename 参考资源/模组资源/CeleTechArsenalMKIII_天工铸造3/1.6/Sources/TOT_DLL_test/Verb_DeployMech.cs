using RimWorld;
using Verse;
using Verse.AI.Group;

namespace TOT_DLL_test;

public class Verb_DeployMech : Verb
{
	protected override bool TryCastShot()
	{
		return Deploy(base.ReloadableCompSource);
	}

	public bool Deploy(CompApparelReloadable comp)
	{
		if (comp == null || !comp.CanBeUsed(out var _))
		{
			return false;
		}
		Pawn wearer = comp.Wearer;
		if (wearer == null)
		{
			return false;
		}
		Map map = wearer.Map;
		int num = GenRadial.NumCellsInRadius(4f);
		for (int i = 0; i < num; i++)
		{
			IntVec3 c = wearer.Position + GenRadial.RadialPattern[i];
			if (c.IsValid && c.InBounds(map) && c.GetFirstPawn(map) == null)
			{
				Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Mech_Warqueen, wearer.Faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: true, allowGay: true, allowPregnant: true, allowFood: false, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, 0f, 0f));
				Log.Message("1.2");
				Pawn p;
				Lord lord = (((p = wearer) != null) ? p.GetLord() : null);
				GenPlace.TryPlaceThing(pawn, wearer.Position, wearer.Map, ThingPlaceMode.Near, null, null, default(Rot4));
				Log.Message("1.3");
				lord?.AddPawn(pawn);
				comp.UsedOnce();
				Log.Message("1.4");
				return true;
			}
		}
		Messages.Message("AbilityNotEnoughFreeSpace".Translate(), wearer, MessageTypeDefOf.RejectInput, historical: false);
		return false;
	}
}
