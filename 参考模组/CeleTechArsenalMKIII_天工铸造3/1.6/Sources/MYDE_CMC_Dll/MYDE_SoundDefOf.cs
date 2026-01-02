using RimWorld;
using Verse;

namespace MYDE_CMC_Dll;

public static class MYDE_SoundDefOf
{
	public static SoundDef PL;

	static MYDE_SoundDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(MYDE_SoundDefOf));
	}
}
