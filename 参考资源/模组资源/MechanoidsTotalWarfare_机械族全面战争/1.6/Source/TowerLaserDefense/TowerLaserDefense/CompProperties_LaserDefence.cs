using Verse;

namespace TowerLaserDefense;

public class CompProperties_LaserDefence : CompProperties
{
	public LaserDefenceProperties laserDefenceProperties;

	public CompProperties_LaserDefence()
	{
		compClass = typeof(CompLaserDefence);
	}
}
