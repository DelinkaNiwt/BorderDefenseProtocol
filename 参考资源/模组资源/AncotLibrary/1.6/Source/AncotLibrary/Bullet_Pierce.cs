using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AncotLibrary;

public class Bullet_Pierce : Bullet
{
	protected int hitTime = 0;

	private int wallPiercingTimes = 0;

	private Sustainer ambientSustainer;

	private static readonly List<ThingComp> EmptyCompsList = new List<ThingComp>();

	private static List<IntVec3> checkedCells = new List<IntVec3>();

	protected Vector3 targdestination;

	private int ticksToHit;

	private bool IsTarget;

	public ModExtension_PierceBullet Props => def.GetModExtension<ModExtension_PierceBullet>();

	public override int DamageAmount
	{
		get
		{
			if (Props.maxHitTime > 0)
			{
				return (int)((float)base.DamageAmount * (1f - (float)hitTime / (float)Props.maxHitTime));
			}
			return base.DamageAmount;
		}
	}

	protected float StartingTicksToDestroy
	{
		get
		{
			float num = Maxrange / def.projectile.SpeedTilesPerTick;
			if (num <= 0f)
			{
				num = 0.001f;
			}
			return num;
		}
	}

	public override bool AnimalsFleeImpact => true;

	public override Vector3 ExactPosition
	{
		get
		{
			Vector3 vector = (destination - origin).Yto0() * DistanceCoveredFraction_New;
			return origin.Yto0() + vector + Vector3.up * def.Altitude;
		}
	}

	public float DistanceCoveredFraction_New => Mathf.Clamp01(1f - (float)lifetime / StartingTicksToDestroy);

	public float Maxrange
	{
		get
		{
			float num = ((Props.hitRange != -1) ? ((float)Props.hitRange) : ((!(launcher is Pawn { equipment: not null } pawn)) ? 30f : pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range));
			float num2 = equipment?.GetStatValue(StatDefOf.RangedWeapon_RangeMultiplier) ?? 1f;
			return num * num2;
		}
	}

	protected float StartingTicksToImpact_New
	{
		get
		{
			float num = (origin - targdestination).magnitude / def.projectile.SpeedTilesPerTick;
			if (num <= 0f)
			{
				num = 0.001f;
			}
			return num;
		}
	}

