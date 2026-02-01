using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded.Wildspeaker;

public class Comp_PlaySound : ThingComp
{
	private Sustainer sustainer;

	private IntVec3 cell;

	public CompProperties_PlaySound Props => (CompProperties_PlaySound)props;

	public override void CompTick()
	{
		base.CompTick();
		if (parent.Spawned)
		{
			if (sustainer == null || sustainer.Ended)
			{
				sustainer = Props.sustainer.TrySpawnSustainer(SoundInfo.InMap(parent, MaintenanceType.PerTick));
			}
			if (Props.sustainer != null)
			{
				sustainer.Maintain();
			}
		}
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		cell = parent.Position;
	}

	public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
	{
		base.PostDeSpawn(map, mode);
		if (Props.sustainer != null && !sustainer.Ended)
		{
			sustainer?.End();
		}
		Props.endSound?.PlayOneShot(new TargetInfo(cell, map));
	}
}
