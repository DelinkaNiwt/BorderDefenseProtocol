using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test;

public class Projectile_CNHitLimb : Bullet
{
	private int tickcount;

	private Vector3 CurretPos(float t)
	{
		return origin + (destination - origin) * t;
	}

	protected override void DrawAt(Vector3 position, bool flip = false)
	{
		Vector3 vector = CurretPos(base.DistanceCoveredFraction - 0.01f);
		position = CurretPos(base.DistanceCoveredFraction);
		Quaternion rotation = Quaternion.LookRotation(position - vector);
		if (tickcount >= 4)
		{
			Vector3 position2 = position;
			position2.y = AltitudeLayer.Projectile.AltitudeFor();
			Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), position2, rotation, DrawMat, 0);
			Comps_PostDraw();
		}
	}

	protected override void Tick()
	{
		tickcount++;
		base.Tick();
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		bool instigatorGuilty = !(launcher is Pawn pawn) || !pawn.Drafted;
		Map map = base.Map;
		IntVec3 position = base.Position;
		base.Impact(hitThing, blockedByShield);
		BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
		Find.BattleLog.Add(battleLogEntry_RangedImpact);
		NotifyImpact(hitThing, map, position);
		if (hitThing != null)
		{
			Pawn pawn2 = hitThing as Pawn;
			if (pawn2 != null)
			{
				DamageInfo dinfo = RefDinfo(hitThing);
				hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
			}
			if (pawn2 != null && pawn2.stances != null)
			{
				pawn2.stances.stagger.Notify_BulletImpact(this);
			}
			if (def.projectile.extraDamages == null)
			{
				return;
			}
			{
				foreach (ExtraDamage extraDamage in def.projectile.extraDamages)
				{
					if (Rand.Chance(extraDamage.chance))
					{
						DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
						hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
					}
				}
				return;
			}
		}
		if (!blockedByShield)
		{
			SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map));
			if (base.Position.GetTerrain(map).takeSplashes)
			{
				FleckMaker.WaterSplash(ExactPosition, map, Mathf.Sqrt(DamageAmount) * 1f, 4f);
			}
			else
			{
				FleckMaker.Static(ExactPosition, map, FleckDefOf.ShotHit_Dirt);
			}
		}
	}

	private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
	{
		BulletImpactData impactData = new BulletImpactData
		{
			bullet = this,
			hitThing = hitThing,
			impactPosition = position
		};
		hitThing?.Notify_BulletImpactNearby(impactData);
		int num = 9;
		for (int i = 0; i < num; i++)
		{
			IntVec3 c = position + GenRadial.RadialPattern[i];
			if (!c.InBounds(map))
			{
				continue;
			}
			List<Thing> thingList = c.GetThingList(map);
			for (int j = 0; j < thingList.Count; j++)
			{
				if (thingList[j] != hitThing)
				{
					thingList[j].Notify_BulletImpactNearby(impactData);
				}
			}
		}
	}

	private DamageInfo RefDinfo(Thing hitThing)
	{
		Pawn pawn;
		bool instigatorGuilty = (pawn = launcher as Pawn) == null || !pawn.Drafted;
		float num = DamageAmount;
		if (pawn.RaceProps.FleshType == FleshTypeDefOf.Mechanoid)
		{
			num *= 0.5f;
		}
		IEnumerable<BodyPartRecord> notMissingParts = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside, BodyPartTagDefOf.MovingLimbCore);
		DamageInfo result;
		foreach (BodyPartRecord item in notMissingParts)
		{
			if (Rand.Chance(0.3f))
			{
				float partHealth = pawn.health.hediffSet.GetPartHealth(item);
				if (partHealth > (float)DamageAmount * 1.3f)
				{
					result = new DamageInfo(def.projectile.damageDef, num, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
					result.SetHitPart(item);
					return result;
				}
				break;
			}
		}
		result = new DamageInfo(def.projectile.damageDef, num, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
		return result;
	}
}
