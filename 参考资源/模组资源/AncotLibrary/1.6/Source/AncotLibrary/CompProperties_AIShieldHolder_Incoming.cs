using Verse;

namespace AncotLibrary;

public class CompProperties_AIShieldHolder_Incoming : CompProperties
{
	public int shieldDurationTick = 600;

	public bool refreshNextIncoming = false;

	public CompProperties_AIShieldHolder_Incoming()
	{
		compClass = typeof(CompAIShieldHolder_Incoming);
	}
}