	public bool IsTargetOrAlly(Thing hitThing)
	{
		return hitThing == usedTarget || hitThing.Faction == null || (hitThing.Faction != launcher.Faction && hitThing.Faction.RelationKindWith(launcher.Faction) == FactionRelationKind.Hostile);
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		if (hitTime >= Props.maxHitTime && Props.maxHitTime > 0)
		{
			Destroy();
			return;
		}
		Map map = base.Map;
		IntVec3 position = base.Position;
		BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
		Find.BattleLog.Add(battleLogEntry_RangedImpact);
		float num = DamageAmount;
		if (num < 0f)
		{
			num = 0f;
		}
		if (def.projectile.landedEffecter != null)
		{
			def.projectile.landedEffecter.Spawn(base.Position, base.Map).Cleanup();
		}
		NotifyImpact(hitThing, map, position);
		if (hitThing != null)
		{
			if (hitThing.Faction == null && hitThing.def.Fillage == FillCategory.Full && hitThing != usedTarget)
			{
				return;
			}
			bool instigatorGuilty = !(launcher is Pawn pawn) || !pawn.Drafted;
			DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, num, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
			dinfo.SetWeaponQuality(equipmentQuality);
			Pawn pawn2 = hitThing as Pawn;
			if (IsTargetOrAlly(hitThing))
			{
				pawn2?.stances?.stagger.Notify_BulletImpact(this);
				Building_Door building_Door = hitThing as Building_Door;
				if (hitThing.def.Fillage == FillCategory.Full && (building_Door == null || !building_Door.Open))
				{
					dinfo.SetAmount(num * 3f);
					hitTime += 2;
				}
				hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
				if (pawn2 != null && pawn2.BodySize > 1f)
				{
					DamageInfo dinfo2 = new DamageInfo(def.projectile.damageDef, num / 2f, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
					dinfo2.SetWeaponQuality(equipmentQuality);
					int num2 = (int)pawn2.BodySize;
					for (int i = 0; i < num2; i++)
					{
						if (i / 2 == 1)
						{
							hitTime++;
						}
						hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
					}
				}
				if (base.ExtraDamages != null)
				{
					foreach (ExtraDamage extraDamage in base.ExtraDamages)
					{
						if (Rand.Chance(extraDamage.chance))
						{
							DamageInfo dinfo3 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
							hitThing.TakeDamage(dinfo3).AssociateWithLog(battleLogEntry_RangedImpact);
						}
					}
				}
				goto IL_04a8;
			}
		}
		else if (!blockedByShield)
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
		if (Rand.Chance(base.DamageDef.igniteCellChance))
		{
			FireUtility.TryStartFireIn(base.Position, map, Rand.Range(0.55f, 0.85f), launcher);
		}
		goto IL_04a8;
		IL_04a8:
		hitTime++;
		if (hitTime >= Props.maxHitTime && Props.maxHitTime > 0)
		{
			Destroy();
		}
	}

	public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
	{
		base.launcher = launcher;
		base.origin = origin;
		base.usedTarget = usedTarget;
		base.intendedTarget = intendedTarget;
		base.targetCoverDef = targetCoverDef;
		base.preventFriendlyFire = preventFriendlyFire;
		base.HitFlags = hitFlags;
		stoppingPower = def.projectile.stoppingPower;
		if (stoppingPower == 0f && def.projectile.damageDef != null)
		{
			stoppingPower = def.projectile.damageDef.defaultStoppingPower;
		}
		if (equipment != null)
		{
			base.equipment = equipment;
			equipmentDef = equipment.def;
			equipment.TryGetQuality(out equipmentQuality);
			if (equipment.TryGetComp(out CompUniqueWeapon comp))
			{
				foreach (WeaponTraitDef item in comp.TraitsListForReading)
				{
					if (!Mathf.Approximately(item.additionalStoppingPower, 0f))
					{
						stoppingPower += item.additionalStoppingPower;
					}
				}
			}
		}
		else
		{
			equipmentDef = null;
		}
		Vector3 vector = launcher.Position.ToVector3Shifted() - usedTarget.Cell.ToVector3Shifted();
		vector.Normalize();
		destination = launcher.Position.ToVector3Shifted() - vector * Maxrange + Gen.RandomHorizontalVector(0.3f);
		targdestination = usedTarget.Cell.ToVector3Shifted() + Gen.RandomHorizontalVector(0.3f);
		ticksToImpact = Mathf.CeilToInt(StartingTicksToDestroy);
		ticksToHit = Mathf.CeilToInt(StartingTicksToImpact_New);
		if (ticksToImpact < 1)
		{
			ticksToImpact = 1;
		}
		lifetime = Mathf.CeilToInt(StartingTicksToDestroy);
		if (!def.projectile.soundAmbient.NullOrUndefined())
		{
			ambientSustainer = def.projectile.soundAmbient.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
		}
	}

	protected override void ImpactSomething()
	{
		if (!IsTarget && CanHit(usedTarget.Thing))
		{
			if (usedTarget.Thing is Pawn p && p.GetPosture() != PawnPosture.Standing && (origin - destination).MagnitudeHorizontalSquared() >= 20.25f && !Rand.Chance(0.5f))
			{
				ThrowDebugText("miss-laying", base.Position);
				Impact(null);
			}
			else
			{
				Impact(usedTarget.Thing);
				IsTarget = true;
			}
		}
	}

	private void ThrowDebugText(string text, IntVec3 c)
	{
		if (DebugViewSettings.drawShooting)
		{
			MoteMaker.ThrowText(c.ToVector3Shifted(), base.Map, text);
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

	private bool CheckForFreeInterceptBetween(Vector3 lastExactPos, Vector3 newExactPos)
	{
		if (lastExactPos == newExactPos)
		{
			return false;
		}
		if (this == null)
		{
			return true;
		}
		List<Thing> list = base.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].TryGetComp<CompProjectileInterceptor>().CheckIntercept(this, lastExactPos, newExactPos))
			{
				hitTime += 2;
				Impact(null);
				if (hitTime >= Props.maxHitTime)
				{
					return true;
				}
			}
		}
		IntVec3 intVec = lastExactPos.ToIntVec3();
		IntVec3 intVec2 = newExactPos.ToIntVec3();
		if (intVec2 == intVec)
		{
			return false;
		}
		if (!intVec.InBounds(base.Map) || !intVec2.InBounds(base.Map))
		{
			return false;
		}
		if (intVec2.AdjacentToCardinal(intVec))
		{
			return CheckForFreeIntercept(intVec2);
		}
		if (VerbUtility.InterceptChanceFactorFromDistance(origin, intVec2) <= 0f)
		{
			return false;
		}
		Vector3 vect = lastExactPos;
		Vector3 v = newExactPos - lastExactPos;
		Vector3 vector = v.normalized * 0.2f;
		int num = (int)(v.MagnitudeHorizontal() / 0.2f);
		checkedCells.Clear();
		int num2 = 0;
		while (true)
		{
			vect += vector;
			IntVec3 intVec3 = vect.ToIntVec3();
			if (hitTime >= Props.maxHitTime && Props.maxHitTime > 0)
			{
				return true;
			}
			if (!checkedCells.Contains(intVec3))
			{
				if (CheckForFreeIntercept(intVec3))
				{
					break;
				}
				checkedCells.Add(intVec3);
			}
			num2++;
			if (num2 > num)
			{
				return false;
			}
			if (intVec3 == intVec2)
			{
				return false;
			}
		}
		return true;
	}

