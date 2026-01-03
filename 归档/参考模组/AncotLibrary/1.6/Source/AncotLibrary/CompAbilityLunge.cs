using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class CompAbilityLunge : CompAbilityShiftForward
{
	public new CompProperties_AbilityLunge Props => (CompProperties_AbilityLunge)props;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		List<Thing> list = new List<Thing>();
		foreach (IntVec3 item in AffectCells(target))
		{
			if (item.InBounds(base.Caster.Map))
			{
				list.AddRange(item.GetThingList(base.Caster.Map));
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] is Pawn pawn && IsPawnAffected(pawn))
			{
				AncotUtility.DoDamage(pawn, Props.damageDef, Props.damageAmount, Props.armorPenetration, base.Caster);
			}
		}
	}

	public virtual bool IsPawnAffected(Pawn pawn)
	{
		if (Props.applyOnAllyOnly && pawn.Faction != base.Caster.Faction)
		{
			return false;
		}
		if (!Props.applyOnAlly && pawn.Faction == base.Caster.Faction)
		{
			return false;
		}
		if (!Props.applyOnMech && pawn.RaceProps.IsMechanoid)
		{
			return false;
		}
		if (Props.ignoreCaster && pawn == base.Caster)
		{
			return false;
		}
		return true;
	}
}
