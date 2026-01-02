using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TOT_DLL_test;

public class StockGenerator_CMCClones : StockGenerator
{
	private bool respectPopulationIntent;

	public PawnKindDef slaveKindDef;

	public override bool HandlesThingDef(ThingDef thingDef)
	{
		return thingDef.category == ThingCategory.Pawn && thingDef.race.Humanlike && (int)thingDef.tradeability > 0;
	}

	public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
	{
		if (respectPopulationIntent && Rand.Value > StorytellerUtilityPopulation.PopulationIntent)
		{
			yield break;
		}
		if (faction != null && faction.ideos != null)
		{
			bool flag = true;
			foreach (Ideo allIdeo in faction.ideos.AllIdeos)
			{
				if (!allIdeo.IdeoApprovesOfSlavery())
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				yield break;
			}
		}
		int count = countRange.RandomInRange;
		int i = 0;
		Faction faction2;
		while (i < count && Find.FactionManager.AllFactionsVisible.Where((Faction fac) => fac != Faction.OfPlayer && fac.def.humanlikeFaction && !fac.temporary).TryRandomElement(out faction2))
		{
			if (!Find.Storyteller.difficulty.ChildrenAllowed)
			{
			}
			PawnKindDef kind = ((slaveKindDef != null) ? slaveKindDef : PawnKindDefOf.Slave);
			Faction faction3 = faction2;
			PlanetTile? tile = forTile;
			bool forceAddFreeWarmLayerIfNeeded = !trader.orbital;
			float? fixedChronologicalAge = Rand.Range(114f, 514f);
			float? fixedBiologicalAge = Rand.Range(17f, 25f);
			PawnGenerationRequest request = new PawnGenerationRequest(kind, faction3, PawnGenerationContext.NonPlayer, tile, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true, 0f, forceAddFreeWarmLayerIfNeeded, allowGay: false, allowPregnant: false, allowFood: true, allowAddictions: false, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, fixedBiologicalAge, fixedChronologicalAge, null, null, null, null, null, forceNoIdeo: true, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 1f, DevelopmentalStage.Adult, null, null, null, forceRecruitable: true, dontGiveWeapon: false, onlyUseForcedBackstories: false, -1, 0, forceNoGear: true);
			yield return PawnGenerator.GeneratePawn(request);
			int num = i;
			faction2 = null;
			i = num + 1;
		}
	}
}