	protected override void TickInterval(int delta)
	{
		foreach (ThingComp allComp in base.AllComps)
		{
			allComp.CompTickInterval(delta);
		}
		if (lifetime <= 0)
		{
			if (base.DestinationCell.InBounds(base.Map))
			{
				base.Position = base.DestinationCell;
			}
			GenClamor.DoClamor(this, 12f, ClamorDefOf.Impact);
			Destroy();
			return;
		}
		if (landed)
		{
			landed = false;
		}
		Vector3 exactPosition = ExactPosition;
		lifetime -= delta;
		ticksToImpact -= delta;
		ticksToHit -= delta;
		if (!ExactPosition.InBounds(base.Map))
		{
			lifetime += delta;
			base.Position = ExactPosition.ToIntVec3();
			Destroy();
			return;
		}
		Vector3 exactPosition2 = ExactPosition;
		if (!CheckForFreeInterceptBetween(exactPosition, exactPosition2))
		{
			base.Position = ExactPosition.ToIntVec3();
			if ((hitTime < Props.maxHitTime || Props.maxHitTime <= 0) && ticksToHit <= 0)
			{
				ImpactSomething();
			}
		}
	}

	private bool CheckForFreeIntercept(IntVec3 c)
	{
		float num = 1f;
		if (num <= 0f)
		{
			return false;
		}
		bool flag = false;
		bool result = false;
		if (base.Map == null || !c.InBounds(base.Map))
		{
			return false;
		}
		List<Thing> thingList = c.GetThingList(base.Map);
		for (int i = 0; i < thingList.Count; i++)
		{
			if (hitTime >= Props.maxHitTime && Props.maxHitTime > 0)
			{
				return false;
			}
			Thing thing = thingList[i];
			if (!CanHit(thing))
			{
				continue;
			}
			bool flag2 = false;
			if (thing.def.Fillage == FillCategory.Full)
			{
				if (!(thing is Building_Door { Open: not false }))
				{
					if (IsTarget && thing == usedTarget)
					{
						continue;
					}
					if (thing == usedTarget)
					{
						IsTarget = true;
					}
					wallPiercingTimes++;
					if ((Props.maxWallPiercing > 0 && wallPiercingTimes > Props.maxWallPiercing) || Props.maxWallPiercing < 0)
					{
						Destroy();
						return false;
					}
					ThrowDebugText("int-wall", c);
					Impact(thing);
					return false;
				}
				flag2 = true;
			}
			float num2 = 0f;
			if (thing is Pawn pawn)
			{
				num2 = 1f;
				num = 1f;
				if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))
				{
					num2 = 0f;
					ThrowDebugText("ff-miss", c);
				}
			}
			else if (thing.def.fillPercent > 0.2f)
			{
				num2 = (flag2 ? 0.05f : ((!base.DestinationCell.AdjacentTo8Way(c)) ? (thing.def.fillPercent * 0.15f) : (thing.def.fillPercent * 1f)));
			}
			num2 *= num;
			if (!(num2 > 1E-05f))
			{
				continue;
			}
			if (Rand.Chance(num2))
			{
				if (!IsTarget || !(thing == usedTarget))
				{
					if (thing == usedTarget)
					{
						IsTarget = true;
					}
					ThrowDebugText("int-" + num2.ToStringPercent(), c);
					Impact(thing);
				}
			}
			else
			{
				flag = true;
				ThrowDebugText(num2.ToStringPercent(), c);
			}
		}
		if (!flag)
		{
			ThrowDebugText("o", c);
		}
		return result;
	}

	protected new bool CanHit(Thing thing)
	{
		if (this == null)
		{
			return false;
		}
		if (thing == null)
		{
			return false;
		}
		if (!thing.Spawned)
		{
			return false;
		}
		if (thing == launcher)
		{
			return false;
		}
		ProjectileHitFlags projectileHitFlags = base.HitFlags | ProjectileHitFlags.NonTargetPawns;
		if (projectileHitFlags == ProjectileHitFlags.None)
		{
			return false;
		}
		if (thing.Map != base.Map)
		{
			return false;
		}
		if (CoverUtility.ThingCovered(thing, base.Map))
		{
			return false;
		}
		if (thing == intendedTarget && (projectileHitFlags & ProjectileHitFlags.IntendedTarget) != ProjectileHitFlags.None)
		{
			return true;
		}
		if (thing != intendedTarget)
		{
			if (thing is Pawn targetpawn)
			{
				if (Rand.Chance(BulletUtility.GetPierceHitChance(launcher, equipment, targetpawn, origin)))
				{
					return true;
				}
			}
			else if (projectileHitFlags != ProjectileHitFlags.None)
			{
				return true;
			}
		}
		return thing == intendedTarget && thing.def.Fillage == FillCategory.Full;
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		if (Props.barrelLength > 0f)
		{
			float num = Props.barrelLength / def.projectile.SpeedTilesPerTick;
			float num2 = Mathf.Clamp01(num / base.StartingTicksToImpact);
			if (base.DistanceCoveredFraction < num2)
			{
				return;
			}
		}
		base.DrawAt(drawLoc, flip);
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		base.Destroy(mode);
		if (Props.impactEffecter != null)
		{
			Props.impactEffecter.Spawn().Trigger(new TargetInfo(ExactPosition.ToIntVec3(), launcher.Map), launcher);
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref origin, "origin");
		Scribe_Values.Look(ref destination, "destination");
		Scribe_Values.Look(ref targdestination, "targdestination");
		Scribe_Values.Look(ref ticksToImpact, "ticksToImpact", 0);
		Scribe_Values.Look(ref hitTime, "hitTime", 0);
		Scribe_Values.Look(ref wallPiercingTimes, "wallPiercingTimes", 0);
		Scribe_TargetInfo.Look(ref usedTarget, "usedTarget");
		Scribe_TargetInfo.Look(ref intendedTarget, "intendedTarget");
		Scribe_References.Look(ref launcher, "launcher");
		Scribe_Defs.Look(ref equipmentDef, "equipmentDef");
		Scribe_Defs.Look(ref targetCoverDef, "targetCoverDef");
		Scribe_Values.Look(ref preventFriendlyFire, "preventFriendlyFire", defaultValue: false);
		Scribe_Values.Look(ref landed, "landed", defaultValue: false);
		Scribe_Values.Look(ref IsTarget, "IsTarget", defaultValue: false);
		Scribe_Values.Look(ref lifetime, "lifetime", 0);
		Scribe_Values.Look(ref equipmentQuality, "equipmentQuality", QualityCategory.Normal);
	}
}
