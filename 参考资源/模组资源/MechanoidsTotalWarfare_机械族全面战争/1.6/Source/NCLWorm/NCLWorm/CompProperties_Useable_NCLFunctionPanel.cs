using RimWorld;

namespace NCLWorm;

public class CompProperties_Useable_NCLFunctionPanel : CompProperties_UseEffect
{
	public NCLCallDef callDef;

	public CompProperties_Useable_NCLFunctionPanel()
	{
		compClass = typeof(CompUseEffect_NCLFunctionPanel);
	}
}
