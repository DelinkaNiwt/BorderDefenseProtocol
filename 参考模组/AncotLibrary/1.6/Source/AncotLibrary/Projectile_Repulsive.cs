using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class Projectile_Repulsive : Projectile_Explosive
{
	private int ticksToDetonation;

	public FieldForceProjectile_Extension Props => def.GetModExtension<FieldForceProjectile_Extension>();

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref ticksToDetonation, "ticksToDetonation", 0);
	}

	protected override void Tick()
	{
		base.Tick();
		if (ticksToDetonation > 0)
		{
			ticksToDetonation--;
			if (ticksToDetonation <= 0)
			{
				DoRepulsive();
			}
		}
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		if (blockedByShield || def.projectile.explosionDelay == 0)
		{
			DoRepulsive();
			return;
		}
		landed = true;
		ticksToDetonation = def.projectile.explosionDelay;
	}

	public void DoRepulsive()
	{
		Map map = base.Map;
		float explosionRadius = def.projectile.explosionRadius;
		float distance = DamageAmount;
		List<Thing> list = new List<Thing>();
		foreach (IntVec3 item in GenRadial.RadialCellsAround(base.Position, explosionRadius, useCenter: true))
		{
			list.AddRange(item.GetThingList(map));
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] is Pawn victim && GenSight.LineOfSight(list[i].Position, base.Position, map))
			{
				ForceMovementUtility.ApplyRepulsiveForce(base.Position, victim, distance, Props.removeHediffsAffected);
			}
		}
		if (def.projectile.explosionEffect != null)
		{
			Effecter effecter = def.projectile.explosionEffect.Spawn();
			if (def.projectile.explosionEffectLifetimeTicks != 0)
			{
				map.effecterMaintainer.AddEffecterToMaintain(effecter, base.Position.ToVector3().ToIntVec3(), def.projectile.explosionEffectLifetimeTicks);
			}
			else
			{
				effecter.Trigger(new TargetInfo(base.Position, map), new TargetInfo(base.Position, map));
				effecter.Cleanup();
			}
		}
		Destroy();
	}

	public void TryToKnockBack(IntVec3 original, Thing thing, float knockBackDistance)
	{
		Vector3 vector = (thing.Position - original).ToVector3();
		vector.Normalize();
		if (vector.magnitude == 0f)
		{
			vector = Random.onUnitSphere;
		}
		IntVec3 position = thing.Position;
		for (int i = 0; (float)i < knockBackDistance; i++)
		{
			Vector3 vect = i * vector;
			IntVec3 intVec = thing.Position + vect.ToIntVec3();
			if (!intVec.InBounds(thing.Map) || !intVec.Walkable(thing.Map) || !GenSight.LineOfSight(original, intVec, thing.Map))
			{
				break;
			}
			position = intVec;
		}
		if (!position.IsValid)
		{
			return;
		}
		thing.Position = position;
		if (!(thing is Pawn pawn))
		{
			return;
		}
		if (!Props.removeHediffsAffected.NullOrEmpty())
		{
			for (int j = 0; j < Props.removeHediffsAffected.Count; j++)
			{
				Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(Props.removeHediffsAffected[j]);
				if (firstHediffOfDef != null)
				{
					pawn.health.RemoveHediff(firstHediffOfDef);
				}
			}
		}
		pawn.pather.StopDead();
		pawn.jobs.StopAll();
	}
}
