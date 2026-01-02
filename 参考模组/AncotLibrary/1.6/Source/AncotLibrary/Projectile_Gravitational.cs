using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class Projectile_Gravitational : Projectile_Explosive
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
				ForceMovementUtility.ApplyGravitationalForce(base.Position, victim, distance, Props.removeHediffsAffected);
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
}
