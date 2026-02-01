using RimWorld;
using RimWorld.Planet;
using VanillaPsycastsExpanded.Skipmaster;

namespace VanillaPsycastsExpanded.Nightstalker;

public class Ability_WorldTeleportNight : Ability_WorldTeleport
{
	public override bool CanHitTargetTile(GlobalTargetInfo target)
	{
		float num = GenLocalDate.HourFloat(target.Tile);
		if (num < 6f || num > 18f)
		{
			return true;
		}
		return false;
	}
}
