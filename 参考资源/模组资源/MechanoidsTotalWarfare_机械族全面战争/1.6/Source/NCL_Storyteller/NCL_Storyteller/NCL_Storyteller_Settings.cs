using Verse;

namespace NCL_Storyteller;

public class NCL_Storyteller_Settings : ModSettings
{
	public const float DEFAULT_MAX_THREAT_POINTS = 100000f;

	public const int DEFAULT_MAX_PAWNS = 300;

	public float maxDefaultThreatPoints = 100000f;

	public int maxPawns = 300;

	public override void ExposeData()
	{
		Scribe_Values.Look(ref maxDefaultThreatPoints, "maxDefaultThreatPoints", 100000f);
		Scribe_Values.Look(ref maxPawns, "maxPawns", 300);
	}
}
