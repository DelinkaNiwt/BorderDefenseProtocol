using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace NyarsModPackOne;

public class CompDroneCarrier : ThingComp, IThingHolder
{
	private ThingOwner innerContainer;

	public int cooldownTicksRemaining;

	public int maxToFill;

	public bool autoFill = true;

	public bool autoSpawn = false;

	public List<Drone> spawnedDrones = new List<Drone>();

	private DroneCarrierGizmo gizmo;

	private List<Thing> tmpResources = new List<Thing>();

	private const int LowIngredientThreshold = 250;

	public bool AutoFill
	{
		get
		{
			return autoFill;
		}
		set
		{
			autoFill = value;
		}
	}

	public CompProperties_DroneCarrier Props => (CompProperties_DroneCarrier)props;

	public int IngredientCount => innerContainer?.TotalStackCountOfDef(Props.fixedIngredient) ?? 0;

	public int AmountToAutofill => Mathf.Max(0, maxToFill - IngredientCount);

	public float FillPercentage => (float)IngredientCount / (float)Props.maxIngredientCount;

	public int MaxCanSpawn => Mathf.Min(Mathf.FloorToInt((float)IngredientCount / (float)Props.costPerDrone), Props.maxDronesPerSpawn);

	public bool LowIngredient => IngredientCount < 250;

	public float CooldownPercent => (Props.cooldownTicks > 0) ? ((float)cooldownTicksRemaining / (float)Props.cooldownTicks) : 0f;

	private bool CanSpawnNow
	{
		get
		{
			int result;
			if (MaxCanSpawn > 0 && cooldownTicksRemaining <= 0 && parent.Spawned)
			{
				Pawn obj = parent as Pawn;
				result = ((obj != null && !obj.Downed) ? 1 : 0);
			}
			else
			{
				result = 0;
			}
			return (byte)result != 0;
		}
	}

	private string DisabledReason
	{
		get
		{
			if (cooldownTicksRemaining > 0)
			{
				return "CooldownActive".Translate();
			}
			if (MaxCanSpawn <= 0)
			{
				return "InsufficientResources".Translate();
			}
			if (parent is Pawn { Downed: not false })
			{
				return "Incapacitated".Translate();
			}
			return string.Empty;
		}
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (!respawningAfterLoad && innerContainer == null)
		{
			innerContainer = new ThingOwner<Thing>(this);
			if (Props.startingIngredientCount > 0)
			{
				AddIngredient(Props.fixedIngredient, Props.startingIngredientCount);
			}
			maxToFill = Props.startingIngredientCount;
		}
	}

	public void AddIngredient(ThingDef ingredientDef, int amount)
	{
		if (innerContainer != null)
		{
			int num = Mathf.Min(amount, Props.maxIngredientCount - IngredientCount);
			if (num > 0)
			{
				Thing thing = ThingMaker.MakeThing(ingredientDef);
				thing.stackCount = num;
				innerContainer.TryAdd(thing);
			}
		}
	}

	public void TrySpawnDrones()
	{
		int maxCanSpawn = MaxCanSpawn;
		if (maxCanSpawn <= 0)
		{
			return;
		}
		Pawn pawn = parent as Pawn;
		Lord lord = pawn?.GetLord();
		tmpResources.Clear();
		tmpResources.AddRange(innerContainer);
		for (int i = 0; i < maxCanSpawn; i++)
		{
			Drone drone = Drone.MakeNewDrone(pawn, DroneKindDef());
			if (drone != null)
			{
				GenPlace.TryPlaceThing(drone, parent.Position, parent.Map, ThingPlaceMode.Near);
				spawnedDrones.Add(drone);
				lord?.AddPawn(drone);
				if (!ConsumeResources(Props.costPerDrone))
				{
					Log.Warning($"Failed to consume resources for drone #{i}");
				}
			}
		}
		tmpResources.Clear();
		cooldownTicksRemaining = Props.cooldownTicks;
	}

	private PawnKindDef DroneKindDef()
	{
		if (Props.droneKind != null)
		{
			return Props.droneKind;
		}
		return PawnKindDef.Named("NCL_Dinergate_Drone");
	}

	private bool ConsumeResources(int amount)
	{
		int num = amount;
		List<Thing> list = new List<Thing>(innerContainer);
		foreach (Thing item in list)
		{
			if (item.def == Props.fixedIngredient && item.stackCount > 0)
			{
				int num2 = Mathf.Min(item.stackCount, num);
				if (num2 >= item.stackCount)
				{
					innerContainer.Remove(item);
					item.Destroy();
				}
				else if (num2 > 0)
				{
					item.SplitOff(num2)?.Destroy();
				}
				num -= num2;
				if (num <= 0)
				{
					break;
				}
			}
		}
		return num <= 0;
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (parent.Faction != Faction.OfPlayer)
		{
			yield break;
		}
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (gizmo == null)
		{
			gizmo = new DroneCarrierGizmo(this);
		}
		yield return gizmo;
		yield return new Command_Toggle
		{
			icon = ContentFinder<Texture2D>.Get("ModIcon/CompAutoMechSpawner"),
			defaultLabel = "AutoReleaseDrones".Translate(),
			defaultDesc = "AutoReleaseDronesDesc".Translate(),
			isActive = () => autoSpawn,
			toggleAction = delegate
			{
				autoSpawn = !autoSpawn;
			}
		};
		Command_Action spawnCommand = new Command_Action
		{
			icon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath),
			defaultLabel = "ReleaseDrones".Translate(),
			defaultDesc = "ReleaseDronesDesc".Translate(MaxCanSpawn, Props.droneKind?.LabelCap ?? "Drone".Translate()),
			disabledReason = DisabledReason,
			action = TrySpawnDrones
		};
		if (cooldownTicksRemaining > 0)
		{
			spawnCommand.Disable("Cooldown".Translate() + ": " + cooldownTicksRemaining.ToStringSecondsFromTicks());
		}
		yield return spawnCommand;
		if (DebugSettings.ShowDevGizmos)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Fill Resources",
				action = delegate
				{
					AddIngredient(Props.fixedIngredient, Props.maxIngredientCount);
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "DEV: Reset Cooldown",
				action = delegate
				{
					cooldownTicksRemaining = 0;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "DEV: Toggle AutoSpawn",
				action = delegate
				{
					autoSpawn = !autoSpawn;
				}
			};
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
		Scribe_Values.Look(ref cooldownTicksRemaining, "cooldownTicksRemaining", 0);
		Scribe_Values.Look(ref maxToFill, "maxToFill", 0);
		Scribe_Values.Look(ref autoSpawn, "autoSpawn", defaultValue: false);
		Scribe_Collections.Look(ref spawnedDrones, "spawnedDrones", LookMode.Reference);
	}

	public void GetChildHolders(List<IThingHolder> outChildren)
	{
		ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
	}

	public ThingOwner GetDirectlyHeldThings()
	{
		return innerContainer;
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		innerContainer?.ClearAndDestroyContents();
		foreach (Drone spawnedDrone in spawnedDrones)
		{
			if (!spawnedDrone.Destroyed)
			{
				spawnedDrone.Destroy();
			}
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		if (cooldownTicksRemaining > 0)
		{
			cooldownTicksRemaining--;
		}
		if (autoSpawn && cooldownTicksRemaining <= 0 && MaxCanSpawn > 0 && parent.Spawned)
		{
			Pawn obj = parent as Pawn;
			if (obj != null && !obj.Downed)
			{
				TrySpawnDrones();
			}
		}
	}
}
