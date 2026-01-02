using RimWorld;
using Verse;

namespace Milira;

public class CompSunBlastFurnaceIllegalUse : ThingComp
{
	private int num = 0;

	public CompProperties_SunBlastFurnaceIllegalUse Props => (CompProperties_SunBlastFurnaceIllegalUse)props;

	public void Notify_UsedThisTick()
	{
		num++;
		if (num >= Props.ticksPerPoint)
		{
			num = 0;
			IncreaseMiliraThreatPoint();
		}
	}

	public void IncreaseMiliraThreatPoint()
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		if (faction != null && faction.HostileTo(Faction.OfPlayer))
		{
			Current.Game.GetComponent<MiliraGameComponent_OverallControl>().miliraThreatPoint++;
		}
	}

	public override void PostExposeData()
	{
		Scribe_Values.Look(ref num, "num", 0);
	}
}
