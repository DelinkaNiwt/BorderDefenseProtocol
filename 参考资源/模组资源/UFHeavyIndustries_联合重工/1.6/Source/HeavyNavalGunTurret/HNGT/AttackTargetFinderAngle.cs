using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HNGT;

public static class AttackTargetFinderAngle
{
	private const float FriendlyFireScoreOffsetPerHumanlikeOrMechanoid = 18f;

	private const float FriendlyFireScoreOffsetPerAnimal = 7f;

	private const float FriendlyFireScoreOffsetPerNonPawn = 10f;

	private const float FriendlyFireScoreOffsetSelf = 40f;

	private static List<IAttackTarget> tmpTargets = new List<IAttackTarget>(128);

	private static List<Pair<IAttackTarget, float>> availableShootingTargets = new List<Pair<IAttackTarget, float>>();

	private static List<float> tmpTargetScores = new List<float>();

	private static List<bool> tmpCanShootAtTarget = new List<bool>();

	public static IAttackTarget BestShootTargetFromCurrentPosition(IAttackTargetSearcher searcher, TargetScanFlags flags, Vector3 angle, Predicate<Thing> validator = null, float minDistance = 0f, float maxDistance = 9999f)
	{
		Verb currentEffectiveVerb = searcher.CurrentEffectiveVerb;
		if (currentEffectiveVerb == null)
		{
			Log.Error("BestShootTargetFromCurrentPosition with " + searcher.ToStringSafe() + "Has no attack verb.");
			return null;
		}
		float minDist = Mathf.Max(minDistance, currentEffectiveVerb.verbProps.minRange);
		float maxDist = Mathf.Min(maxDistance, currentEffectiveVerb.verbProps.range);
		return BestAttackTarget(searcher, flags, angle, validator, minDist, maxDist, default(IntVec3), float.MaxValue, canTakeTargetsCloserThanEffectiveMinRange: false);
	}

