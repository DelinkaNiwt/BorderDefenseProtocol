using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_ApparelReloadable_DeployThing : CompProperties_ApparelReloadable
{
	public ThingDef thingToDeploy;

	public FleckDef deployFleck;

	public SoundDef deploySound;

	public int ai_DeployIntervalTick = 0;

	public int deployCooldown = 0;

	public CompProperties_ApparelReloadable_DeployThing()
	{
		compClass = typeof(CompApparelReloadable_DeployThing);
	}
}
