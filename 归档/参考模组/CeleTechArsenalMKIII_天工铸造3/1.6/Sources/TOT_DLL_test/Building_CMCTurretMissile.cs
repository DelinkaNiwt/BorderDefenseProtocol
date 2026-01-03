using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Building_CMCTurretMissile : Building_CMCTurretGun
{
	public static Material LTexture0 = MaterialPool.MatFrom("Things/Buildings/CMC_MissileLauncherTop_0", ShaderDatabase.Cutout);

	public static Material LTexture1 = MaterialPool.MatFrom("Things/Buildings/CMC_MissileLauncherTop_1", ShaderDatabase.Cutout);

	public static Material LTexture2 = MaterialPool.MatFrom("Things/Buildings/CMC_MissileLauncherTop_2", ShaderDatabase.Cutout);

	public static Material LTexture3 = MaterialPool.MatFrom("Things/Buildings/CMC_MissileLauncherTop_3", ShaderDatabase.Cutout);

	public static Material LTexture4 = MaterialPool.MatFrom("Things/Buildings/CMC_MissileLauncherTop_4", ShaderDatabase.Cutout);

	public static Material LTexture5 = MaterialPool.MatFrom("Things/Buildings/CMC_MissileLauncherTop_5", ShaderDatabase.Cutout);

	public static Material LTexture6 = MaterialPool.MatFrom("Things/Buildings/CMC_MissileLauncherTop_6", ShaderDatabase.Cutout);

	public static List<Material> ML_LToptexture = new List<Material> { LTexture0, LTexture1, LTexture2, LTexture3, LTexture4, LTexture5, LTexture6 };

	public static bool HasAESARadar = false;

	public override bool CanSetForcedTarget => true;

	public override Material TurretTopMaterial
	{
		get
		{
			if (def == CMC_Def.CMCML)
			{
				int index = (int)refuelableComp.Fuel;
				return ML_LToptexture[index];
			}
			return def.building.turretTopMat;
		}
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		List<Thing> list = map.listerThings.ThingsOfDef(CMC_Def.CMC_CICAESA_Radar);
		if (list.Count >= 1)
		{
			HasAESARadar = true;
		}
	}

	public override bool IsValidTarget(Thing t)
	{
		if (t is Pawn pawn)
		{
			if (base.Faction == Faction.OfPlayer && pawn.IsPrisoner)
			{
				return false;
			}
			if (AttackVerb.ProjectileFliesOverhead())
			{
				RoofDef roofDef = base.Map.roofGrid.RoofAt(t.Position);
				if (roofDef != null && roofDef.isThickRoof)
				{
					return false;
				}
			}
			if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
			{
				return false;
			}
		}
		return true;
	}

	public override LocalTargetInfo TryFindNewTarget()
	{
		LocalTargetInfo result;
		if (!HasAESARadar)
		{
			result = null;
		}
		else
		{
			IAttackTargetSearcher attackTargetSearcher = TargSearcher();
			Faction faction = attackTargetSearcher.Thing.Faction;
			float range = AttackVerb.verbProps.range;
			if (Rand.Value < 0.5f && AttackVerb.ProjectileFliesOverhead() && faction.HostileTo(Faction.OfPlayer) && base.Map.listerBuildings.allBuildingsColonist.Where(delegate(Building x)
			{
				float num = AttackVerb.verbProps.EffectiveMinRange(x, this);
				float num2 = x.Position.DistanceToSquared(base.Position);
				return num2 > num * num && num2 < range * range;
			}).TryRandomElement(out var result2))
			{
				return result2;
			}
			TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
			if (!AttackVerb.ProjectileFliesOverhead())
			{
				targetScanFlags |= TargetScanFlags.NeedLOSToAll;
				targetScanFlags |= TargetScanFlags.LOSBlockableByGas;
			}
			if (AttackVerb.IsIncendiary_Ranged())
			{
				targetScanFlags |= TargetScanFlags.NeedNonBurning;
			}
			if (base.IsMortar)
			{
				targetScanFlags |= TargetScanFlags.NeedNotUnderThickRoof;
			}
			result = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, IsValidTarget);
		}
		return result;
	}

	protected override void Tick()
	{
		base.Tick();
		if (forcedTarget.IsValid && !CanSetForcedTarget)
		{
			ResetForcedTarget();
		}
		if (!base.CanToggleHoldFire)
		{
			holdFire = false;
		}
		if (forcedTarget.ThingDestroyed)
		{
			ResetForcedTarget();
		}
		if (base.Active && !base.IsStunned && base.Spawned)
		{
			base.GunCompEq.verbTracker.VerbsTick();
			if (AttackVerb.state == VerbState.Bursting)
			{
				return;
			}
			burstActivated = false;
			if (base.WarmingUp && turrettop.CurRotation == turrettop.DestRotation)
			{
				burstWarmupTicksLeft--;
				if (burstWarmupTicksLeft == 0)
				{
					BeginBurst();
				}
			}
			else
			{
				if (burstCooldownTicksLeft > 0)
				{
					burstCooldownTicksLeft--;
				}
				if (burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
				{
					TryStartShootSomething(canBeginBurstImmediately: true);
				}
			}
			turrettop.TurretTopTick();
		}
		else
		{
			ResetCurrentTarget();
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref HasAESARadar, "HasRadar", defaultValue: false);
	}
}
