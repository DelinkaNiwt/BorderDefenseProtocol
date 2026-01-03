using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CompAbilityEffect_AntiInv : CompAbilityEffect
{
	private new CompProperties_AntiInv Props => (CompProperties_AntiInv)props;

	private IntVec3 Centre => IntVec3.FromVector3(parent.pawn.DrawPos);

	public override void OnGizmoUpdate()
	{
		GenDraw.DrawRadiusRing(Centre, Props.SpotRange, new Color(0.2f, 0.6f, 1f, 1f));
		GenDraw.DrawRadiusRing(Centre, Props.DetectRange, new Color(0.2f, 0.6f, 1f, 0.5f));
	}

	public override IEnumerable<PreCastAction> GetPreCastActions()
	{
		yield return new PreCastAction
		{
			action = delegate
			{
				Pawn pawn = parent.pawn;
			},
			ticksAwayFromCast = 5
		};
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		target = parent.pawn;
		Spot();
		base.Apply(target, dest);
	}

	private void Spot()
	{
		IntVec3 centre = Centre;
		IEnumerable<IntVec3> enumerable = GenRadial.RadialCellsAround(centre, Props.SpotRange, useCenter: true);
		foreach (IntVec3 item in enumerable)
		{
			Pawn firstPawn = item.GetFirstPawn(parent.pawn.Map);
			if (firstPawn == null || firstPawn.Faction == null || !firstPawn.Faction.HostileTo(parent.pawn.Faction))
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
			}
		}
	}
}
