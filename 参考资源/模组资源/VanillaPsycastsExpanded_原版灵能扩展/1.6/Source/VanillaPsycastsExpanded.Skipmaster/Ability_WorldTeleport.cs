using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace VanillaPsycastsExpanded.Skipmaster;

public class Ability_WorldTeleport : Ability
{
	public override void DoAction()
	{
		Pawn pawn = PawnsToSkip().FirstOrDefault((Pawn p) => p.IsQuestLodger());
		if (pawn != null)
		{
			Dialog_MessageBox.CreateConfirmation("FarskipConfirmTeleportingLodger".Translate(pawn.Named("PAWN")), (Action)base.DoAction, destructive: false, (string)null, WindowLayer.Dialog);
		}
		else
		{
			((Ability)this).DoAction();
		}
	}

	private IEnumerable<Pawn> PawnsToSkip()
	{
		Caravan caravan = base.pawn.GetCaravan();
		if (caravan != null)
		{
			foreach (Pawn pawn2 in caravan.pawns)
			{
				yield return pawn2;
			}
			yield break;
		}
		bool homeMap = base.pawn.Map.IsPlayerHome;
		foreach (Thing item in GenRadial.RadialDistinctThingsAround(base.pawn.Position, base.pawn.Map, ((Ability)this).GetRadiusForPawn(), useCenter: true))
		{
			if (!(item is Pawn { Dead: false } pawn))
			{
				continue;
			}
			if (!pawn.IsColonist && !pawn.IsPrisonerOfColony)
			{
				if (homeMap || !pawn.RaceProps.Animal)
				{
					continue;
				}
				Faction faction = pawn.Faction;
				if (faction == null || !faction.IsPlayer)
				{
					continue;
				}
			}
			yield return pawn;
		}
	}

	private Pawn AlliedPawnOnMap(Map targetMap)
	{
		return targetMap.mapPawns.AllPawnsSpawned.FirstOrDefault((Pawn p) => !p.NonHumanlikeOrWildMan() && p.IsColonist && p.HomeFaction == Faction.OfPlayer && !PawnsToSkip().Contains(p));
	}

	private bool ShouldEnterMap(GlobalTargetInfo target)
	{
		if (target.WorldObject is Caravan caravan && caravan.Faction == base.pawn.Faction)
		{
			return false;
		}
		if (target.WorldObject is MapParent { HasMap: not false } mapParent)
		{
			if (AlliedPawnOnMap(mapParent.Map) == null)
			{
				return mapParent.Map == base.pawn.Map;
			}
			return true;
		}
		return false;
	}

	public override bool CanHitTargetTile(GlobalTargetInfo target)
	{
		Caravan caravan = base.pawn.GetCaravan();
		if (caravan != null && caravan.ImmobilizedByMass)
		{
			return false;
		}
		Caravan caravan2 = target.WorldObject as Caravan;
		if ((caravan == null || caravan != caravan2) && (ShouldEnterMap(target) || (caravan2 != null && caravan2.Faction == base.pawn.Faction)))
		{
			return ((Ability)this).CanHitTargetTile(target);
		}
		return false;
	}

	public override bool IsEnabledForPawn(out string reason)
	{
		if (!((Ability)this).IsEnabledForPawn(ref reason))
		{
			return false;
		}
		Caravan caravan = base.pawn.GetCaravan();
		if (caravan != null && caravan.ImmobilizedByMass)
		{
			reason = "CaravanImmobilizedByMass".Translate();
			return false;
		}
		return true;
	}

