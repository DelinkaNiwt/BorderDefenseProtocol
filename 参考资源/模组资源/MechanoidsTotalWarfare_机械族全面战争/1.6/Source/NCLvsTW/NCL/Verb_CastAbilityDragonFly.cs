using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace NCL;

[StaticConstructorOnStartup]
public class Verb_CastAbilityDragonFly : Verb_CastAbility
{
	public static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("Things/SuckerPunch");

	private DragonFlyExtension dragonFlyExtension;

	public int MaxLaunchDistance = 50;

	protected override bool TryCastShot()
	{
		if (caster.Faction != Faction.OfPlayer)
		{
			return false;
		}
		dragonFlyExtension = ability.def.GetModExtension<DragonFlyExtension>();
		if (dragonFlyExtension == null)
		{
			Log.Error("Missing DragonFlyExtension on ability: " + ability.def.defName);
			return false;
		}
		if (CasterPawn.IsColonyMechPlayerControlled && CasterPawn.Drafted)
		{
			StartChoosingDestination();
		}
		ability.StartCooldown(ability.def.cooldownTicksRange.RandomInRange);
		return true;
	}

	public void StartChoosingDestination()
	{
		CameraJumper.TryJump(CameraJumper.GetWorldTarget(caster));
		Find.WorldSelector.ClearSelection();
		int tile = caster.Map.Tile;
		Find.WorldTargeter.BeginTargeting(ChoseWorldTarget, canTargetTiles: true, TargeterMouseAttachment, closeWorldTabWhenFinished: true, delegate
		{
			GenDraw.DrawWorldRadiusRing(tile, MaxLaunchDistance);
		}, (GlobalTargetInfo target) => TargetingLabelGetter(target, tile, MaxLaunchDistance, null, TryLaunch));
	}

	public void TryLaunch(int destinationTile, TransportersArrivalAction arrivalAction)
	{
		if (caster is Pawn { drafter: not null } pawn)
		{
			pawn.drafter.Drafted = false;
		}
		if (!caster.Spawned)
		{
			Log.Error("Tried to launch " + caster?.ToString() + ", but it's unspawned.");
			return;
		}
		Map map = caster.Map;
		int distance = Find.WorldGrid.TraversalDistanceBetween(map.Tile, destinationTile);
		if (distance > MaxLaunchDistance)
		{
			Messages.Message("TransportPodDestinationBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput);
			return;
		}
		ThingDef activeTransporterDef = dragonFlyExtension.activeTransporterDef ?? ThingDef.Named("TW_ActiveDropPod");
		Thing transporterThing = ThingMaker.MakeThing(activeTransporterDef);
		ActiveTransporter activeTransporter = transporterThing as ActiveTransporter;
		activeTransporter.Contents = new ActiveTransporterInfo();
		caster.DeSpawn();
		if (!activeTransporter.Contents.innerContainer.TryAddOrTransfer(caster))
		{
			Log.Error("Failed to add caster to transporter");
			return;
		}
		FlyShipLeaving flyShip = (FlyShipLeaving)SkyfallerMaker.MakeSkyfaller(dragonFlyExtension.dropPodLeavingDef, activeTransporter);
		flyShip.groupID = Find.UniqueIDsManager.GetNextTransporterGroupID();
		flyShip.destinationTile = destinationTile;
		flyShip.arrivalAction = arrivalAction;
		flyShip.worldObjectDef = WorldObjectDefOf.TravellingTransporters;
		GenSpawn.Spawn(flyShip, caster.Position, map);
		CameraJumper.TryHideWorld();
	}

	public bool ChoseWorldTarget(GlobalTargetInfo target)
	{
		return ChoseWorldTarget(target, caster.Map.Tile, MaxLaunchDistance, TryLaunch);
	}

	public bool ChoseWorldTarget(GlobalTargetInfo target, int tile, int maxLaunchDistance, Action<int, TransportersArrivalAction> launchAction)
	{
		if (!target.IsValid)
		{
			Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return false;
		}
		int distance = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
		if (maxLaunchDistance > 0 && distance > maxLaunchDistance)
		{
			Messages.Message("TransportPodDestinationBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return false;
		}
		IEnumerable<FloatMenuOption> options = GetTransportPodsFloatMenuOptionsAt(target.Tile);
		if (!options.Any())
		{
			return false;
		}
		if (options.Count() == 1)
		{
			FloatMenuOption option = options.First();
			if (!option.Disabled)
			{
				option.action();
				return true;
			}
			return false;
		}
		Find.WindowStack.Add(new FloatMenu(options.ToList()));
		return false;
	}

	public IEnumerable<FloatMenuOption> GetTransportPodsFloatMenuOptionsAt(int tile)
	{
		foreach (WorldObject worldObject in Find.WorldObjects.AllWorldObjects)
		{
			if (worldObject.Tile == tile)
			{
				MapParent mapParent = worldObject as MapParent;
				if (mapParent?.HasMap ?? false)
				{
					yield return GetTransportPodsFloatMenuOptions(tile, mapParent);
				}
			}
		}
	}

	public FloatMenuOption GetTransportPodsFloatMenuOptions(int tile, MapParent mapParent)
	{
		return new FloatMenuOption("LandInExistingMap".Translate(mapParent.Label), delegate
		{
			Map originalMap = caster.Map;
			Map map = mapParent.Map;
			Current.Game.CurrentMap = map;
			Find.Targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(), delegate(LocalTargetInfo x)
			{
				ThingDef landingPodDef = dragonFlyExtension.landingPodDef ?? ThingDefOf.DropPodIncoming;
				ThingDef activeTransporterDef = dragonFlyExtension.activeTransporterDef ?? ThingDef.Named("TW_ActiveDropPod");
				TransportersArrivalAction_SZLandInSpecificCell arrivalAction = new TransportersArrivalAction_SZLandInSpecificCell(mapParent, x.Cell, landingPodDef, activeTransporterDef);
				TryLaunch(tile, arrivalAction);
			}, null, delegate
			{
				if (Find.Maps.Contains(originalMap))
				{
					Current.Game.CurrentMap = originalMap;
				}
			}, CompLaunchable.TargeterMouseAttachment);
		});
	}

	public static string TargetingLabelGetter(GlobalTargetInfo target, int tile, int maxLaunchDistance, IEnumerable<IThingHolder> pods, Action<int, TransportersArrivalAction> launchAction)
	{
		if (target.WorldObject is MapParent { HasMap: not false } mapParent)
		{
			return "ClickToSeeAvailableOrders_WorldObject".Translate(mapParent.LabelCap);
		}
		return "InvalidTarget".Translate();
	}
}
