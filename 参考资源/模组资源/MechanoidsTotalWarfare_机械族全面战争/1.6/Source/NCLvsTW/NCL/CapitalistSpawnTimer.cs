using RimWorld;
using Verse;

namespace NCL;

public class CapitalistSpawnTimer : GameComponent
{
	private int ticksPassed = 0;

	private bool eventFired = false;

	private const int DaysToTrigger = 15;

	public CapitalistSpawnTimer(Game game)
	{
	}

	public override void GameComponentTick()
	{
		base.GameComponentTick();
		if (Current.ProgramState == ProgramState.Playing && !eventFired)
		{
			ticksPassed++;
			int currentDays = ticksPassed / 60000;
			if (currentDays >= 15)
			{
				TriggerCapitalistEvent();
				eventFired = true;
			}
		}
	}

	private void TriggerCapitalistEvent()
	{
		IncidentDef incident = DefDatabase<IncidentDef>.GetNamed("TW_CapitalistWandersIn");
		if (incident == null)
		{
			Log.Error("[NCL] TW_CapitalistWandersIn incident not found!");
			return;
		}
		foreach (Map map in Find.Maps)
		{
			if (map.IsPlayerHome)
			{
				IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
				if (incident.Worker.CanFireNow(parms))
				{
					incident.Worker.TryExecute(parms);
				}
			}
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref ticksPassed, "ticksPassed", 0);
		Scribe_Values.Look(ref eventFired, "eventFired", defaultValue: false);
	}
}
