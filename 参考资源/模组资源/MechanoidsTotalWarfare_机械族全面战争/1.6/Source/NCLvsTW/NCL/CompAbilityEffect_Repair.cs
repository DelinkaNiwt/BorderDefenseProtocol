using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NCL;

public class CompAbilityEffect_Repair : CompAbilityEffect
{
	private static List<Hediff> tmpHediffs = new List<Hediff>();

	public new CompProperties_RepairAbility Props => props as CompProperties_RepairAbility;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		RepairSelf(parent.pawn);
	}

	public override void Apply(GlobalTargetInfo target)
	{
		Apply(null, null);
	}

	public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
	{
		return true;
	}

	public override bool CanApplyOn(GlobalTargetInfo target)
	{
		return CanApplyOn(null, null);
	}

	private void RepairSelf(Pawn pawn)
	{
		tmpHediffs.Clear();
		tmpHediffs.AddRange(pawn.health.hediffSet.hediffs);
		tmpHediffs.SortBy((Hediff injury) => 0f - injury.Severity);
		for (int num = 0; num < tmpHediffs.Count && num < 10; num++)
		{
			Hediff hediff = tmpHediffs[num];
			if (hediff != null && (hediff is Hediff_Injury || hediff is Hediff_MissingPart))
			{
				pawn.health.RemoveHediff(hediff);
			}
		}
		tmpHediffs.Clear();
	}

	public bool CanTargetNow()
	{
		Pawn pawn = parent.pawn;
		List<Pawn> list = pawn.Map.mapPawns.AllPawns.FindAll((Pawn p) => p.Faction != null && p.Faction.HostileTo(pawn.Faction));
		list.SortBy((Pawn m) => m.Position.DistanceTo(pawn.Position));
		bool result;
		if (list[0].Position.DistanceTo(pawn.Position) <= 18.9f)
		{
			result = false;
		}
		else
		{
			List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
			for (int i = 0; i < hediffs.Count; i++)
			{
				Hediff_Injury hediff_Injury = hediffs[i] as Hediff_Injury;
				if (hediffs[i] is Hediff_MissingPart || hediff_Injury != null)
				{
					return true;
				}
			}
			result = false;
		}
		return result;
	}

	public override bool GizmoDisabled(out string reason)
	{
		reason = null;
		return false;
	}
}
