using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HNGT;

[StaticConstructorOnStartup]
public class HighOrbitAttack : OrbitalStrike
{
	public float impactAreaRadius = 15f;

	public FloatRange explosionRadiusRange = new FloatRange(6f, 8f);

	public int bombIntervalTicks = 18;

	public int explosionCount = 30;

	public int warmupTicks = 60;

	private int ticksToNextEffect;

	private IntVec3 nextExplosionCell = IntVec3.Invalid;

	private List<Bombardment.BombardmentProjectile> projectiles = new List<Bombardment.BombardmentProjectile>();

	private int projectileFlyTimeTicks = 60;

	private Material cachedProjectileMaterial = null;

	public static readonly SimpleCurve DistanceChanceFactor = new SimpleCurve
	{
		new CurvePoint(0f, 1f),
		new CurvePoint(1f, 0.1f)
	};

	private ModExtension_HighOrbitAttack ExtProps => def.GetModExtension<ModExtension_HighOrbitAttack>();

	public override void SpawnSetup(Map map, bool respawningAfterReload)
	{
		if (ExtProps == null)
		{
			Log.Error("HNGT: " + def.defName + " lack ModExtension_HighOrbitAttack.");
		}
		if (ExtProps != null && string.IsNullOrEmpty(ExtProps.projectileDefName))
		{
			Log.Error("HNGT: " + def.defName + " ModExtension_HighOrbitAttack lack 'projectileDefName'.");
		}
		if (ExtProps != null)
		{
			impactAreaRadius = ExtProps.impactAreaRadius;
			explosionCount = ExtProps.explosionCount;
			bombIntervalTicks = ExtProps.bombIntervalTicks;
			warmupTicks = ExtProps.warmupTicks;
			projectileFlyTimeTicks = ExtProps.projectileFlyTimeTicks;
		}
		base.SpawnSetup(map, respawningAfterReload);
		if (!respawningAfterReload)
		{
			GetNextExplosionCell();
		}
		string texturePath = "Things/Projectile/Bullet_Big";
		if (ExtProps != null && !string.IsNullOrEmpty(ExtProps.projectileTexturePath))
		{
			texturePath = ExtProps.projectileTexturePath;
		}
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			cachedProjectileMaterial = MaterialPool.MatFrom(texturePath, ShaderDatabase.Transparent, Color.white);
		});
	}

	public override void StartStrike()
	{
		duration = bombIntervalTicks * explosionCount;
		base.StartStrike();
	}

	protected override void Tick()
	{
		if (base.Destroyed)
		{
			return;
		}
		if (warmupTicks > 0)
		{
			warmupTicks--;
			if (warmupTicks <= 0)
			{
				StartStrike();
			}
		}
		else
		{
			base.Tick();
		}
		EffectTick();
	}

	private void EffectTick()
	{
		if (!nextExplosionCell.IsValid)
		{
			ticksToNextEffect = warmupTicks;
			GetNextExplosionCell();
		}
		if (warmupTicks <= 0)
		{
			ticksToNextEffect--;
		}
		if (ticksToNextEffect <= 0 && base.TicksLeft >= bombIntervalTicks)
		{
			float volumeFactor = 1f;
			if (ExtProps != null)
			{
				volumeFactor = ExtProps.preImpactSoundVolume;
			}
			SoundInfo info = SoundInfo.InMap(new TargetInfo(nextExplosionCell, base.Map));
			info.volumeFactor = volumeFactor;
			SoundDefOf.Bombardment_PreImpact.PlayOneShot(info);
			projectiles.Add(new Bombardment.BombardmentProjectile(projectileFlyTimeTicks, nextExplosionCell));
			ticksToNextEffect = bombIntervalTicks;
			GetNextExplosionCell();
		}
		for (int num = projectiles.Count - 1; num >= 0; num--)
		{
			projectiles[num].Tick();
			if (projectiles[num].LifeTime <= 0)
			{
				TryDoCustomExplosion(projectiles[num]);
				projectiles.RemoveAt(num);
			}
		}
	}

	private void TryDoCustomExplosion(Bombardment.BombardmentProjectile proj)
	{
		if (ExtProps == null || string.IsNullOrEmpty(ExtProps.projectileDefName))
		{
			return;
		}
		ThingDef named = DefDatabase<ThingDef>.GetNamed(ExtProps.projectileDefName, errorOnFail: false);
		if (named == null || named.projectile == null)
		{
			Log.ErrorOnce("HNGT: HighOrbitAttack can not find '" + ExtProps.projectileDefName + "' projectileDef.", def.defName.GetHashCode());
			return;
		}
		ProjectileProperties_CompoundExplosion projectileProperties_CompoundExplosion = named.projectile as ProjectileProperties_CompoundExplosion;
		IntVec3 targetCell = proj.targetCell;
		Map map = base.Map;
		Thing weapon = instigator;
		ThingDef weapon2 = weaponDef;
		ThingDef thingDef = def;
		if (projectileProperties_CompoundExplosion != null)
		{
			GenExplosion.DoExplosion(targetCell, map, projectileProperties_CompoundExplosion.explosionRadius, projectileProperties_CompoundExplosion.damageDef, weapon, projectileProperties_CompoundExplosion.GetDamageAmount(weapon), projectileProperties_CompoundExplosion.GetArmorPenetration(weapon), projectileProperties_CompoundExplosion.soundExplode, weapon2, named, null, projectileProperties_CompoundExplosion.postExplosionSpawnThingDef, projectileProperties_CompoundExplosion.postExplosionSpawnChance, projectileProperties_CompoundExplosion.postExplosionSpawnThingCount, projectileProperties_CompoundExplosion.postExplosionGasType, null, 255, projectileProperties_CompoundExplosion.applyDamageToExplosionCellsNeighbors, projectileProperties_CompoundExplosion.preExplosionSpawnThingDef, projectileProperties_CompoundExplosion.preExplosionSpawnChance, projectileProperties_CompoundExplosion.preExplosionSpawnThingCount, projectileProperties_CompoundExplosion.explosionChanceToStartFire, projectileProperties_CompoundExplosion.explosionDamageFalloff, null, null, null, projectileProperties_CompoundExplosion.doExplosionVFX, projectileProperties_CompoundExplosion.damageDef.expolosionPropagationSpeed, 0f, doSoundEffects: true, projectileProperties_CompoundExplosion.postExplosionSpawnThingDefWater, projectileProperties_CompoundExplosion.screenShakeFactor, null, null, projectileProperties_CompoundExplosion.postExplosionSpawnSingleThingDef, projectileProperties_CompoundExplosion.preExplosionSpawnSingleThingDef);
			if (projectileProperties_CompoundExplosion.additionalExplosions == null)
			{
				return;
			}
			{
				foreach (ExplosionParams additionalExplosion in projectileProperties_CompoundExplosion.additionalExplosions)
				{
					if (additionalExplosion.damageDef != null && !(additionalExplosion.radius <= 0f))
					{
						GenExplosion.DoExplosion(targetCell, map, additionalExplosion.radius, additionalExplosion.damageDef, weapon, additionalExplosion.damageAmount, additionalExplosion.armorPenetration, additionalExplosion.soundExplode, weapon2, named, null, null, 0f, 1, null, null, 255, projectileProperties_CompoundExplosion.applyDamageToExplosionCellsNeighbors, null, 0f, 1, (additionalExplosion.damageDef.defName.ToLower() == "flame") ? 0.5f : 0f, projectileProperties_CompoundExplosion.explosionDamageFalloff, null, null, null, projectileProperties_CompoundExplosion.doExplosionVFX, additionalExplosion.damageDef.expolosionPropagationSpeed, 0f, doSoundEffects: true, null, projectileProperties_CompoundExplosion.screenShakeFactor);
					}
				}
				return;
			}
		}
		GenExplosion.DoExplosion(targetCell, map, named.projectile.explosionRadius, named.projectile.damageDef, weapon, named.projectile.GetDamageAmount(weapon), named.projectile.GetArmorPenetration(weapon), named.projectile.soundExplode, weapon2, named, null, named.projectile.postExplosionSpawnThingDef, named.projectile.postExplosionSpawnChance, named.projectile.postExplosionSpawnThingCount, named.projectile.postExplosionGasType, null, 255, named.projectile.applyDamageToExplosionCellsNeighbors, named.projectile.preExplosionSpawnThingDef, named.projectile.preExplosionSpawnChance, named.projectile.preExplosionSpawnThingCount, named.projectile.explosionChanceToStartFire, named.projectile.explosionDamageFalloff, null, null, null, named.projectile.doExplosionVFX, named.projectile.damageDef.expolosionPropagationSpeed, 0f, doSoundEffects: true, named.projectile.postExplosionSpawnThingDefWater, named.projectile.screenShakeFactor, null, null, named.projectile.postExplosionSpawnSingleThingDef, named.projectile.preExplosionSpawnSingleThingDef);
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		if (!(cachedProjectileMaterial == null) && !projectiles.NullOrEmpty())
		{
			for (int i = 0; i < projectiles.Count; i++)
			{
				projectiles[i].Draw(cachedProjectileMaterial);
			}
		}
	}

	private void GetNextExplosionCell()
	{
		nextExplosionCell = (from x in GenRadial.RadialCellsAround(base.Position, impactAreaRadius, useCenter: true)
			where x.InBounds(base.Map)
			select x).RandomElementByWeight((IntVec3 x) => DistanceChanceFactor.Evaluate(x.DistanceTo(base.Position) / impactAreaRadius));
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref impactAreaRadius, "impactAreaRadius", 15f);
		Scribe_Values.Look(ref explosionRadiusRange, "explosionRadiusRange", new FloatRange(6f, 8f));
		Scribe_Values.Look(ref bombIntervalTicks, "bombIntervalTicks", 18);
		Scribe_Values.Look(ref explosionCount, "explosionCount", 30);
		Scribe_Values.Look(ref warmupTicks, "warmupTicks", 0);
		Scribe_Values.Look(ref projectileFlyTimeTicks, "projectileFlyTimeTicks", 60);
		Scribe_Values.Look(ref ticksToNextEffect, "ticksToNextEffect", 0);
		Scribe_Values.Look(ref nextExplosionCell, "nextExplosionCell");
		Scribe_Collections.Look(ref projectiles, "projectiles", LookMode.Deep);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (!nextExplosionCell.IsValid)
			{
				GetNextExplosionCell();
			}
			if (projectiles == null)
			{
				projectiles = new List<Bombardment.BombardmentProjectile>();
			}
		}
	}
}
