using Verse;

namespace Milira;

public class CompProperties_SendSignalOnMotion_Milira : CompProperties
{
	public bool triggerOnPawnInRoom;

	public float radius;

	public int enableAfterTicks;

	public bool onlyHumanlike;

	public bool triggeredBySkipPsycasts;

	public string signalTag;

	public CompProperties_SendSignalOnMotion_Milira()
	{
		compClass = typeof(CompSendSignalOnMotion_Milira);
	}
}
