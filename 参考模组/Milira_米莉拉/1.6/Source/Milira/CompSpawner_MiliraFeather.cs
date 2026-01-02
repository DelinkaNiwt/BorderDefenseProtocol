using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Milira;

public class CompSpawner_MiliraFeather : ThingComp
{
	private int ticksUntilSpawn;

	public CompProperties_Spawner PropsSpawner => (CompProperties_Spawner)props;

	public override void Initialize(CompProperties props)
	{
		base.props = props;
		ResetCountdown();
	}

	public override void CompTickRare()
	{
		TickInterval(250);
	}

	private void TickInterval(int interval)
	{
		if (parent.Spawned && !parent.Position.Fogged(parent.Map))
		{
			ticksUntilSpawn -= interval;
			CheckShouldSpawn();
		}
	}

	private void CheckShouldSpawn()
	{
		if (ticksUntilSpawn <= 0)
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
		if (TryFindSpawnCell(parent, PropsSpawner.thingToSpawn, PropsSpawner.spawnCount, out var result))
		{
			Thing thing = ThingMaker.MakeThing(PropsSpawner.thingToSpawn);
			thing.stackCount = PropsSpawner.spawnCount;
			if (thing == null)
			{
				Log.Error("Could not spawn anything for " + parent);
			}
			if (PropsSpawner.inheritFaction && thing.Faction != parent.Faction)
			{
				thing.SetFaction(parent.Faction);
			}
			GenPlace.TryPlaceThing(thing, result, parent.Map, ThingPlaceMode.Direct, out var lastResultingThing);
			if (PropsSpawner.spawnForbidden)
			{
				lastResultingThing.SetForbidden(value: true);
			}
			if (PropsSpawner.showMessageIfOwned && parent.Faction == Faction.OfPlayer)
			{
				Messages.Message("MessageCompSpawnerSpawnedItem".Translate(PropsSpawner.thingToSpawn.LabelCap), thing, MessageTypeDefOf.PositiveEvent);
			}
			return true;
		}
		return false;
	}

	public static bool TryFindSpawnCell(Thing parent, ThingDef thingToSpawn, int spawnCount, out IntVec3 result)
	{
		foreach (IntVec3 item in GenAdj.CellsAdjacent8Way(parent).InRandomOrder())
		{
			if (!item.Walkable(parent.Map))
			{
				continue;
			}
			Building edifice = item.GetEdifice(parent.Map);
			if ((edifice != null && thingToSpawn.IsEdifice()) || edifice is Building_Door { FreePassage: false } || (parent.def.passability != Traversability.Impassable && !GenSight.LineOfSight(parent.Position, item, parent.Map)))
			{
				continue;
			}
			bool flag = false;
			List<Thing> thingList = item.GetThingList(parent.Map);
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				if (thing.def.category == ThingCategory.Item && (thing.def != thingToSpawn || thing.stackCount > thingToSpawn.stackLimit - spawnCount))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				continue;
			}
			result = item;
			return true;
		}
		result = IntVec3.Invalid;
		return false;
	}

	private void ResetCountdown()
	{
		ticksUntilSpawn = PropsSpawner.spawnIntervalRange.RandomInRange;
	}

	public override void PostExposeData()
	{
		string text = (PropsSpawner.saveKeysPrefix.NullOrEmpty() ? null : (PropsSpawner.saveKeysPrefix + "_"));
		Scribe_Values.Look(ref ticksUntilSpawn, text + "ticksUntilSpawn", 0);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (DebugSettings.ShowDevGizmos)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Spawn " + PropsSpawner.thingToSpawn.label,
				icon = TexCommand.DesirePower,
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
		if (PropsSpawner.writeTimeLeftToSpawn)
		{
			return "NextSpawnedItemIn".Translate(GenLabel.ThingLabel(PropsSpawner.thingToSpawn, null, PropsSpawner.spawnCount)).Resolve() + ": " + ticksUntilSpawn.ToStringTicksToPeriod().Colorize(ColoredText.DateTimeColor);
		}
		return null;
	}
}
