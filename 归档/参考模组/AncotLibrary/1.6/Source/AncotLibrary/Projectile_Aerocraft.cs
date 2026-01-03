using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AncotLibrary;

public class Projectile_Aerocraft : Bullet
{
	private ProjectileHitFlags cachedHitFlags = ProjectileHitFlags.All;

	private Sustainer ambientSustainer;

	private static List<IntVec3> checkedCells = new List<IntVec3>();

	private float? cachedStartingTicksToImpact;

	public int ticksToHit;

	public Projectile_Custom_Extension Props => def.GetModExtension<Projectile_Custom_Extension>();

	public Projectile_Aerocraft_Extension Aero_Props => def.GetModExtension<Projectile_Aerocraft_Extension>();

	public new ProjectileHitFlags HitFlags
	{
		get
		{
			if (def.projectile.alwaysFreeIntercept)
			{
				return ProjectileHitFlags.All;
			}
			return cachedHitFlags;
		}
		set
		{
			cachedHitFlags = value;
		}
	}

	protected new float StartingTicksToImpact
	{
		get
		{
			if (!cachedStartingTicksToImpact.HasValue)
			{
				float num = (origin - destination).magnitude / def.projectile.SpeedTilesPerTick;
				if (num <= 0f)
				{
					num = 0.001f;
				}
				cachedStartingTicksToImpact = num;
			}
			return cachedStartingTicksToImpact.Value;
		}
	}

