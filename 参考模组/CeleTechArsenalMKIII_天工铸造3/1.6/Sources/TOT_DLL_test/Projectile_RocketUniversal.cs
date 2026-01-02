using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Projectile_RocketUniversal : Projectile_Explosive
{
	public ProjectileExtension_CMC modExtension2;

	public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_FireGlow_Exp");

	public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_ProjectileSmoke_LongLasting");

	private static readonly Material shadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.DefaultShader);

	public int Fleck_MakeFleckTickMax = 1;

	public IntRange Fleck_MakeFleckNum = new IntRange(5, 7);

	public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);

	public FloatRange Fleck_Speed = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);

	public int Fleck_MakeFleckTick;

	public int livecount = 0;

	public bool ThrowLauncherFlag = true;

	private int ticksToDetonation;

	private Vector3 lastPos;

	private Quaternion adjustedRot
	{
		get
		{
			if (!(lastPos == Vector3.zero))
			{
				return Quaternion.LookRotation((DrawPos - lastPos).Yto0());
			}
			return ExactRotation;
		}
	}

	private float ArcHeightFactor
	{
		get
		{
			float num = def.projectile.arcHeightFactor;
			float num2 = (destination - origin).MagnitudeHorizontalSquared();
			if (num * num > num2 * 0.2f * 0.2f)
			{
				num = Mathf.Sqrt(num2) * 0.2f;
			}
			return num;
		}
	}

	public override Vector3 DrawPos => ExactPosition + new Vector3(0f, 0f, 1f) * (ArcHeightFactor * GenMath.InverseParabola(base.DistanceCoveredFraction));

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		modExtension2 = def.GetModExtension<ProjectileExtension_CMC>();
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), DrawPos, adjustedRot, DrawMat, 0);
		Comps_PostDraw();
	}

	protected override void Tick()
	{
		base.Position = DrawPos.ToIntVec3();
		lastPos = DrawPos;
		Fleck_MakeFleckTick++;
		livecount++;
		if (ticksToDetonation > 0)
		{
			ticksToDetonation--;
			if (ticksToDetonation <= 0)
			{
				Explode();
			}
		}
		if (base.DistanceCoveredFraction > 0.99f)
		{
			ImpactSomething();
			return;
		}
		if (Fleck_MakeFleckTick >= Fleck_MakeFleckTickMax)
		{
			Fleck_MakeFleckTick = 0;
			Map map = base.Map;
			int num = Mathf.Clamp(Fleck_MakeFleckNum.RandomInRange - (int)(10f * base.DistanceCoveredFraction), 1, 7);
			Vector3 drawPos = DrawPos;
			Vector3 vector = lastPos;
			drawPos.y = AltitudeLayer.Projectile.AltitudeFor();
			vector.y = AltitudeLayer.Projectile.AltitudeFor();
			for (int i = 0; i < num; i++)
			{
				float num2 = (drawPos - intendedTarget.CenterVector3).AngleFlat();
				float velocityAngle = Fleck_Angle.RandomInRange + num2;
				float scale = def.graphicData.drawSize.x / 1.92f;
				float randomInRange = Fleck_Speed2.RandomInRange;
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(drawPos, map, FleckDef2, scale);
				dataStatic.rotationRate = Fleck_Rotation.RandomInRange;
				dataStatic.velocityAngle = velocityAngle;
				dataStatic.velocitySpeed = randomInRange;
				dataStatic.rotation = Rand.Range(0, 360);
				map.flecks.CreateFleck(dataStatic);
			}
		}
		base.Tick();
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		if (blockedByShield || def.projectile.explosionDelay == 0)
		{
			for (int i = 0; i < 6; i++)
			{
				float num = Rand.Range(-180f, 180f);
				float scale = Rand.Range(2.5f, 3.3f);
				float velocitySpeed = Rand.Range(0.5f, 1.3f);
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(base.Position.ToVector3(), base.Map, FleckDef2, scale);
				dataStatic.rotationRate = Fleck_Rotation.RandomInRange;
				dataStatic.velocityAngle = Rand.Range(-180f, 180f);
				dataStatic.velocitySpeed = velocitySpeed;
				dataStatic.rotation = Rand.Range(0, 360);
				base.Map.flecks.CreateFleck(dataStatic);
			}
			ModExtentsion_Fragments modExtension = def.GetModExtension<ModExtentsion_Fragments>();
			FleckMaker.ThrowFireGlow(base.Position.ToVector3Shifted(), base.Map, 3.5f);
			FleckMaker.ThrowSmoke(base.Position.ToVector3Shifted(), base.Map, 5.5f);
			FleckMaker.ThrowHeatGlow(base.Position, base.Map, 3.5f);
			if (modExtension != null)
			{
				IntVec3 intVec = IntVec3.FromVector3(base.Position.ToVector3Shifted());
				IEnumerable<IntVec3> enumerable = GenRadial.RadialCellsAround(intVec, modExtension.radius, useCenter: true);
				foreach (IntVec3 item in enumerable)
				{
					if (item.InBounds(base.Map) && Rand.Chance(0.1f * Mathf.Sqrt((item - intVec).Magnitude / (float)modExtension.radius)))
					{
						ProjectileHitFlags hitFlags = ProjectileHitFlags.All;
						Projectile newThing = ThingMaker.MakeThing(CMC_Def.Bullet_CMC_Fragments) as Projectile;
						Projectile projectile = (Projectile)GenSpawn.Spawn(newThing, base.Position, base.Map);
						projectile.Launch(launcher, base.Position.ToVector3Shifted(), item, item, hitFlags, preventFriendlyFire, null, targetCoverDef);
					}
				}
			}
			Explode();
		}
		else
		{
			landed = true;
			ticksToDetonation = def.projectile.explosionDelay;
			GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, def.projectile.damageDef, launcher.Faction, launcher);
		}
	}

	protected override void Explode()
	{
		Map map = base.Map;
		IntVec3 position = base.Position;
		Map map2 = map;
		float explosionRadius = def.projectile.explosionRadius;
		DamageDef damageDef = def.projectile.damageDef;
		Thing instigator = launcher;
		int damageAmount = DamageAmount;
		float armorPenetration = ArmorPenetration;
		SoundDef soundExplode = def.projectile.soundExplode;
		ThingDef weapon = equipmentDef;
		ThingDef projectile = def;
		ThingDef postExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDef ?? def.projectile.filth;
		ThingDef postExplosionSpawnThingDefWater = def.projectile.postExplosionSpawnThingDefWater;
		float postExplosionSpawnChance = def.projectile.postExplosionSpawnChance;
		int postExplosionSpawnThingCount = def.projectile.postExplosionSpawnThingCount;
		GasType? postExplosionGasType = def.projectile.postExplosionGasType;
		ThingDef preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
		float preExplosionSpawnChance = def.projectile.preExplosionSpawnChance;
		int preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
		bool applyDamageToExplosionCellsNeighbors = def.projectile.applyDamageToExplosionCellsNeighbors;
		ThingDef preExplosionSpawnThingDef2 = preExplosionSpawnThingDef;
		float preExplosionSpawnChance2 = preExplosionSpawnChance;
		int preExplosionSpawnThingCount2 = preExplosionSpawnThingCount;
		float explosionChanceToStartFire = def.projectile.explosionChanceToStartFire;
		bool explosionDamageFalloff = def.projectile.explosionDamageFalloff;
		float? direction = origin.AngleToFlat(destination);
		List<Thing> ignoredThings = null;
		FloatRange? affectedAngle = null;
		float expolosionPropagationSpeed = def.projectile.damageDef.expolosionPropagationSpeed;
		float screenShakeFactor = def.projectile.screenShakeFactor;
		if (def.projectile.explosionEffect != null)
		{
			Effecter effecter = def.projectile.explosionEffect.Spawn();
			if (def.projectile.explosionEffectLifetimeTicks != 0)
			{
				map.effecterMaintainer.AddEffecterToMaintain(effecter, base.Position.ToVector3().ToIntVec3(), def.projectile.explosionEffectLifetimeTicks);
			}
			else
			{
				effecter.Trigger(new TargetInfo(base.Position, map), new TargetInfo(base.Position, map));
				effecter.Cleanup();
			}
		}
		GenExplosion.DoExplosion(position, map2, explosionRadius, damageDef, instigator, damageAmount, armorPenetration, soundExplode, weapon, projectile, null, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, postExplosionGasType, null, 255, applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef2, preExplosionSpawnChance2, preExplosionSpawnThingCount2, explosionChanceToStartFire, explosionDamageFalloff, direction, ignoredThings, affectedAngle, def.projectile.doExplosionVFX, expolosionPropagationSpeed, 0f, doSoundEffects: true, postExplosionSpawnThingDefWater, screenShakeFactor);
		Destroy();
	}
}
