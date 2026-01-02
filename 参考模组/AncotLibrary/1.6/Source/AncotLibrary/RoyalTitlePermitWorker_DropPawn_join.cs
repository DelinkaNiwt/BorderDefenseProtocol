using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class RoyalTitlePermitWorker_DropPawn_join : RoyalTitlePermitWorker_Targeted
{
	private Faction faction;

	public override void OrderForceTarget(LocalTargetInfo target)
	{
		CallResources(target.Cell);
	}

	public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
	{
		if (faction.HostileTo(Faction.OfPlayer))
		{
			yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null);
			yield break;
		}
		Action action = null;
		string description = def.LabelCap + ": ";
		if (FillAidOption(pawn, faction, ref description, out var free))
		{
			action = delegate
			{
				BeginCallResources(pawn, faction, map, free);
			};
		}
		yield return new FloatMenuOption(description, action, faction.def.FactionIcon, faction.Color);
	}

	private void BeginCallResources(Pawn caller, Faction faction, Map map, bool free)
	{
		targetingParameters = new TargetingParameters();
		targetingParameters.canTargetLocations = true;
		targetingParameters.canTargetBuildings = false;
		targetingParameters.canTargetPawns = false;
		base.caller = caller;
		base.map = map;
		this.faction = faction;
		base.free = free;
		targetingParameters.validator = delegate(TargetInfo target)
		{
			if (def.royalAid.targetingRange > 0f && target.Cell.DistanceTo(caller.Position) > def.royalAid.targetingRange)
			{
				return false;
			}
			return target.Cell.Walkable(map) && !target.Cell.Fogged(map);
		};
		Find.Targeter.BeginTargeting(this);
	}

	private void CallResources(IntVec3 cell)
	{
		List<Pawn> list = new List<Pawn>();
		for (int i = 0; i < def.royalAid.pawnCount; i++)
		{
			PawnGenerationRequest request = new PawnGenerationRequest(def.royalAid.pawnKindDef, Faction.OfPlayer, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: false, allowAddictions: false, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: true, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, forceRecruitable: true);
			Pawn item = PawnGenerator.GeneratePawn(request);
			list.Add(item);
		}
		if (list.Any())
		{
			ActiveTransporterInfo activeTransporterInfo = new ActiveTransporterInfo();
			activeTransporterInfo.innerContainer.TryAddRangeOrTransfer(list);
			DropPodUtility.MakeDropPodAt(cell, map, activeTransporterInfo);
			Messages.Message("Ancot.MessagePermitDropPawn_join".Translate(faction.Named("FACTION")), new LookTargets(cell, map), MessageTypeDefOf.NeutralEvent);
			caller.royalty.GetPermit(def, faction).Notify_Used();
			if (!free)
			{
				caller.royalty.TryRemoveFavor(faction, def.royalAid.favorCost);
			}
		}
	}
}