	public override Vector3 DrawPos
	{
		get
		{
			if (Aero_Props.launchHeight == 0f)
			{
				return base.DrawPos;
			}
			float num = Aero_Props.launchHeight * Mathf.Clamp01(1f - (float)ticksToImpact / StartingTicksToImpact);
			Vector3 drawPos = base.DrawPos;
			return new Vector3(drawPos.x, drawPos.y, drawPos.z + num);
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
		HitFlags = hitFlags;
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
		destination = usedTarget.Cell.ToVector3Shifted() + Gen.RandomHorizontalVector(0.3f);
		ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
		ticksToHit = Mathf.CeilToInt((float)ticksToImpact * Aero_Props.hitDistancePercent);
		if (ticksToImpact < 1)
		{
			ticksToImpact = 1;
		}
		lifetime = ticksToImpact;
		if (!def.projectile.soundAmbient.NullOrUndefined())
		{
			ambientSustainer = def.projectile.soundAmbient.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
		}
	}

	protected override void Tick()
	{
		if (!base.AllComps.NullOrEmpty())
		{
			int i = 0;
			for (int count = base.AllComps.Count; i < count; i++)
			{
				base.AllComps[i].CompTick();
			}
		}
		if (ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && def.projectile.soundImpactAnticipate != null)
		{
			def.projectile.soundImpactAnticipate.PlayOneShot(this);
		}
		ambientSustainer?.Maintain();
	}

	protected override void TickInterval(int delta)
	{
		if (!base.AllComps.NullOrEmpty())
		{
			int i = 0;
			for (int count = base.AllComps.Count; i < count; i++)
			{
				base.AllComps[i].CompTickInterval(delta);
			}
		}
		lifetime -= delta;
		if (landed)
		{
			return;
		}
		Vector3 exactPosition = ExactPosition;
		ticksToImpact -= delta;
		if (!ExactPosition.InBounds(base.Map))
		{
			ticksToImpact += delta;
			base.Position = ExactPosition.ToIntVec3();
			Destroy();
			return;
		}
		Vector3 exactPosition2 = ExactPosition;
		if (CheckForFreeInterceptBetween(exactPosition, exactPosition2))
		{
			return;
		}
		base.Position = ExactPosition.ToIntVec3();
		if (ticksToImpact <= 0)
		{
			if (base.DestinationCell.InBounds(base.Map))
			{
				base.Position = base.DestinationCell;
			}
			ImpactSomething();
		}
	}

	private bool CheckForFreeInterceptBetween(Vector3 lastExactPos, Vector3 newExactPos)
	{
		if (lastExactPos == newExactPos)
		{
			return false;
		}
		List<Thing> list = base.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].TryGetComp<CompProjectileInterceptor>().CheckIntercept(this, lastExactPos, newExactPos))
			{
				Impact(null, blockedByShield: true);
				return true;
			}
		}
		if (ticksToHit < ticksToImpact)
		{
			return false;
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

	private bool CheckForFreeIntercept(IntVec3 c)
	{
		if (destination.ToIntVec3() == c)
		{
			return false;
		}
		float num = VerbUtility.InterceptChanceFactorFromDistance(origin, c);
		if (num <= 0f)
		{
			return false;
		}
		bool flag = false;
		List<Thing> thingList = c.GetThingList(base.Map);
		for (int i = 0; i < thingList.Count; i++)
		{
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
					ThrowDebugText("int-wall", c);
					Impact(thing);
					return true;
				}
				flag2 = true;
			}
			float num2 = 0f;
			if (thing is Pawn pawn)
			{
				num2 = 0.4f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
				if (pawn.GetPosture() != PawnPosture.Standing)
				{
					num2 *= 0.1f;
				}
				if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))
				{
					if (preventFriendlyFire)
					{
						num2 = 0f;
						ThrowDebugText("ff-miss", c);
					}
					else
					{
						num2 *= Find.Storyteller.difficulty.friendlyFireChanceFactor;
					}
				}
			}
			else if (thing.def.fillPercent > 0.2f)
			{
				num2 = (flag2 ? 0.05f : ((!base.DestinationCell.AdjacentTo8Way(c)) ? (thing.def.fillPercent * 0.15f) : (thing.def.fillPercent * 1f)));
			}
			num2 *= num;
			if (num2 > 1E-05f)
			{
				if (Rand.Chance(num2))
				{
					ThrowDebugText("int-" + num2.ToStringPercent(), c);
					Impact(thing);
					return true;
				}
				flag = true;
				ThrowDebugText(num2.ToStringPercent(), c);
			}
		}
		if (!flag)
		{
			ThrowDebugText("o", c);
		}
		return false;
	}

	private void ThrowDebugText(string text, IntVec3 c)
	{
		if (DebugViewSettings.drawShooting)
		{
			MoteMaker.ThrowText(c.ToVector3Shifted(), base.Map, text);
		}
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		if (usedTarget.HasThing)
		{
			Log.Message(CanHit(usedTarget.Thing).ToString());
		}
		base.Impact(hitThing, blockedByShield);
		Props?.impactEffecter?.Spawn().Trigger(new TargetInfo(ExactPosition.ToIntVec3(), launcher.Map), launcher);
	}

	protected override void ImpactSomething()
	{
		if (def.projectile.flyOverhead)
		{
			RoofDef roofDef = base.Map.roofGrid.RoofAt(base.Position);
			if (roofDef != null)
			{
				if (roofDef.isThickRoof)
				{
					ThrowDebugText("hit-thick-roof", base.Position);
					if (!def.projectile.soundHitThickRoof.NullOrUndefined())
					{
						def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(base.Position, base.Map));
					}
					Destroy();
					return;
				}
				if (base.Position.GetEdifice(base.Map) == null || base.Position.GetEdifice(base.Map).def.Fillage != FillCategory.Full)
				{
					RoofCollapserImmediate.DropRoofInCells(base.Position, base.Map);
				}
			}
		}
		if (!usedTarget.HasThing || !CanHit(usedTarget.Thing))
		{
			List<Thing> list = VerbUtility.ThingsToHit(base.Position, base.Map, CanHit);
			list.Shuffle();
			for (int i = 0; i < list.Count; i++)
			{
				Thing thing = list[i];
				float num;
				if (thing is Pawn pawn)
				{
					num = 0.5f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
					if (pawn.GetPosture() != PawnPosture.Standing && (origin - destination).MagnitudeHorizontalSquared() >= 20.25f)
					{
						num *= 0.5f;
					}
					if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))
					{
						num *= VerbUtility.InterceptChanceFactorFromDistance(origin, base.Position);
					}
				}
				else
				{
					num = 1.5f * thing.def.fillPercent;
				}
				if (Rand.Chance(num))
				{
					ThrowDebugText("hit-" + num.ToStringPercent(), base.Position);
					Impact(thing);
					return;
				}
				ThrowDebugText("miss-" + num.ToStringPercent(), base.Position);
			}
			Impact(null);
		}
		else if (usedTarget.Thing is Pawn p && p.GetPosture() != PawnPosture.Standing && (origin - destination).MagnitudeHorizontalSquared() >= 20.25f && !Rand.Chance(0.5f))
		{
			ThrowDebugText("miss-laying", base.Position);
			Impact(null);
		}
		else
		{
			Impact(usedTarget.Thing);
		}
	}

	protected new bool CanHit(Thing thing)
	{
		if (!thing.Spawned)
		{
			return false;
		}
		if (thing == launcher)
		{
			return false;
		}
		ProjectileHitFlags hitFlags = HitFlags;
		if (hitFlags == ProjectileHitFlags.None)
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
		if (thing == intendedTarget && (hitFlags & ProjectileHitFlags.IntendedTarget) != ProjectileHitFlags.None)
		{
			return true;
		}
		if (thing != intendedTarget)
		{
			if (thing is Pawn)
			{
				if ((hitFlags & ProjectileHitFlags.NonTargetPawns) != ProjectileHitFlags.None)
				{
					return true;
				}
			}
			else if ((hitFlags & ProjectileHitFlags.NonTargetWorld) != ProjectileHitFlags.None)
			{
				return true;
			}
		}
		return thing == intendedTarget && thing.def.Fillage == FillCategory.Full;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref cachedHitFlags, "cachedHitFlags", ProjectileHitFlags.All);
		Scribe_Values.Look(ref ticksToHit, "ticksToHit", 0);
		Scribe_Values.Look(ref cachedStartingTicksToImpact, "cachedStartingTicksToImpact");
	}
}
