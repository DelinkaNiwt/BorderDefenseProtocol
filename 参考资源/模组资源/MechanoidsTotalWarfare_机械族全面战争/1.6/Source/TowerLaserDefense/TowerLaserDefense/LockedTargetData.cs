using Verse;

namespace TowerLaserDefense;

public class LockedTargetData : IExposable
{
	public Thing target;

	public int time;

	public LockedTargetData()
	{
	}

	public LockedTargetData(Thing target)
	{
		this.target = target;
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref time, "time", 0);
		Scribe_References.Look(ref target, "target");
	}
}
