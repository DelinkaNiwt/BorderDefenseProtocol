using Verse;

namespace TOT_DLL_test;

public class CompProperties_FloatGunRework : CompProperties
{
	public ThingDef turretDef;

	public GraphicData FloatingGunGraphicData;

	public int BatteryLifeTick = 7200;

	public int BatteryRecoverPerSec = 180;

	public SimpleColor RadiusColor = SimpleColor.White;

	public string saveKeysPrefix;

	public float ChargingSpeedMutiplier = 1f;

	public CompProperties_FloatGunRework()
	{
		compClass = typeof(Comp_FloatingGunRework);
	}
}
