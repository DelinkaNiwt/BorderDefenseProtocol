using Verse;

namespace AncotLibrary;

public class VerbProperties_Custom : VerbProperties
{
	public int bulletPerBurstShot = 1;

	public bool fullChargeShot = false;

	public int chargeCostPerBurstShot = 1;

	public bool disableWhenChargeEmpty = false;

	public bool sustainIgnoreBurstCount = false;
}
