using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(GenExplosion), "DoExplosion")]
public static class Patch_GenExplosion_DoExplosion
{
	public static bool Prefix(IntVec3 center, Map map, DamageDef damType, Thing instigator)
	{
		if (map == null)
		{
			return true;
		}
		if (damType == DamageDefOf.EMP && instigator is ThingWithComps thing && thing.TryGetComp<Comp_AbsoluteTerrorField>() != null)
		{
			return true;
		}
		List<Comp_AbsoluteTerrorField> activeFields = ATFieldManager.Get(map).activeFields;
		if (activeFields == null || activeFields.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < activeFields.Count; i++)
		{
			Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = activeFields[i];
			if (comp_AbsoluteTerrorField.CheckExplosionIntercept(center, damType))
			{
				return false;
			}
		}
		return true;
	}
}
