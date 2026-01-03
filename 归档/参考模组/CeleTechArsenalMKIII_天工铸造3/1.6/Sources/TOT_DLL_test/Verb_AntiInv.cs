using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class Verb_AntiInv : Verb
{
	private VerbProp_Anti_Inv Props => (VerbProp_Anti_Inv)verbProps;

	protected override bool TryCastShot()
	{
		IntVec3 center = IntVec3.FromVector3(caster.DrawPos);
		IEnumerable<IntVec3> enumerable = GenRadial.RadialCellsAround(center, Props.MaxRange, useCenter: true);
		foreach (IntVec3 item in enumerable)
		{
			Pawn firstPawn = item.GetFirstPawn(caster.Map);
			if (firstPawn == null || firstPawn.Faction == null || !firstPawn.Faction.HostileTo(caster.Faction))
			{
				continue;
			}
			List<Hediff> resultHediffs = new List<Hediff>();
			firstPawn.health.hediffSet.GetHediffs(ref resultHediffs, (Hediff x) => x.TryGetComp<HediffComp_Invisibility>() != null);
			if (resultHediffs.Count > 0)
			{
				for (int num = 0; num <= resultHediffs.Count; num++)
				{
					firstPawn.health.RemoveHediff(resultHediffs[num]);
				}
				resultHediffs.Clear();
			}
		}
		base.ReloadableCompSource?.UsedOnce();
		return true;
	}
}