	public static IAttackTarget BestAttackTarget(IAttackTargetSearcher searcher, TargetScanFlags flags, Vector3 angle, Predicate<Thing> validator = null, float minDist = 0f, float maxDist = 9999f, IntVec3 locus = default(IntVec3), float maxTravelRadiusFromLocus = float.MaxValue, bool canTakeTargetsCloserThanEffectiveMinRange = true)
	{
		Thing searcherThing = searcher.Thing;
		Verb verb = searcher.CurrentEffectiveVerb;
		if (verb == null)
		{
			Log.Error("BestAttackTarget with " + searcher.ToStringSafe() + "Has no attack verb.");
			return null;
		}
		float minDistSquared = minDist * minDist;
		float num = maxTravelRadiusFromLocus + verb.verbProps.range;
		float maxLocusDistSquared = num * num;
		Predicate<IntVec3> losValidator = null;
		if ((flags & TargetScanFlags.LOSBlockableByGas) > TargetScanFlags.None)
		{
			losValidator = (IntVec3 vec3) => !vec3.AnyGas(searcherThing.Map, GasType.BlindSmoke);
		}
		tmpTargets.Clear();
		tmpTargets.AddRange(searcherThing.Map.attackTargetsCache.GetPotentialTargetsFor(searcher));
		tmpTargets.RemoveAll((IAttackTarget t) => ShouldIgnoreNoncombatant(searcherThing, t, flags));
		bool flag = false;
		for (int num2 = 0; num2 < tmpTargets.Count; num2++)
		{
			IAttackTarget attackTarget = tmpTargets[num2];
			if (attackTarget.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) && InnerValidator(attackTarget) && CanShootAtFromCurrentPosition(attackTarget, searcher, verb))
			{
				flag = true;
				break;
			}
		}
		IAttackTarget result;
		if (flag)
		{
			tmpTargets.RemoveAll((IAttackTarget x) => !x.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) || !InnerValidator(x));
			result = GetRandomShootingTargetByScore(tmpTargets, searcher, verb, angle);
		}
		else
		{
			bool flag2 = (flags & TargetScanFlags.NeedReachableIfCantHitFromMyPos) > TargetScanFlags.None;
			bool flag3 = (flags & TargetScanFlags.NeedReachable) > TargetScanFlags.None;
			result = (IAttackTarget)GenClosest.ClosestThing_Global(validator: (!(!flag2 || flag3)) ? ((Predicate<Thing>)((Thing t) => InnerValidator((IAttackTarget)t) && CanShootAtFromCurrentPosition((IAttackTarget)t, searcher, verb))) : ((Predicate<Thing>)((Thing t) => InnerValidator((IAttackTarget)t))), center: searcherThing.Position, searchSet: tmpTargets, maxDistance: maxDist);
		}
		tmpTargets.Clear();
		return result;
		bool InnerValidator(IAttackTarget target)
		{
			Thing thing = target.Thing;
			if (target == searcher)
			{
				return false;
			}
			if (minDistSquared > 0f && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < minDistSquared)
			{
				return false;
			}
			if (!canTakeTargetsCloserThanEffectiveMinRange)
			{
				float num3 = verb.verbProps.EffectiveMinRange(thing, searcherThing);
				if (num3 > 0f && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < num3 * num3)
				{
					return false;
				}
			}
			if (maxTravelRadiusFromLocus < 9999f && (float)(thing.Position - locus).LengthHorizontalSquared > maxLocusDistSquared)
			{
				return false;
			}
			if (!searcherThing.HostileTo(thing))
			{
				return false;
			}
			if (validator != null && !validator(thing))
			{
				return false;
			}
			if ((flags & TargetScanFlags.NeedNotUnderThickRoof) != TargetScanFlags.None)
			{
				RoofDef roof = thing.Position.GetRoof(thing.Map);
				if (roof != null && roof.isThickRoof)
				{
					return false;
				}
			}
			if ((flags & TargetScanFlags.NeedLOSToAll) != TargetScanFlags.None)
			{
				if (losValidator != null && (!losValidator(searcherThing.Position) || !losValidator(thing.Position)))
				{
					return false;
				}
				if (!searcherThing.CanSee(thing))
				{
					if (target is Pawn)
					{
						if ((flags & TargetScanFlags.NeedLOSToPawns) != TargetScanFlags.None)
						{
							return false;
						}
					}
					else if ((flags & TargetScanFlags.NeedLOSToNonPawns) != TargetScanFlags.None)
					{
						return false;
					}
				}
			}
			if (((flags & TargetScanFlags.NeedThreat) != TargetScanFlags.None || (flags & TargetScanFlags.NeedAutoTargetable) != TargetScanFlags.None) && target.ThreatDisabled(searcher))
			{
				return false;
			}
			if ((flags & TargetScanFlags.NeedAutoTargetable) != TargetScanFlags.None && !AttackTargetFinder.IsAutoTargetable(target))
			{
				return false;
			}
			if ((flags & TargetScanFlags.NeedActiveThreat) != TargetScanFlags.None && !GenHostility.IsActiveThreatTo(target, searcher.Thing.Faction))
			{
				return false;
			}
			Pawn pawn = target as Pawn;
			if ((flags & TargetScanFlags.NeedNonBurning) != TargetScanFlags.None && thing.IsBurning())
			{
				return false;
			}
			if (searcherThing.def.race != null && (int)searcherThing.def.race.intelligence >= 2)
			{
				CompExplosive compExplosive = thing.TryGetComp<CompExplosive>();
				if (compExplosive != null && compExplosive.wickStarted)
				{
					return false;
				}
			}
			if (!thing.Position.InHorDistOf(searcherThing.Position, maxDist))
			{
				return false;
			}
			if (!canTakeTargetsCloserThanEffectiveMinRange && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < minDistSquared)
			{
				return false;
			}
			if (locus.IsValid && (float)(locus - thing.Position).LengthHorizontalSquared > maxLocusDistSquared)
			{
				return false;
			}
			if (validator != null && !validator(thing))
			{
				return false;
			}
			return true;
		}
	}

	private static bool ShouldIgnoreNoncombatant(Thing searcherThing, IAttackTarget target, TargetScanFlags flags)
	{
		if (!(target is Pawn pawn))
		{
			return false;
		}
		if (pawn.IsCombatant())
		{
			return false;
		}
		if ((flags & TargetScanFlags.IgnoreNonCombatants) > TargetScanFlags.None)
		{
			return true;
		}
		return !GenSight.LineOfSightToThing(searcherThing.Position, pawn, searcherThing.Map);
	}

	private static bool CanShootAtFromCurrentPosition(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
	{
		return verb?.CanHitTargetFrom(searcher.Thing.Position, target.Thing) ?? false;
	}

	private static IAttackTarget GetRandomShootingTargetByScore(List<IAttackTarget> targets, IAttackTargetSearcher searcher, Verb verb, Vector3 angle)
	{
		List<Pair<IAttackTarget, float>> availableShootingTargetsByScore = GetAvailableShootingTargetsByScore(targets, searcher, verb, angle);
		if (availableShootingTargetsByScore.TryRandomElementByWeight((Pair<IAttackTarget, float> x) => x.Second, out var result))
		{
			return result.First;
		}
		return null;
	}

	private static List<Pair<IAttackTarget, float>> GetAvailableShootingTargetsByScore(List<IAttackTarget> rawTargets, IAttackTargetSearcher searcher, Verb verb, Vector3 angle)
	{
		availableShootingTargets.Clear();
		if (rawTargets.Count == 0)
		{
			return availableShootingTargets;
		}
		tmpTargetScores.Clear();
		tmpCanShootAtTarget.Clear();
		float num = float.MinValue;
		IAttackTarget attackTarget = null;
		for (int i = 0; i < rawTargets.Count; i++)
		{
			tmpTargetScores.Add(float.MinValue);
			tmpCanShootAtTarget.Add(item: false);
			if (rawTargets[i] == searcher)
			{
				continue;
			}
			bool flag = CanShootAtFromCurrentPosition(rawTargets[i], searcher, verb);
			tmpCanShootAtTarget[i] = flag;
			if (flag)
			{
				float shootingTargetScore = GetShootingTargetScore(rawTargets[i], searcher, verb, angle);
				tmpTargetScores[i] = shootingTargetScore;
				if (attackTarget == null || shootingTargetScore > num)
				{
					attackTarget = rawTargets[i];
					num = shootingTargetScore;
				}
			}
		}
		for (int j = 0; j < rawTargets.Count; j++)
		{
			if (rawTargets[j] != searcher && tmpCanShootAtTarget[j])
			{
				availableShootingTargets.Add(new Pair<IAttackTarget, float>(rawTargets[j], tmpTargetScores[j]));
			}
		}
		return availableShootingTargets;
	}

	private static float GetShootingTargetScore(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb, Vector3 angle)
	{
		float num = 60f;
		float lengthHorizontal = (target.Thing.Position - searcher.Thing.Position).LengthHorizontal;
		num -= Mathf.Min(lengthHorizontal, 40f);
		if (target.TargetCurrentlyAimingAt == searcher.Thing)
		{
			num += 10f;
		}
		if (searcher.LastAttackedTarget == target.Thing && Find.TickManager.TicksGame - searcher.LastAttackTargetTick <= 300)
		{
			num += 40f;
		}
		float num2 = CoverUtility.CalculateOverallBlockChance(target.Thing.Position, searcher.Thing.Position, searcher.Thing.Map);
		num -= num2 * 10f;
		if (target is Pawn pawn)
		{
			num -= NonCombatantScore(pawn);
			if (verb.verbProps.ai_TargetHasRangedAttackScoreOffset != 0f && pawn.CurrentEffectiveVerb != null && pawn.CurrentEffectiveVerb.verbProps.Ranged)
			{
				num += verb.verbProps.ai_TargetHasRangedAttackScoreOffset;
			}
			if (pawn.Downed)
			{
				num -= 50f;
			}
		}
		num += FriendlyFireBlastRadiusTargetScoreOffset(target, searcher, verb);
		num += FriendlyFireConeTargetScoreOffset(target, searcher, verb);
		Vector3 to = (target.Thing.DrawPos - searcher.Thing.DrawPos).Yto0();
		float num3 = Vector3.Angle(angle, to);
		if (num3 < 0.1f)
		{
			num3 = 0.1f;
		}
		float a = num * target.TargetPriorityFactor / num3;
		return Mathf.Max(a, 0.01f);
	}

	private static float NonCombatantScore(Thing target)
	{
		if (!(target is Pawn pawn))
		{
			return 0f;
		}
		if (!pawn.IsCombatant())
		{
			return 50f;
		}
		if (pawn.DevelopmentalStage.Juvenile())
		{
			return 25f;
		}
		return 0f;
	}

	private static float FriendlyFireBlastRadiusTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
	{
		if (verb.verbProps.ai_AvoidFriendlyFireRadius <= 0f)
		{
			return 0f;
		}
		Map map = target.Thing.Map;
		IntVec3 position = target.Thing.Position;
		int num = GenRadial.NumCellsInRadius(verb.verbProps.ai_AvoidFriendlyFireRadius);
		float num2 = 0f;
		for (int i = 0; i < num; i++)
		{
			IntVec3 intVec = position + GenRadial.RadialPattern[i];
			if (!intVec.InBounds(map))
			{
				continue;
			}
			bool flag = true;
			List<Thing> thingList = intVec.GetThingList(map);
			for (int j = 0; j < thingList.Count; j++)
			{
				Thing thing = thingList[j];
				if (!(thing is IAttackTarget) || thing == target)
				{
					continue;
				}
				if (flag)
				{
					if (!GenSight.LineOfSight(position, intVec, map, skipFirstCell: true))
					{
						break;
					}
					flag = false;
				}
				float num3 = ((thing == searcher) ? 40f : ((!(thing is Pawn)) ? 10f : ((!thing.def.race.Animal) ? 18f : 7f)));
				num2 = (searcher.Thing.HostileTo(thing) ? (num2 + num3 * 0.6f) : (num2 - num3));
			}
		}
		return num2;
	}

	private static float FriendlyFireConeTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
	{
		if (!(searcher.Thing is Pawn pawn))
		{
			return 0f;
		}
		if ((int)pawn.RaceProps.intelligence < 1)
		{
			return 0f;
		}
		if (pawn.RaceProps.IsMechanoid)
		{
			return 0f;
		}
		if (!(verb is Verb_Shoot verb_Shoot))
		{
			return 0f;
		}
		ThingDef defaultProjectile = verb_Shoot.verbProps.defaultProjectile;
		if (defaultProjectile == null)
		{
			return 0f;
		}
		if (defaultProjectile.projectile.flyOverhead)
		{
			return 0f;
		}
		Map map = pawn.Map;
		ShotReport report = ShotReport.HitReportFor(pawn, verb, (Thing)target);
		float radius = Mathf.Max(VerbUtility.CalculateAdjustedForcedMiss(verb.verbProps.ForcedMissRadius, report.ShootLine.Dest - report.ShootLine.Source), 1.5f);
		IEnumerable<IntVec3> source = (from dest in GenRadial.RadialCellsAround(report.ShootLine.Dest, radius, useCenter: true)
			where dest.InBounds(map)
			select new ShootLine(report.ShootLine.Source, dest)).SelectMany(delegate(ShootLine line)
		{
			ShootLine shootLine = line;
			IEnumerable<IntVec3> lhs = shootLine.Points();
			shootLine = line;
			return lhs.Concat(shootLine.Dest).TakeWhile((IntVec3 pos) => pos.CanBeSeenOverFast(map));
		}, (ShootLine line, IntVec3 pos) => pos);
		source = source.Distinct();
		float num = 0f;
		foreach (IntVec3 item in source)
		{
			float num2 = VerbUtility.InterceptChanceFactorFromDistance(report.ShootLine.Source.ToVector3Shifted(), item);
			if (num2 <= 0f)
			{
				continue;
			}
			List<Thing> thingList = item.GetThingList(map);
			for (int num3 = 0; num3 < thingList.Count; num3++)
			{
				Thing thing = thingList[num3];
				if (thing is IAttackTarget && thing != target)
				{
					float num4 = ((thing == searcher) ? 40f : ((!(thing is Pawn)) ? 10f : ((!thing.def.race.Animal) ? 18f : 7f)));
					num4 *= num2;
					num4 = (searcher.Thing.HostileTo(thing) ? (num4 * 0.6f) : (0f - num4));
					num += num4;
				}
			}
		}
		return num;
	}
}
