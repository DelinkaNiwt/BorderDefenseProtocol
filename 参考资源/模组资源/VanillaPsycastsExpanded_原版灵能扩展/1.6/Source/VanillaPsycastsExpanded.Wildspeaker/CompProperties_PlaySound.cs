using Verse;

namespace VanillaPsycastsExpanded.Wildspeaker;

public class CompProperties_PlaySound : CompProperties
{
	public SoundDef sustainer;

	public SoundDef endSound;

	public CompProperties_PlaySound()
	{
		compClass = typeof(Comp_PlaySound);
	}
}
