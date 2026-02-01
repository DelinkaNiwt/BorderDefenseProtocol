using RimWorld;
using Verse;

namespace NCL;

public class CompMechanicalBuilding : ThingComp
{
	private int lastCheckTick = 0;

	public override void CompTick()
	{
		base.CompTick();
		if (Find.TickManager.TicksGame > lastCheckTick + 60)
		{
			lastCheckTick = Find.TickManager.TicksGame;
			CheckAndFixFaction();
		}
	}

	private void CheckAndFixFaction()
	{
		if (parent.Faction != Faction.OfMechanoids)
		{
			parent.SetFaction(Faction.OfMechanoids);
			Log.Message("强制修正建筑 " + parent.Label + " 为机械族阵营");
		}
	}
}