	public override void WarmupToil(Toil toil)
	{
		((Ability)this).WarmupToil(toil);
		toil.AddPreTickAction(delegate
		{
			if (base.pawn.jobs.curDriver.ticksLeftThisToil != 5)
			{
				return;
			}
			foreach (Pawn item in PawnsToSkip())
			{
				FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(item, FleckDefOf.PsycastSkipFlashEntry, Vector3.zero);
				dataAttachedOverlay.link.detachAfterTicks = 5;
				item.Map.flecks.CreateFleck(dataAttachedOverlay);
				((Ability)this).AddEffecterToMaintain(EffecterDefOf.Skip_Entry.Spawn(base.pawn, base.pawn.Map), base.pawn.Position, 60, (Map)null);
			}
		});
	}

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		Caravan caravan = base.pawn.GetCaravan();
		Map targetMap = ((targets[0].WorldObject is MapParent mapParent) ? mapParent.Map : null);
		IntVec3 targetCell = IntVec3.Invalid;
		List<Pawn> list = PawnsToSkip().ToList();
		if (base.pawn.Spawned)
		{
			SoundDefOf.Psycast_Skip_Pulse.PlayOneShot(new TargetInfo(targets[0].Cell, base.pawn.Map));
		}
		if (targetMap != null)
		{
			Pawn pawn = AlliedPawnOnMap(targetMap);
			if (pawn != null)
			{
				IntVec3 position = pawn.Position;
				targetCell = position;
			}
		}
		AbilityExtension_Clamor modExtension = ((Def)(object)base.def).GetModExtension<AbilityExtension_Clamor>();
		if (targetCell.IsValid)
		{
			foreach (Pawn item in list)
			{
				if (item.Spawned)
				{
					item.teleporting = true;
					item.ExitMap(allowedToJoinOrCreateCaravan: false, Rot4.Invalid);
					AbilityUtility.DoClamor(item.Position, modExtension.clamorRadius, base.pawn, modExtension.clamorType);
					item.teleporting = false;
				}
				CellFinder.TryFindRandomSpawnCellForPawnNear(targetCell, targetMap, out var result, 4, (IntVec3 cell) => cell != targetCell && cell.GetRoom(targetMap) == targetCell.GetRoom(targetMap));
				GenSpawn.Spawn(item, result, targetMap);
				if (item.drafter != null && item.IsColonistPlayerControlled)
				{
					item.drafter.Drafted = true;
				}
				item.Notify_Teleported();
				if (item.IsPrisoner)
				{
					item.guest.WaitInsteadOfEscapingForDefaultTicks();
				}
				((Ability)this).AddEffecterToMaintain(EffecterDefOf.Skip_ExitNoDelay.Spawn(item, item.Map), item.Position, 60, targetMap);
				SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(result, item.Map));
				if ((item.IsColonist || item.RaceProps.packAnimal) && item.Map.IsPlayerHome)
				{
					item.inventory.UnloadEverything = true;
				}
			}
			if (Find.WorldSelector.IsSelected(caravan))
			{
				Find.WorldSelector.Deselect(caravan);
				CameraJumper.TryJump(targetCell, targetMap);
			}
			caravan?.Destroy();
		}
		else if (targets[0].WorldObject is Caravan caravan2 && caravan2.Faction == base.pawn.Faction)
		{
			if (caravan != null)
			{
				caravan.pawns.TryTransferAllToContainer(caravan2.pawns);
				caravan2.Notify_Merged(new List<Caravan> { caravan });
				caravan.Destroy();
			}
			else
			{
				foreach (Pawn item2 in list)
				{
					caravan2.AddPawn(item2, addCarriedPawnToWorldPawnsIfAny: true);
					item2.ExitMap(allowedToJoinOrCreateCaravan: false, Rot4.Invalid);
					AbilityUtility.DoClamor(item2.Position, modExtension.clamorRadius, base.pawn, modExtension.clamorType);
				}
			}
		}
		else if (caravan != null)
		{
			caravan.Tile = targets[0].Tile;
			caravan.pather.StopDead();
		}
		else
		{
			CaravanMaker.MakeCaravan(list, base.pawn.Faction, targets[0].Tile, addToWorldPawnsIfNotAlready: false);
			foreach (Pawn item3 in list)
			{
				item3.ExitMap(allowedToJoinOrCreateCaravan: false, Rot4.Invalid);
			}
		}
		((Ability)this).Cast(targets);
	}

	public override void GizmoUpdateOnMouseover()
	{
		((Ability)this).GizmoUpdateOnMouseover();
		GenDraw.DrawRadiusRing(base.pawn.Position, ((Ability)this).GetRadiusForPawn(), Color.blue);
	}
}
