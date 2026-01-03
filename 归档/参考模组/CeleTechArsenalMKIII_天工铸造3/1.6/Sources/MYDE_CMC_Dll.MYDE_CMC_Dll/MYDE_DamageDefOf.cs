using RimWorld;
using Verse;

namespace MYDE_CMC_Dll.MYDE_CMC_Dll;

[DefOf]
public static class MYDE_DamageDefOf
{
	public static DamageDef InfernoBeam;

	static MYDE_DamageDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(MYDE_DamageDefOf));
	}
}
