using Verse;

namespace TOT_DLL_test;

public class CompProperties_WeaponRenderStatic : CompProperties
{
	public string TexturePath;

	public string TexturePath_Camo;

	public CompProperties_WeaponRenderStatic()
	{
		compClass = typeof(Comp_WeaponRenderStatic);
	}
}
