using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Projectile_PoiBullet : Bullet
{
	private bool flag2 = false;

	private bool flag3 = true;

	private bool CalHit = false;

	private Vector3 Randdd;

	private int tickcount;

	public Mote MoteonTarget;

	public Mote mote = null;

	public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_GunTail_Blue");

	public int Fleck_MakeFleckTickMax = 1;

	public int Fleck_MakeFleckTick;

	public Vector3 lastposition;

	private float probeStep = 0.2f;

	private void RandFactor()
	{
		FloatRange floatRange = new FloatRange(-0.5f, 0.5f);
		FloatRange floatRange2 = new FloatRange(-0.5f, 0.5f);
		Randdd.x = floatRange.RandomInRange;
		Randdd.z = floatRange2.RandomInRange;
		flag2 = true;
	}

	public Vector3 BPos(float t)
	{
		if (!flag2)
		{
			RandFactor();
		}
		Vector3 vector = origin;
		Vector3 vector2 = (origin + destination) / 2f;
		vector2 += Randdd;
		vector2.y = destination.y;
		Vector3 vector3 = destination;
		return (1f - t) * (1f - t) * vector + 2f * t * (1f - t) * vector2 + t * t * vector3;
	}

	private void FindRandCell(Vector3 d)
	{
		IntVec3 center = IntVec3.FromVector3(d);
		intendedTarget = CellRect.CenteredOn(center, 2).RandomCell;
	}

	protected override void DrawAt(Vector3 position, bool flip = false)
	{
		Vector3 vector = BPos(base.DistanceCoveredFraction - 0.01f);
		position = BPos(base.DistanceCoveredFraction);
		Quaternion rotation = Quaternion.LookRotation(position - vector);
		if (tickcount >= 4)
		{
			Vector3 position2 = position;
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
		Vector3 vector = BPos(base.DistanceCoveredFraction + 0.02f);
		base.Position = vector.ToIntVec3();
		if (tickcount >= 8)
		{
			Map map = base.Map;
			FleckMaker.ConnectingLine(vector, lastposition, FleckDef, map, 0.035f);
			lastposition = vector;
		}
		if (flag3)
		{
			CalHit = CanHitTarget();
			flag3 = false;
		}
		if (!CalHit)
		{
			FindRandCell(intendedTarget.CenterVector3);
		}
		if (intendedTarget.Thing != null && intendedTarget.Thing is Pawn { DeadOrDowned: false })
		{
			destination = intendedTarget.Thing.DrawPos;
			if (mote.DestroyedOrNull())
			{
				ThingDef cMC_Mote_SWTargetLocked = CMC_Def.CMC_Mote_SWTargetLocked;
				Vector3 offset = new Vector3(0f, 0f, 0f);
				offset.y = AltitudeLayer.PawnRope.AltitudeFor();
				mote = MoteMaker.MakeAttachedOverlay(intendedTarget.Thing, cMC_Mote_SWTargetLocked, offset, 1f, 1f);
				mote.exactRotation = 45f;
			}
			else
			{
				mote.Maintain();
			}
		}
		base.Tick();
	}

	public virtual bool CanHitTarget()
	{
		return Rand.Chance(Hitchance());
	}

	private float Hitchance()
	{
		Pawn pawn = launcher as Pawn;
		bool flag = pawn != null && !pawn.NonHumanlikeOrWildMan();
		int num = 0;
		if (flag)
		{
			SkillDef named = DefDatabase<SkillDef>.GetNamed("Intellectual");
			num = pawn.skills.GetSkill(named)?.GetLevel() ?? 10;
		}
		else
		{
			num = 8;
		}
		float num2 = Mathf.Clamp01((float)num / 20f);
		float num3 = 3f * num2 * num2 - 2f * num2 * num2 * num2;
		return 0.33f + 0.62f * num3;
	}

	public float GetDamageMultiplier(float distance, float d0, float m)
	{
		if (distance <= d0)
		{
			return 1f;
		}
		float num = Mathf.Pow(distance - d0, 2f) / Mathf.Pow(distance, 2f);
		return Mathf.Clamp(m + (1f - m) * num, m, 1f);
	}

	public float GetPenetrationMultiplier(float distance, float d0, float mp)
	{
		if (distance <= d0)
		{
			return 1f;
		}
		float value = Mathf.Pow(distance - d0, 3f) / Mathf.Pow(distance, 3f);
		return Mathf.Clamp(value, mp, 1f);
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Map map = base.Map;
		IntVec3 position = base.Position;
		if (intendedTarget.Thing is Pawn || intendedTarget.Thing is Building)
		{
			hitThing = intendedTarget.Thing;
		}
		base.Impact(hitThing, blockedByShield);
		BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
		Find.BattleLog.Add(battleLogEntry_RangedImpact);
		NotifyImpact(hitThing, map, position);
		if (hitThing != null && !blockedByShield)
		{
			Pawn pawn = launcher as Pawn;
			bool instigatorGuilty = pawn == null || !pawn.Drafted;
			float num = 1f;
			float num2 = 1f;
			if (pawn != null)
			{
				CompSmartWeapon compSmartWeapon = pawn.equipment.Primary.TryGetComp<CompSmartWeapon>();
				if (compSmartWeapon != null)
				{
					num = GetDamageMultiplier((origin - DrawPos).magnitude, compSmartWeapon.Props.DamageDeductionRange, compSmartWeapon.Props.MinDamageMultiplier);
					num2 = GetPenetrationMultiplier((origin - DrawPos).magnitude, compSmartWeapon.Props.DamageDeductionRange, compSmartWeapon.Props.MinPenetrationMultiplier);
				}
			}
			DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, (float)DamageAmount * num, ArmorPenetration * num, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
			hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
			if (hitThing is Pawn { stances: not null } pawn2)
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
}
