using System.Collections.Generic;
using RimWorld;
using VEF.Abilities;
using VEF.Weapons;
using Verse;

namespace VanillaPsycastsExpanded;

public class IceBreatheProjectile : ExpandableProjectile
{
	public Ability ability;

	public override void DoDamage(IntVec3 pos)
	{
		((ExpandableProjectile)this).DoDamage(pos);
		try
		{
			if (!(pos != ((Projectile)this).launcher.Position) || ((Projectile)this).launcher.Map == null || !pos.InBounds(((Projectile)this).launcher.Map))
			{
				return;
			}
			((Thing)this).Map.snowGrid.AddDepth(pos, 0.5f);
			List<Thing> list = ((Projectile)this).launcher.Map.thingGrid.ThingsListAt(pos);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				if (((ExpandableProjectile)this).IsDamagable(list[num]))
				{
					base.customImpact = true;
					((ExpandableProjectile)this).Impact(list[num], false);
					base.customImpact = false;
					if (list[num] is Pawn pawn)
					{
						float sevOffset = 0.5f / pawn.Position.DistanceTo(((Projectile)this).launcher.Position);
						if (pawn.CanReceiveHypothermia(out var hypothermiaHediff))
						{
							HealthUtility.AdjustSeverity(pawn, hypothermiaHediff, sevOffset);
						}
						HealthUtility.AdjustSeverity(pawn, VPE_DefOf.VFEP_HypothermicSlowdown, sevOffset);
						if (ability.def.goodwillImpact != 0)
						{
							ability.ApplyGoodwillImpact(pawn);
						}
					}
				}
			}
		}
		catch
		{
		}
	}

	public override bool IsDamagable(Thing t)
	{
		if (!(t is Pawn) || !((ExpandableProjectile)this).IsDamagable(t))
		{
			return t.def == ThingDefOf.Fire;
		}
		return true;
	}

	public override void ExposeData()
	{
		((ExpandableProjectile)this).ExposeData();
		Scribe_References.Look<Ability>(ref ability, "ability", saveDestroyedThings: false);
	}
}
