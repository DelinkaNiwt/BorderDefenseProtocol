using Verse;

namespace RimWorld;

public class CompProperties_RestartableCerebrexCore : CompProperties
{
	public float bobFrequency = 0.02f;

	public float bobAmplitude = 0.35f;

	public float zOffset = 4f;

	public float yOffset = 0.35f;

	public int restartDurationTicks = 300;

	public CompProperties_RestartableCerebrexCore()
	{
		compClass = typeof(CompRestartableCerebrexCore);
	}
}
