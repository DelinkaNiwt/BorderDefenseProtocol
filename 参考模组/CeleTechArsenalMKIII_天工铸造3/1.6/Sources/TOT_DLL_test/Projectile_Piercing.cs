using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Projectile_Piercing : Projectile
{
	private PiercingAmmo_Extension piercingProjectileDefInt;

	private int penetratingPowerLeft;

	private Vector3 prevPosition;

	private HashSet<Thing> hitHashSet = new HashSet<Thing>();

	private Vector3 startPosition;

	public static readonly HashSet<AltitudeLayer> altitudeLayersBlackList = new HashSet<AltitudeLayer>
	{
		AltitudeLayer.Item,
		AltitudeLayer.ItemImportant,
		AltitudeLayer.Conduits,
		AltitudeLayer.Floor,
		AltitudeLayer.FloorEmplacement
	};

	public PiercingAmmo_Extension piercingProjectileDef
	{
		get
		{
			if (piercingProjectileDefInt == null)
			{
				piercingProjectileDefInt = def.GetModExtension<PiercingAmmo_Extension>();
			}
			return piercingProjectileDefInt;
		}
	}

	public int PenetratingPowerLeft => penetratingPowerLeft;

	public override bool AnimalsFleeImpact => true;

	public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
	{
		base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
		Init(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
	}

	private void Init(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
	{
		if (piercingProjectileDef == null)
		{
			Destroy();
			return;
		}
		penetratingPowerLeft = piercingProjectileDef.penetratingPower;
		prevPosition = new Vector3(origin.x, 0f, origin.z);
		startPosition = new Vector3(origin.x, 0f, origin.z);
		if (piercingProjectileDef.rangeOverride.HasValue)
		{
			SetRangeTo(piercingProjectileDef.rangeOverride.Value);
		}
		else if (piercingProjectileDef.reachMaxRangeAlways && equipment != null && (intendedTarget.Thing == null || intendedTarget.Thing.def.Fillage == FillCategory.Full || piercingProjectileDef.penetratingPower > piercingProjectileDef.penetratingPowerCostByShield || intendedTarget.Cell.DistanceToSquared(origin.ToIntVec3()) > 16))
		{
			SetDestinationToMax(equipment, launcher);
		}
	}

	public void SetRangeTo(float range)
	{
		Vector3 normalized = (destination - origin).normalized;
		Vector3 vect = normalized * range + origin;
		List<IntVec3> list = new ShootLine(origin.ToIntVec3(), vect.ToIntVec3()).Points().ToList();
		while (list.Count > 0)
		{
			IntVec3 c = list.Pop();
			if (c.InBounds(base.Map))
			{
				destination = c.ToVector3();
				destination.x += Rand.Value;
				destination.z += Rand.Value;
				break;
			}
		}
		ticksToImpact = Mathf.CeilToInt(base.StartingTicksToImpact);
	}

	public void SetDestinationToMax(Thing equipment, Thing launcher)
	{
		SetRangeTo(Mathf.Min(Mathf.Max(base.Map.Size.x, base.Map.Size.z), GetEquipmentRange(equipment)));
	}

	private float GetEquipmentRange(Thing equipment)
	{
		CompEquippable compEquippable = equipment.TryGetComp<CompEquippable>();
		if (compEquippable != null)
		{
			return compEquippable.PrimaryVerb.verbProps.range;
		}
		throw new Exception("Couldn'hitThing determine max range for " + Label);
	}

	public bool TryHitThing(Thing t, out bool needToBeDestroy, bool blockedByShield = false)
	{
		needToBeDestroy = false;
		bool result = false;
		if (!hitHashSet.Contains(t))
		{
			if (IsDamagable(t, blockedByShield))
			{
				if (!CanPiercing(t))
				{
					needToBeDestroy = true;
				}
				HitThing(t, blockedByShield);
				result = true;
			}
			else
			{
				MissThing(t);
			}
		}
		if (penetratingPowerLeft <= 0)
		{
			needToBeDestroy = true;
		}
		return result;
	}

	private bool IsDamagable(Thing thing, bool blockedByShield = false)
	{
		bool result;
		if (blockedByShield)
		{
			result = true;
		}
		else if (intendedTarget.Thing == thing)
		{
			result = true;
		}
		else if (thing == null)
		{
			result = false;
		}
		else if (altitudeLayersBlackList.Contains(thing.def.altitudeLayer))
		{
			result = false;
		}
		else if ((float)thing.Position.DistanceToSquared(startPosition.ToIntVec3()) < piercingProjectileDef.minDistanceToAffectAny * piercingProjectileDef.minDistanceToAffectAny)
		{
			result = false;
		}
		else
		{
			float chance = 0f;
			if (thing is Pawn pawn)
			{
				chance = 0.4f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
				bool flag = pawn.GetPosture() == PawnPosture.Standing;
				if (!flag)
				{
					chance *= 0.1f;
				}
				if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))
				{
					chance = ((!preventFriendlyFire && !((float)pawn.Position.DistanceToSquared(startPosition.ToIntVec3()) < piercingProjectileDef.minDistanceToAffectAlly * piercingProjectileDef.minDistanceToAffectAlly)) ? (chance * Find.Storyteller.difficulty.friendlyFireChanceFactor) : 0f);
				}
				else if (flag && piercingProjectileDef.alwaysHitStandingEnemy)
				{
					return true;
				}
				result = Rand.Chance(chance);
			}
			else if (thing.def.Fillage == FillCategory.Full)
			{
				result = !(thing is Building_Door { Open: not false }) || Rand.Chance(0.05f);
			}
			else
			{
				if (thing.def.fillPercent > 0.2f)
				{
					chance = (intendedTarget.Cell.AdjacentTo8Way(thing.Position) ? (thing.def.fillPercent * 1f) : (thing.def.fillPercent * 0.15f));
				}
				result = Rand.Chance(chance);
			}
		}
		return result;
	}

	private void HitThing(Thing hitThing, bool blockedByShield = false)
	{
		if (blockedByShield)
		{
			penetratingPowerLeft -= piercingProjectileDef.penetratingPowerCostByShield;
		}
		else
		{
			penetratingPowerLeft--;
		}
		if (hitThing == null)
		{
			return;
		}
		hitHashSet.Add(hitThing);
		BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = ((equipmentDef != null) ? new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef) : new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, ThingDef.Named("Gun_Autopistol"), def, targetCoverDef));
		Find.BattleLog.Add(battleLogEntry_RangedImpact);
		DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, base.ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing);
		hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
		if (hitThing != null && hitThing is Pawn && (hitThing as Pawn).stances != null)
		{
			Pawn pawn = (Pawn)hitThing;
			if (pawn.BodySize <= def.projectile.stoppingPower + 0.001f)
			{
				pawn.stances.stagger.StaggerFor(95);
			}
		}
		if (def.projectile.extraDamages == null)
		{
			return;
		}
		foreach (ExtraDamage extraDamage in def.projectile.extraDamages)
		{
			if (Rand.Chance(extraDamage.chance))
			{
				DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing);
				hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
			}
		}
	}

	private void MissThing(Thing t)
	{
		if (t != null)
		{
			hitHashSet.Add(t);
		}
	}

	public virtual bool CanPiercing(Thing thing)
	{
		if (thing == null)
		{
			return true;
		}
		bool flag = thing.def.Fillage == FillCategory.Full && !(thing is Building_Door);
		return !flag;
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		if (TryHitThing(hitThing, out var needToBeDestroy, blockedByShield))
		{
			GenClamor.DoClamor(this, 12f, ClamorDefOf.Impact);
			if (!blockedByShield && def.projectile.landedEffecter != null)
			{
				def.projectile.landedEffecter.Spawn(base.Position, base.Map).Cleanup();
			}
		}
		if (needToBeDestroy && !base.Destroyed)
		{
			Destroy();
		}
	}

	protected override void ImpactSomething()
	{
		if (penetratingPowerLeft == piercingProjectileDef.penetratingPower)
		{
			base.ImpactSomething();
		}
		if (!base.Destroyed)
		{
			Destroy();
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref penetratingPowerLeft, "penetratingPowerLeft", 1);
		Scribe_Values.Look(ref prevPosition, "prevPosition");
		Scribe_Values.Look(ref startPosition, "startPosition");
		Scribe_Collections.Look(ref hitHashSet, "hitHashSet", LookMode.Reference);
	}
}
