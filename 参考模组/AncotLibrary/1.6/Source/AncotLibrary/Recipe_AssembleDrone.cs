using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class Recipe_AssembleDrone : RecipeWorker
{
	public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
	{
		ModExtension_AssembleDrone modExtension = recipe.GetModExtension<ModExtension_AssembleDrone>();
		if (modExtension?.droneKind != null)
		{
			for (int i = 0; i < modExtension.num; i++)
			{
				PawnGenerationRequest request = new PawnGenerationRequest(modExtension.droneKind, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: true, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: true, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, forceRecruitable: true);
				Pawn pawn = PawnGenerator.GeneratePawn(request);
				pawn.ageTracker.AgeBiologicalTicks = 0L;
				pawn.ageTracker.AgeChronologicalTicks = 0L;
				pawn.SetFaction(billDoer.Faction);
				GenSpawn.Spawn(pawn, billDoer.Position, billDoer.Map);
			}
		}
	}
}
