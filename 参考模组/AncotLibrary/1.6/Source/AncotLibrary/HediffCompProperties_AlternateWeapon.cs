using Verse;

namespace AncotLibrary;

public class HediffCompProperties_AlternateWeapon : HediffCompProperties
{
	public string switchLabel;

	public string switchDesc;

	public HediffCompProperties_AlternateWeapon()
	{
		compClass = typeof(HediffComp_AlternateWeapon);
	}
}
