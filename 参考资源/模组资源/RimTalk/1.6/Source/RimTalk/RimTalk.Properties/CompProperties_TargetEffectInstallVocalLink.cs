using Verse;

namespace RimTalk.Properties;

public class CompProperties_TargetEffectInstallVocalLink : CompProperties
{
	public HediffDef hediffDef;

	public SoundDef soundOnUsed;

	public CompProperties_TargetEffectInstallVocalLink()
	{
		compClass = typeof(CompTargetEffect_InstallVocalLink);
	}
}
