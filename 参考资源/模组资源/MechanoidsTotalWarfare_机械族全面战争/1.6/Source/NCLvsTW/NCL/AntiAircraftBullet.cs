using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class AntiAircraftBullet : Bullet
{
	private const float COLLISION_RADIUS = 5f;

	private const float UPDATE_INTERVAL = 3f;

	private const float CRUSH_DAMAGE_RADIUS = 1.5f;

	private const int CRUSH_DAMAGE_AMOUNT = 10;

	private int lastCheckTick;

	private bool isDestroyed = false;

	private HashSet<Thing> crushedThisFrame = new HashSet<Thing>();

	protected override void Tick()
	{
		if (isDestroyed || base.Destroyed)
		{
			return;
		}
		base.Tick();
		if (!ExactPosition.InBounds(base.Map))
		{
			DestroyBullet();
			return;
		}
		crushedThisFrame.Clear();
		CheckForCrushDamage();
		if ((float)Find.TickManager.TicksGame > (float)lastCheckTick + 3f)
		{
			CheckForAircraftCollision();
			lastCheckTick = Find.TickManager.TicksGame;
		}
	}

	private void DestroyBullet()
	{
		if (!isDestroyed)
		{
			isDestroyed = true;
			Destroy();
		}
	}

	private void CheckForCrushDamage()
	{
		IntVec3 bulletCell = ExactPosition.ToIntVec3();
		List<Thing> potentialTargets = GenRadial.RadialDistinctThingsAround(bulletCell, base.Map, 1.5f, useCenter: true).ToList();
		foreach (Thing thing in potentialTargets)
		{
			if (thing != null && thing != this && !crushedThisFrame.Contains(thing) && !thing.Destroyed && thing is Pawn && thing.Faction != launcher?.Faction)
			{
				ApplyCrushDamage(thing);
				crushedThisFrame.Add(thing);
			}
		}
	}

	private void ApplyCrushDamage(Thing target)
	{
		DamageInfo crushDamage = new DamageInfo(DamageDefOf.Crush, 10f, 0f, -1f, launcher, null, def);
		target.TakeDamage(crushDamage);
		FleckMaker.ThrowDustPuffThick(target.DrawPos, target.Map, 0.5f, Color.gray);
	}

	private void CheckForAircraftCollision()
	{
		Vector3 bulletPos = ExactPosition;
		List<Thing> allFlyers = base.Map.listerThings.ThingsOfDef(ThingDef.Named("B2000Mech"));
		foreach (Thing thing in allFlyers)
		{
			if (thing is CruiseFlyer { Destroyed: false } flyer)
			{
				Vector3 flyerPos = flyer.GetCurrentDrawPosition();
				float distance = Vector3.Distance(bulletPos, flyerPos);
				if (distance < 5f)
				{
					OnAircraftHit(flyer);
					break;
				}
			}
		}
	}

	private void OnAircraftHit(CruiseFlyer flyer)
	{
		FleckMaker.ThrowSmoke(flyer.Position.ToVector3(), flyer.Map, 5f);
		flyer.Destroy();
		DestroyBullet();
		CreateCombatLog(flyer);
	}

	private void CreateCombatLog(CruiseFlyer aircraft)
	{
		BattleLogEntry_RangedImpact entry = new BattleLogEntry_RangedImpact(launcher, aircraft, intendedTarget.Thing, equipmentDef, def, null);
		Find.BattleLog.Add(entry);
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		if (hitThing == null)
		{
			DestroyBullet();
		}
		else if (hitThing is CruiseFlyer)
		{
			base.Impact(hitThing, blockedByShield);
		}
	}
}
