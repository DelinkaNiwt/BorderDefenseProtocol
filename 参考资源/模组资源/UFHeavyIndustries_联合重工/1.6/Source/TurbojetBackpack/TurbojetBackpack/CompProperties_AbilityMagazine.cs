using RimWorld;
using Verse;

namespace TurbojetBackpack;

public class CompProperties_AbilityMagazine : CompProperties_AbilityEffect
{
	public int maxCharges = 3;

	public int reloadTicks = 600;

	public SoundDef soundReload;

	public CompProperties_AbilityMagazine()
	{
		compClass = typeof(CompAbility_Magazine);
	}
}
