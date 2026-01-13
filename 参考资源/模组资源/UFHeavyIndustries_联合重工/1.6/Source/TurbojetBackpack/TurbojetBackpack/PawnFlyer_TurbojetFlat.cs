using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TurbojetBackpack;

public class PawnFlyer_TurbojetFlat : PawnFlyer_Turbojet
{
	private List<Pawn> hitPawns = new List<Pawn>();

	private float FlatAltitude => AltitudeLayer.Skyfaller.AltitudeFor();

	public override Vector3 DrawPos => GetLinearPosition();

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref hitPawns, "hitPawns", LookMode.Reference);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && hitPawns == null)
		{
			hitPawns = new List<Pawn>();
		}
	}

	protected override void Tick()
	{
		base.Tick();
		if (!base.Spawned || base.Map == null)
		{
			return;
		}
		Vector3 linearPosition = GetLinearPosition();
		if (apparelExtension != null && base.FlyingPawn != null)
		{
			ThingDef flightMote = apparelExtension.flightMote;
			if (flightMote != null)
			{
				float num = apparelExtension.flightSmokeScaleMin;
				if (num <= 0f)
				{
					num = 1.2f;
				}
				TurboJumpUtility.SpawnTrailMotes(base.Map, linearPosition, base.FlyingPawn.Rotation, apparelExtension, flightMote, num);
			}
		}
		if (combatMode && base.FlyingPawn != null)
		{
			CheckCollision(linearPosition);
		}
	}

	private void CheckCollision(Vector3 currentPos)
	{
		if (hitPawns == null)
		{
			hitPawns = new List<Pawn>();
		}
		IntVec3 intVec = currentPos.ToIntVec3();
		if (!intVec.InBounds(base.Map))
		{
			return;
		}
		float radius = cachedExtension?.collisionScanRadius ?? 1.5f;
		foreach (Thing item in GenRadial.RadialDistinctThingsAround(intVec, base.Map, radius, useCenter: true))
		{
			if (item is Pawn pawn && pawn != base.FlyingPawn && !hitPawns.Contains(pawn))
			{
				bool flag = pawn.HostileTo(base.FlyingPawn);
				bool flag2 = pawn.RaceProps.Animal && pawn.Faction == null;
				if (flag || flag2)
				{
					ApplyKnockback(pawn);
				}
			}
		}
	}

	private void ApplyKnockback(Pawn victim)
	{
		if (victim == null || victim.Map == null)
		{
			return;
		}
		hitPawns.Add(victim);
		float num = cachedExtension?.pushDistance ?? 10f;
		int num2 = cachedExtension?.stunAmount ?? 20;
		Vector3 normalized = (base.DestinationPos - startVec).normalized;
		IntVec3 intVec = victim.Position;
		Vector3 vector = victim.Position.ToVector3Shifted();
		for (int i = 1; i <= (int)num; i++)
		{
			Vector3 vect = vector + normalized * i;
			IntVec3 intVec2 = vect.ToIntVec3();
			if (!intVec2.InBounds(victim.Map) || !intVec2.Walkable(victim.Map))
			{
				break;
			}
			intVec = intVec2;
		}
		if (intVec != victim.Position)
		{
			victim.Position = intVec;
			victim.Notify_Teleported(endCurrentJob: true, resetTweenedPos: false);
			if (cachedExtension?.landingSound != null)
			{
				cachedExtension.landingSound.PlayOneShot(victim);
			}
			else
			{
				SoundDefOf.Pawn_Melee_Punch_HitPawn.PlayOneShot(victim);
			}
			FleckMaker.ThrowMicroSparks(victim.DrawPos, victim.Map);
			FleckMaker.ThrowDustPuff(victim.DrawPos, victim.Map, 1f);
		}
		int num3 = Mathf.Max(1, cachedExtension?.damageAmount ?? 10);
		DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, num3, 0f, -1f, base.FlyingPawn);
		dinfo.SetBodyRegion(BodyPartHeight.Middle, BodyPartDepth.Outside);
		victim.TakeDamage(dinfo);
		ApplyMeleeImpact(base.FlyingPawn, victim);
		if (!victim.DestroyedOrNull() && !victim.Dead && num2 > 0)
		{
			DamageInfo dinfo2 = new DamageInfo(DamageDefOf.Stun, num2, 0f, -1f, base.FlyingPawn);
			victim.TakeDamage(dinfo2);
		}
	}

	private Vector3 GetLinearPosition()
	{
		float num = Mathf.Max(1, ticksFlightTime);
		float t = (float)ticksFlying / num;
		Vector3 result = Vector3.Lerp(startVec, base.DestinationPos, t);
		float num2 = Mathf.Lerp(startHeight, targetHeight, t);
		result.y = FlatAltitude;
		result.z += num2;
		return result;
	}
}
