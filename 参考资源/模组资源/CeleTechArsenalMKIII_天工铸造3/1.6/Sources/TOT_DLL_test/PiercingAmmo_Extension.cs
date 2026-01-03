using Verse;

namespace TOT_DLL_test;

public class PiercingAmmo_Extension : DefModExtension
{
	public int penetratingPower = 255;

	public bool reachMaxRangeAlways;

	public float? rangeOverride = null;

	public float minDistanceToAffectAlly = 4.9f;

	public float minDistanceToAffectAny = 1.9f;

	public int penetratingPowerCostByShield = 120;

	public bool alwaysHitStandingEnemy = false;
}
