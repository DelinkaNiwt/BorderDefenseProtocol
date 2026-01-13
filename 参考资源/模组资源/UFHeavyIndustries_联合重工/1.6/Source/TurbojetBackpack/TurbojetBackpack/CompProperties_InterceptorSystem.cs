using Verse;

namespace TurbojetBackpack;

public class CompProperties_InterceptorSystem : CompProperties
{
	public int checkInterval = 10;

	public ThingDef interceptorProjectileDef;

	public int burstCount = 1;

	public SoundDef launchSound;

	public string uiIconPath;

	public CompProperties_InterceptorSystem()
	{
		compClass = typeof(CompInterceptorSystem);
	}
}
