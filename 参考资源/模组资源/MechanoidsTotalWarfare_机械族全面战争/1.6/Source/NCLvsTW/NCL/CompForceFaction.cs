using RimWorld;
using Verse;

namespace NCL;

public class CompForceFaction : ThingComp
{
	private CompProperties_ForceFaction Props => (CompProperties_ForceFaction)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (!(parent is Blueprint) && !(parent is Frame))
		{
			ApplyFaction();
		}
	}

	public override void ReceiveCompSignal(string signal)
	{
		base.ReceiveCompSignal(signal);
		if (signal == "SpawnedBuilding")
		{
			ApplyFaction();
		}
	}

	private void ApplyFaction()
	{
		if (parent.Faction?.def != Props.factionDef)
		{
			Faction faction = Find.FactionManager.FirstFactionOfDef(Props.factionDef);
			if (faction != null)
			{
				parent.SetFaction(faction);
			}
		}
	}
}
