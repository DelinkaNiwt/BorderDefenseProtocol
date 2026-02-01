using RimWorld;
using Verse;

namespace NCL;

public class Comp_HostilePresence : ThingComp
{
	private GameCondition activeCondition;

	private GameConditionDef conditionDef;

	private Map conditionMap;

	public int conditionDurationTicks = 180000;

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
		conditionDef = DefDatabase<GameConditionDef>.GetNamed("Mech2000Coming");
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		TryCreateCondition();
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_References.Look(ref activeCondition, "activeCondition");
		Scribe_References.Look(ref conditionMap, "conditionMap");
	}

	public override void CompTick()
	{
		base.CompTick();
		if (Find.TickManager.TicksGame % 250 == 0)
		{
			if (activeCondition == null)
			{
				TryCreateCondition();
			}
			else
			{
				ValidateCondition();
			}
		}
	}

	public override void PostDeSpawn(Map map, DestroyMode mode)
	{
		base.PostDeSpawn(map, mode);
		RemoveCondition();
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		RemoveCondition();
	}

	private void TryCreateCondition()
	{
		if (activeCondition == null && !parent.Destroyed && parent.Map != null && IsHostileToPlayer(parent))
		{
			activeCondition = GameConditionMaker.MakeCondition(conditionDef, conditionDurationTicks);
			parent.Map.gameConditionManager.RegisterCondition(activeCondition);
			conditionMap = parent.Map;
			Log.Message("Hostile presence condition created for " + parent.Label + ", duration: " + conditionDurationTicks.ToStringTicksToPeriod());
		}
	}

	private void ValidateCondition()
	{
		if (activeCondition != null)
		{
			bool shouldRemove = parent.Destroyed || parent.Map == null || !IsHostileToPlayer(parent) || activeCondition.Expired;
			if (parent.Map != null && parent.Map != conditionMap)
			{
				shouldRemove = true;
			}
			if (shouldRemove)
			{
				RemoveCondition();
			}
		}
	}

	private void RemoveCondition()
	{
		if (activeCondition != null)
		{
			if (conditionMap != null && conditionMap.gameConditionManager != null)
			{
				conditionMap.gameConditionManager.ActiveConditions.Remove(activeCondition);
			}
			activeCondition.End();
			activeCondition = null;
			conditionMap = null;
			Log.Message("Hostile presence condition removed for " + parent.Label);
		}
	}

	private bool IsHostileToPlayer(Thing thing)
	{
		return thing.Faction != null && thing.Faction != Faction.OfPlayer && thing.Faction.HostileTo(Faction.OfPlayer);
	}
}
