using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test;

public class Projectile_XM : Bullet
{
	public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_GunTail_Blue");

	public int Fleck_MakeFleckTickMax = 1;

	public IntRange Fleck_MakeFleckNum = new IntRange(1, 1);

	public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);

	public FloatRange Fleck_Scale = new FloatRange(1.6f, 1.7f);

	public FloatRange Fleck_Speed = new FloatRange(5f, 7f);

	public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);

	public int Fleck_MakeFleckTick;

	public int CurrentTick;

	public int tickcount;

	public Vector3 lastposition;

	private Vector3 CurretPos(float t)
	{
		return origin + (destination - origin) * t;
	}

	protected override void DrawAt(Vector3 position, bool flip = false)
	{
		Vector3 vector = CurretPos(base.DistanceCoveredFraction - 0.01f);
		position = CurretPos(base.DistanceCoveredFraction);
		Quaternion rotation = Quaternion.LookRotation(position - vector);
		if (tickcount > 8)
		{
			Vector3 position2 = lastposition;
			position2.y = AltitudeLayer.Projectile.AltitudeFor();
			Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), position2, rotation, DrawMat, 0);
			Comps_PostDraw();
		}
	}

	public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
	{
		lastposition = origin;
		tickcount = 0;
		base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
	}

	protected override void Tick()
	{
		tickcount++;
		if (intendedTarget.Thing != null)
		{
			destination = intendedTarget.Thing.DrawPos;
		}
		Fleck_MakeFleckTick++;
		if (Fleck_MakeFleckTick >= Fleck_MakeFleckTickMax && tickcount >= 8)
		{
			Fleck_MakeFleckTick = 0;
			Map map = base.Map;
			Vector3 start = CurretPos(base.DistanceCoveredFraction + 0.02f);
			FleckMaker.ConnectingLine(start, lastposition, FleckDef, map, 0.09f);
			lastposition = start;
		}
		if (landed)
		{
			return;
		}
		Vector3 exactPosition = ExactPosition;
		ticksToImpact--;
		if (!ExactPosition.InBounds(base.Map))
		{
			ticksToImpact++;
			base.Position = ExactPosition.ToIntVec3();
			Destroy();
			return;
		}
		Vector3 exactPosition2 = ExactPosition;
		base.Position = ExactPosition.ToIntVec3();
		if (ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && def.projectile.soundImpactAnticipate != null)
		{
			def.projectile.soundImpactAnticipate.PlayOneShot(this);
		}
		if (ticksToImpact <= 0)
		{
			if (base.DestinationCell.InBounds(base.Map))
			{
				base.Position = base.DestinationCell;
			}
			ImpactSomething();
		}
	}

	private void spawnFleck()
	{
		Vector3 drawPos = DrawPos;
		FleckCreationData fleckData = new FleckCreationData
		{
			def = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_HitFlash"),
			spawnPosition = drawPos,
			scale = 1.9f,
			rotation = Rand.Range(0, 360),
			velocitySpeed = 0f,
			rotationRate = 0f,
			orbitSpeed = 0f,
			ageTicksOverride = -1
		};
		base.Map.flecks.CreateFleck(fleckData);
		for (int i = 0; i <= 7; i++)
		{
			FleckCreationData fleckData2 = new FleckCreationData
			{
				def = DefDatabase<FleckDef>.GetNamed("SparkFlash"),
				spawnPosition = drawPos,
				scale = 4f,
				velocitySpeed = 0f,
				rotationRate = 0f,
				rotation = Rand.Range(0, 360),
				ageTicksOverride = -1
			};
			base.Map.flecks.CreateFleck(fleckData2);
		}
		FleckMaker.ThrowFireGlow(drawPos, base.Map, 1f);
		FleckMaker.ThrowAirPuffUp(drawPos, base.Map);
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		spawnFleck();
		Map map = base.Map;
		IntVec3 position = base.Position;
		if (intendedTarget.Thing != null)
		{
			hitThing = intendedTarget.Thing;
			base.Impact(intendedTarget.Thing, blockedByShield);
			BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
			Find.BattleLog.Add(battleLogEntry_RangedImpact);
			NotifyImpact(hitThing, map, position);
			if (hitThing != null)
			{
				bool instigatorGuilty = !(launcher is Pawn pawn) || !pawn.Drafted;
				DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
				dinfo.SetWeaponQuality(equipmentQuality);
				hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
				if (hitThing is Pawn { stances: { } stances })
				{
					stances.stagger.Notify_BulletImpact(this);
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
		else
		{
			Destroy();
		}
	}

	private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
	{
		BulletImpactData impactData = new BulletImpactData
		{
			bullet = this,
			hitThing = intendedTarget.Thing,
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
}
