using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace GD3
{
    public class Building_AntiAirTurret : Building_TurretGun
    {
		public override LocalTargetInfo TryFindNewTarget()
		{
			return TryFindNewTargetStatic(this, AttackVerb);
		}

		public static LocalTargetInfo TryFindNewTargetStatic(Building_TurretGun thing, Verb verb)
		{
			IAttackTargetSearcher attackTargetSearcher = thing;
			Faction faction = attackTargetSearcher.Thing.Faction;
			float range = verb.EffectiveRange;
			if (Rand.Value < 0.5f && thing.Map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile).Where(delegate (Thing x)
			{
				float num = verb.verbProps.EffectiveMinRange(x, thing);
				float num2 = x.Position.DistanceToSquared(thing.Position);
				return x is Projectile proj && x.def.projectile.flyOverhead && x.def.projectile.explosionRadius > 0 && (proj.Launcher == null || proj.Launcher.HostileTo(Faction.OfPlayer)) && num2 > num * num && num2 < range * range;
			}).TryRandomElement(out var result))
			{
				return result;
			}
			TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
			if (!verb.ProjectileFliesOverhead())
			{
				targetScanFlags |= TargetScanFlags.NeedLOSToAll;
				targetScanFlags |= TargetScanFlags.LOSBlockableByGas;
			}
			if (verb.IsIncendiary_Ranged())
			{
				targetScanFlags |= TargetScanFlags.NeedNonBurning;
			}
			targetScanFlags |= TargetScanFlags.NeedNotUnderThickRoof;
			return AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, IsValidTargetStatic) as Thing;
			bool IsValidTargetStatic(Thing t)
			{
				if (t is Pawn pawn)
				{
					if (thing.Faction == Faction.OfPlayer && pawn.IsPrisoner)
					{
						return false;
					}
					if (verb.ProjectileFliesOverhead())
					{
						RoofDef roofDef = thing.Map.roofGrid.RoofAt(t.Position);
						if (roofDef != null && roofDef.isThickRoof)
						{
							return false;
						}
					}
					CompMannable mannableComp = thing.TryGetComp<CompMannable>();
					if (mannableComp == null)
					{
						return !GenAI.MachinesLike(thing.Faction, pawn) && pawn.Flying;
					}
					if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
					{
						return false;
					}
					if (pawn.Flying)
					{
						return true;
					}
				}
				return false;
			}
		}
	}
}
