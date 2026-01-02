using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AncotLibrary;

public class CompAbilitySpawnPawn : CompAbilityEffect
{
	public new CompProperties_AbilitySpawnPawn Props => (CompProperties_AbilitySpawnPawn)props;

	private Pawn Pawn => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override bool GizmoDisabled(out string reason)
	{
		Map map = Pawn.Map;
		reason = "";
		if (FindValidPosition(map, out var _))
		{
			return false;
		}
		reason = "Ancot.Ability_NoSpace".Translate();
		return true;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		Map map = Pawn.Map;
		Lord lord = Pawn.GetLord();
		if (FindValidPosition(map, out var _))
		{
			for (int i = 0; i < Props.spawnCount; i++)
			{
				PawnGenerationRequest request = new PawnGenerationRequest(Props.pawnKind, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: true, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: true, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, forceRecruitable: true);
				Pawn pawn = PawnGenerator.GeneratePawn(request);
				pawn.ageTracker.AgeBiologicalTicks = Props.spawnAge * 60000;
				GenSpawn.Spawn(pawn, Pawn.Position, Pawn.Map);
				if (Props.setFaction)
				{
					pawn.SetFaction(Pawn.Faction);
				}
				SpawnEffect(pawn);
				if (Props.hediffAddToSpawnPawn != null)
				{
					pawn.health.AddHediff(Props.hediffAddToSpawnPawn);
				}
				lord?.AddPawn(pawn);
				CompCommandTerminal compCommandTerminal = pawn.TryGetComp<CompCommandTerminal>();
				if (compCommandTerminal != null)
				{
					compCommandTerminal.sortie_Terminal = true;
					if (Pawn != null)
					{
						compCommandTerminal.pivot = Pawn;
					}
				}
			}
			parent.comps.OfType<CompAbilityUsedCount>().FirstOrDefault()?.UsedOnce();
		}
		else
		{
			Messages.Message("AbilityNotEnoughFreeSpace".Translate(), Pawn, MessageTypeDefOf.RejectInput, historical: false);
		}
	}

	private void SpawnEffect(Thing thing)
	{
		if (Props.effect != null)
		{
			Effecter effecter = Props.effect.Spawn();
			effecter.Trigger(new TargetInfo(thing.Position, thing.Map), null);
			effecter.Cleanup();
		}
	}

	private bool FindValidPosition(Map map, out IntVec3 validPosition)
	{
		int num = GenRadial.NumCellsInRadius(4f);
		for (int i = 0; i < num; i++)
		{
			IntVec3 intVec = Pawn.Position + GenRadial.RadialPattern[i];
			if (intVec.IsValid && intVec.InBounds(map))
			{
				validPosition = intVec;
				return true;
			}
		}
		validPosition = IntVec3.Invalid;
		return false;
	}
}
