using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded.Staticlord;

public class BallLightning : AbilityProjectile
{
	private const int WARMUP = 180;

	private List<Thing> currentTargets = new List<Thing>();

	private int ticksTillAttack = -1;

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		((ThingWithComps)this).SpawnSetup(map, respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			ticksTillAttack = 180;
		}
	}

	protected override void Tick()
	{
		((Projectile)this).Tick();
		if (!((Thing)this).Spawned)
		{
			return;
		}
		ticksTillAttack--;
		if (ticksTillAttack > 0)
		{
			return;
		}
		currentTargets.Clear();
		foreach (Thing item in (from t in GenRadial.RadialDistinctThingsAround(((Projectile)(object)this).ExactPosition.ToIntVec3(), ((Thing)this).Map, base.ability.GetRadiusForPawn(), useCenter: true)
			where t.HostileTo(((Projectile)this).launcher)
			select t).Take(Mathf.FloorToInt(base.ability.GetPowerForPawn())))
		{
			currentTargets.Add(item);
			BattleLogEntry_RangedImpact log = new BattleLogEntry_RangedImpact(((Projectile)this).launcher, item, item, ((Thing)this).def, VPE_DefOf.VPE_Bolt, ((Projectile)this).targetCoverDef);
			item.TakeDamage(new DamageInfo(DamageDefOf.Flame, 12f, 5f, ((Thing)(object)this).DrawPos.AngleToFlat(item.DrawPos), (Thing)(object)this)).AssociateWithLog(log);
			item.TakeDamage(new DamageInfo(DamageDefOf.EMP, 20f, 5f, ((Thing)(object)this).DrawPos.AngleToFlat(item.DrawPos), (Thing)(object)this)).AssociateWithLog(log);
			VPE_DefOf.VPE_BallLightning_Zap.PlayOneShot(item);
		}
		ticksTillAttack = 60;
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		((Projectile)this).DrawAt(drawLoc, flip);
		Vector3 vector = drawLoc.Yto0() + new Vector3(1f, 0f, 0f).RotatedBy(((Projectile)this).origin.AngleToFlat(((Projectile)this).destination));
		Graphic graphic = VPE_DefOf.VPE_ChainBolt.graphicData.Graphic;
		foreach (Thing currentTarget in currentTargets)
		{
			Vector3 vector2 = currentTarget.DrawPos.Yto0();
			Vector3 s = new Vector3(graphic.drawSize.x, 1f, (vector2 - vector).magnitude);
			Matrix4x4 matrix = Matrix4x4.TRS(vector + (vector2 - vector) / 2f + Vector3.up * (((Thing)this).def.Altitude - 0.018292684f), Quaternion.LookRotation(vector2 - vector), s);
			UnityEngine.Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
		}
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		GenExplosion.DoExplosion(((Thing)this).Position, ((Thing)this).Map, ((Thing)this).def.projectile.explosionRadius, ((Thing)this).def.projectile.damageDef, ((Projectile)this).launcher, ((Projectile)(object)this).DamageAmount, ((Projectile)(object)this).ArmorPenetration, ((Thing)this).def.projectile.soundExplode, ((Projectile)this).equipmentDef, ((Thing)this).def, ((Projectile)this).intendedTarget.Thing, ((Thing)this).def.projectile.postExplosionSpawnThingDef, ((Thing)this).def.projectile.postExplosionSpawnChance, ((Thing)this).def.projectile.postExplosionSpawnThingCount, ((Thing)this).def.projectile.postExplosionGasType, ((Thing)this).def.projectile.postExplosionSpawnChance, 255, ((Thing)this).def.projectile.applyDamageToExplosionCellsNeighbors, ((Thing)this).def.projectile.preExplosionSpawnThingDef, ((Thing)this).def.projectile.preExplosionSpawnChance, ((Thing)this).def.projectile.preExplosionSpawnThingCount, ((Thing)this).def.projectile.explosionChanceToStartFire, ((Thing)this).def.projectile.explosionDamageFalloff, ((Projectile)this).origin.AngleToFlat(((Projectile)this).destination));
		((AbilityProjectile)this).Impact(hitThing, blockedByShield);
	}

	public override void ExposeData()
	{
		((AbilityProjectile)this).ExposeData();
		Scribe_Values.Look(ref ticksTillAttack, "ticksTillAttack", 0);
		Scribe_Collections.Look(ref currentTargets, "currentTargets", LookMode.Reference);
	}
}
