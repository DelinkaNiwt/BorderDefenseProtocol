using System.Collections.Generic;
using Verse;

namespace TOT_DLL_test;

internal class CompPawnEquipmentGizmo : ThingComp
{
	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		ThingWithComps thingWithComps = (parent as Pawn)?.equipment.Primary;
		if (thingWithComps == null || thingWithComps.AllComps.NullOrEmpty())
		{
			yield break;
		}
		foreach (ThingComp thingComp in thingWithComps.AllComps)
		{
			if (!(thingComp is CompSecondaryVerb_Rework compSecondaryVerb_Rework))
			{
				continue;
			}
			foreach (Gizmo item in compSecondaryVerb_Rework.CompGetGizmosExtra())
			{
				yield return item;
			}
			yield break;
		}
	}
}
