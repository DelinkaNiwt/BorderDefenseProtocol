using System;
using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Harmonist;

public class Ability_RandomEvent : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			DoRandomEvent(globalTargetInfo.Map);
		}
	}

	public static void DoRandomEvent(Map map)
	{
		int num = 0;
		do
		{
			try
			{
				IncidentDef incidentDef = DefDatabase<IncidentDef>.AllDefs.RandomElement();
				if (incidentDef.Worker.TryExecute(StorytellerUtility.DefaultParmsNow(incidentDef.category, map)))
				{
					return;
				}
			}
			catch (Exception)
			{
			}
			num++;
		}
		while (num <= 1000);
		Log.Error("[VPE] Exceeded 1000 tries to spawn random event");
	}
}
