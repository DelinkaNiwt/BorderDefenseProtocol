using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompSpawnerCustom : ThingComp
{
	private int ticksToSpawn = 1;

	private int ticksUntilSpawn;

	public CompProperties_SpawnerCustom PropsSpawner => (CompProperties_SpawnerCustom)props;

	public bool PowerOn => parent.GetComp<CompPowerTrader>()?.PowerOn ?? false;

	public bool HasFuel => parent.GetComp<CompRefuelable>()?.HasFuel ?? false;

	public override void Initialize(CompProperties props)
	{
		base.props = props;
		ResetCountdown();
	}

	public override void CompTick()
	{
		TickInterval(1);
	}

	public override void CompTickRare()
	{
		TickInterval(250);
	}

	public override void CompTickLong()
	{
		TickInterval(2000);
	}

	private void TickInterval(int interval)
	{
		if (!parent.Spawned)
		{
			return;
		}
		CompCanBeDormant comp = parent.GetComp<CompCanBeDormant>();
		if (comp != null)
		{
			if (!comp.Awake)
			{
				return;
			}
		}
		else if (parent.Position.Fogged(parent.Map))
		{
			return;
		}
		if ((!PropsSpawner.requiresPower || PowerOn) && (!PropsSpawner.requiresFuel || HasFuel))
		{
			ticksToSpawn += interval;
			CheckShouldSpawn();
		}
	}

	private void CheckShouldSpawn()
	{
		if (ticksToSpawn > ticksUntilSpawn)
		{
			ResetCountdown();
			TryDoSpawn();
		}
	}

	public bool TryDoSpawn()
	{
		if (!parent.Spawned)
		{
			return false;
		}
		if (PropsSpawner.spawnMaxAdjacent >= 0)
		{
			int num = 0;
			for (int i = 0; i < 9; i++)
			{
				IntVec3 c = parent.Position + GenAdj.AdjacentCellsAndInside[i];
				if (!c.InBounds(parent.Map))
				{
					continue;
				}
				List<Thing> thingList = c.GetThingList(parent.Map);
				for (int j = 0; j < thingList.Count; j++)
				{
					if (thingList[j].def == PropsSpawner.thingToSpawn)
					{
						num += thingList[j].stackCount;
						if (num >= PropsSpawner.spawnMaxAdjacent)
						{
							return false;
						}
					}
				}
			}
		}
		if (TryFindSpawnCell(parent, PropsSpawner.thingToSpawn, PropsSpawner.spawnCountRange.max, PropsSpawner.spawnCountRange.min, out var result))
		{
			Thing thing = ThingMaker.MakeThing(PropsSpawner.thingToSpawn);
			thing.stackCount = PropsSpawner.spawnCountRange.RandomInRange;
			if (thing == null)
			{
				Log.Error("Could not spawn anything for " + parent);
			}
			if (PropsSpawner.inheritFaction && thing.Faction != parent.Faction)
			{
				thing.SetFaction(parent.Faction);
			}
			GenPlace.TryPlaceThing(thing, result, parent.Map, ThingPlaceMode.Direct, out var lastResultingThing, null, null, default(Rot4));
			if (PropsSpawner.spawnForbidden)
			{
				lastResultingThing.SetForbidden(value: true);
			}
			if (PropsSpawner.showMessageIfOwned && parent.Faction == Faction.OfPlayer)
			{
				Messages.Message("MessageCompSpawnerSpawnedItem".Translate(PropsSpawner.thingToSpawn.LabelCap), thing, MessageTypeDefOf.PositiveEvent);
			}
			if (PropsSpawner.explodeWhileSpawn)
			{
				int randomInRange = PropsSpawner.explosionDamageRange.RandomInRange;
				GenExplosion.DoExplosion(parent.Position, parent.Map, 4f, PropsSpawner.explodeDamageDef, null, randomInRange);
			}
			return true;
		}
		return false;
	}

	public static bool TryFindSpawnCell(Thing parent, ThingDef thingToSpawn, int maxSpawnCount, int minSpawnCount, out IntVec3 result)
	{
		foreach (IntVec3 item in GenAdj.CellsAdjacent8Way(parent).InRandomOrder())
		{
			if (!item.Walkable(parent.Map))
			{
				continue;
			}
			Building edifice = item.GetEdifice(parent.Map);
			if ((edifice != null && thingToSpawn.IsEdifice()) || edifice is Building_Door { FreePassage: false } || (parent.def.passability != Traversability.Impassable && !GenSight.LineOfSight(parent.Position, item, parent.Map, skipFirstCell: false, null, 0, 0)))
			{
				continue;
			}
			bool flag = false;
			List<Thing> thingList = item.GetThingList(parent.Map);
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				if (thing.def.category == ThingCategory.Item && (thing.def != thingToSpawn || thing.stackCount > thingToSpawn.stackLimit - maxSpawnCount))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				result = item;
				return true;
			}
		}
		result = IntVec3.Invalid;
		return false;
	}

	private void ResetCountdown()
	{
		ticksUntilSpawn = PropsSpawner.spawnIntervalRange.RandomInRange;
		ticksToSpawn = 1;
	}

	public override void PostExposeData()
	{
		string text = (PropsSpawner.saveKeysPrefix.NullOrEmpty() ? null : (PropsSpawner.saveKeysPrefix + "_"));
		Scribe_Values.Look(ref ticksToSpawn, text + "ticksToSpawn", 0);
		Scribe_Values.Look(ref ticksUntilSpawn, text + "ticksUntilSpawn", 0);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (DebugSettings.ShowDevGizmos)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Spawn " + PropsSpawner.thingToSpawn.label,
				action = delegate
				{
					ResetCountdown();
					TryDoSpawn();
				}
			};
		}
	}

	public override string CompInspectStringExtra()
	{
		if (PropsSpawner.writeTimeLeftToSpawn && (!PropsSpawner.requiresPower || PowerOn))
		{
			return "Ancot.NextSpawnedItemIn".Translate(PropsSpawner.spawnCountRange.ToString(), PropsSpawner.thingToSpawn.label).Resolve() + ": " + ((float)ticksToSpawn / (float)ticksUntilSpawn).ToStringPercentEmptyZero().Colorize(ColoredText.DateTimeColor);
		}
		return null;
	}
}
