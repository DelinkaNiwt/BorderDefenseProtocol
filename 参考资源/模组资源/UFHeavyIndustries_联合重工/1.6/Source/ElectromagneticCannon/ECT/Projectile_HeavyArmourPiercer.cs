using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ECT;

public class Projectile_HeavyArmourPiercer : Bullet
{
	private bool isSkimming = false;

	private bool homingLost = false;

	private Vector3 trueOrigin;

	private Dictionary<int, int> hitCounts = new Dictionary<int, int>();

	private int lastLightningTick = -999;

	private int lastSoundTick = -999;

	private HashSet<IntVec3> visitedCellsCache = new HashSet<IntVec3>();

	private List<Thing> tmpThings = new List<Thing>();

	private ModExtension_HeavyArmourPiercer Stats => def.GetModExtension<ModExtension_HeavyArmourPiercer>();

	private float HitWidthRadius => Stats?.hitWidthRadius ?? 1.5f;

	private IntRange SparkCount => Stats?.sparkCountRange ?? new IntRange(5, 10);

	private FloatRange SparkSpeed => Stats?.sparkSpeedRange ?? new FloatRange(10f, 20f);

	private FleckDef SparkFleck => Stats?.sparkFleckDef ?? FleckDefOf.MicroSparks;

	private float KnockbackDist => Stats?.knockbackDistance ?? 3f;

	private int StunTicks => Stats?.stunDuration ?? 120;

	private int MaxHitsPerTarget => Stats?.maxHitsPerTarget ?? 1;

	private float LightningLength => Stats?.lightningLength ?? 20f;

	private int LightningDuration => Stats?.lightningDuration ?? 20;

	private int LightningGrowthTicks => Stats?.lightningGrowthTicks ?? 5;

	private float LightningVariance => Stats?.lightningVariance ?? 1f;

	private float LightningWidth => Stats?.lightningWidth ?? 2.5f;

	private float MaxHomingAngle => Stats?.maxHomingAngle ?? 45f;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref isSkimming, "isSkimming", defaultValue: false);
		Scribe_Values.Look(ref homingLost, "homingLost", defaultValue: false);
		Scribe_Values.Look(ref trueOrigin, "trueOrigin");
		Scribe_Collections.Look(ref hitCounts, "hitCounts", LookMode.Value, LookMode.Value);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && hitCounts == null)
		{
			hitCounts = new Dictionary<int, int>();
		}
	}

	public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
	{
		base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
		isSkimming = false;
		homingLost = false;
		hitCounts.Clear();
		trueOrigin = origin;
		lastLightningTick = Find.TickManager.TicksGame;
		lastSoundTick = -999;
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		if (isSkimming)
		{
			if (hitThing != null && hitThing.def.passability == Traversability.Impassable)
			{
				Vector3 currentDirection = GetCurrentDirection();
				DoSkimmingVisualsAt(hitThing.DrawPos, base.Map, currentDirection, playSound: true, forceLightning: true, spawnSparks: true);
				DoFinalExplosion(hitThing.Position);
				Destroy();
			}
		}
		else if (blockedByShield)
		{
			DoSkimmingVisualsAt(ExactPosition, base.Map, GetCurrentDirection(), playSound: false, forceLightning: true, spawnSparks: true);
			StartSkimming();
		}
		else if (hitThing != null)
		{
			if (hitThing.def.passability == Traversability.Impassable)
			{
				Vector3 normalized = (hitThing.DrawPos - trueOrigin).Yto0().normalized;
				DoSkimmingVisualsAt(hitThing.DrawPos, base.Map, normalized, playSound: true, forceLightning: true, spawnSparks: true);
				DoFinalExplosion(hitThing.Position);
				Destroy();
				return;
			}
			Vector3 currentDirection2 = GetCurrentDirection();
			DoSkimmingVisualsAt(hitThing.DrawPos, base.Map, currentDirection2, playSound: true, forceLightning: true, spawnSparks: true);
			if (hitThing.thingIDNumber >= 0)
			{
				hitCounts[hitThing.thingIDNumber] = 1;
			}
			ApplyDamage(hitThing, currentDirection2, isFirstHit: true);
			StartSkimming();
		}
		else
		{
			StartSkimming();
		}
	}

	protected override void ImpactSomething()
	{
		if (!isSkimming)
		{
			StartSkimming();
		}
	}

	private Vector3 GetCurrentDirection()
	{
		Vector3 vector = (destination - origin).Yto0().normalized;
		if (vector == Vector3.zero)
		{
			vector = Vector3.forward;
		}
		return vector;
	}

	private void StartSkimming()
	{
		if (!isSkimming)
		{
			isSkimming = true;
			if (def.projectile.soundExplode != null)
			{
				def.projectile.soundExplode.PlayOneShot(this);
			}
			Vector3 vector = GetCurrentDirection();
			if (vector == Vector3.zero)
			{
				vector = (ExactPosition - trueOrigin).Yto0().normalized;
			}
			float num = base.Map.Size.x + base.Map.Size.z;
			origin = ExactPosition;
			destination = origin + vector * num;
			ticksToImpact = Mathf.CeilToInt(num / def.projectile.SpeedTilesPerTick);
			if (ticksToImpact < 1)
			{
				ticksToImpact = 1;
			}
		}
	}

	protected override void Tick()
	{
		if (!isSkimming && !homingLost && intendedTarget.Thing is Pawn { Dead: false, Spawned: not false } pawn && pawn.Map == base.Map)
		{
			Vector3 drawPos = pawn.DrawPos;
			if ((destination - drawPos).MagnitudeHorizontalSquared() > 0.01f)
			{
				Vector3 vector = (destination - origin).Yto0();
				if (vector == Vector3.zero)
				{
					vector = (destination - trueOrigin).Yto0();
				}
				Vector3 to = (drawPos - ExactPosition).Yto0();
				if (vector.sqrMagnitude > 0.1f && to.sqrMagnitude > 0.1f)
				{
					float num = Vector3.Angle(vector, to);
					if (num > MaxHomingAngle)
					{
						homingLost = true;
					}
					else
					{
						origin = ExactPosition;
						destination = drawPos;
						ticksToImpact = Mathf.CeilToInt(base.StartingTicksToImpact);
						if (ticksToImpact < 1)
						{
							ticksToImpact = 1;
						}
					}
				}
			}
		}
		Vector3 exactPosition = ExactPosition;
		base.Tick();
		if (!base.Destroyed && isSkimming)
		{
			if (!base.Position.InBounds(base.Map))
			{
				Destroy();
			}
			else
			{
				DoSkimmingDamage(exactPosition, ExactPosition);
			}
		}
	}

	private void DoSkimmingDamage(Vector3 start, Vector3 end)
	{
		Vector3 vector = end - start;
		Vector3 normalized = (destination - origin).normalized;
		float num = vector.magnitude;
		if (num < 0.1f)
		{
			num = 0.1f;
		}
		float num2 = 0.5f;
		int num3 = Mathf.CeilToInt(num / num2);
		visitedCellsCache.Clear();
		for (int i = 0; i <= num3; i++)
		{
			float num4 = (float)i * num2;
			if (num4 > num)
			{
				num4 = num;
			}
			Vector3 vector2 = start + normalized * num4;
			IntVec3 center = vector2.ToIntVec3();
			foreach (IntVec3 item in GenRadial.RadialCellsAround(center, HitWidthRadius, useCenter: true))
			{
				if (!item.InBounds(base.Map) || !visitedCellsCache.Add(item))
				{
					continue;
				}
				List<Thing> thingList = item.GetThingList(base.Map);
				tmpThings.Clear();
				tmpThings.AddRange(thingList);
				for (int num5 = tmpThings.Count - 1; num5 >= 0; num5--)
				{
					Thing thing = tmpThings[num5];
					if (!thing.Destroyed && (thing is Pawn || thing is Building))
					{
						int num6 = 0;
						if (hitCounts.TryGetValue(thing.thingIDNumber, out var value))
						{
							num6 = value;
						}
						if (num6 < MaxHitsPerTarget)
						{
							if (thing.def.passability == Traversability.Impassable)
							{
								float num7 = (thing.Position.ToVector3Shifted() - vector2).MagnitudeHorizontal();
								if (num7 < 0.9f)
								{
									DoSkimmingVisualsAt(thing.DrawPos, thing.Map, normalized, playSound: true, forceLightning: true, spawnSparks: true);
									DoFinalExplosion(thing.Position);
									Destroy();
									return;
								}
							}
							else
							{
								hitCounts[thing.thingIDNumber] = num6 + 1;
								DoSkimmingVisualsAt(thing.DrawPos, thing.Map, normalized, playSound: true, forceLightning: true, spawnSparks: true);
								bool isFirstHit = num6 == 0;
								ApplyDamage(thing, normalized, isFirstHit);
							}
						}
					}
				}
			}
			tmpThings.Clear();
			if (base.Destroyed)
			{
				break;
			}
		}
	}

	private void DoSkimmingVisualsAt(Vector3 drawPos, Map map, Vector3 flightDir, bool playSound, bool forceLightning, bool spawnSparks)
	{
		if (map == null)
		{
			return;
		}
		if (playSound && Stats?.skimHitSound != null && Find.TickManager.TicksGame > lastSoundTick)
		{
			lastSoundTick = Find.TickManager.TicksGame;
			SoundInfo info = SoundInfo.InMap(new TargetInfo(drawPos.ToIntVec3(), map));
			Stats.skimHitSound.PlayOneShot(info);
		}
		if (spawnSparks)
		{
			float num = flightDir.AngleFlat();
			int randomInRange = SparkCount.RandomInRange;
			float num2 = Stats?.sparkSpreadAngle ?? 30f;
			FleckDef sparkFleck = SparkFleck;
			for (int i = 0; i < randomInRange; i++)
			{
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(drawPos, map, sparkFleck, Rand.Range(3f, 6f));
				dataStatic.velocityAngle = num + Rand.Range(0f - num2, num2);
				dataStatic.velocitySpeed = SparkSpeed.RandomInRange;
				map.flecks.CreateFleck(dataStatic);
			}
		}
		if (Find.TickManager.TicksGame - lastLightningTick > 3)
		{
			lastLightningTick = Find.TickManager.TicksGame;
			if (map.weatherManager != null)
			{
				Vector3 start = drawPos;
				start.y = AltitudeLayer.Weather.AltitudeFor();
				WeatherEvent_LightningTrail newEvent = new WeatherEvent_LightningTrail(map, start, flightDir, LightningLength, LightningDuration, LightningGrowthTicks, LightningVariance, LightningWidth);
				map.weatherManager.eventHandler.AddEvent(newEvent);
				float size = (forceLightning ? 3f : 2.5f);
				FleckMaker.ThrowLightningGlow(drawPos, map, size);
			}
		}
	}

	private void ApplyDamage(Thing t, Vector3 flightDir, bool isFirstHit)
	{
		if (t is Pawn pawn)
		{
			if (isFirstHit)
			{
				ShatterRandomGear(pawn);
			}
			DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef);
			pawn.TakeDamage(dinfo);
			if (!pawn.Dead && !pawn.Destroyed)
			{
				if (isFirstHit)
				{
					KnockbackTarget(pawn, flightDir);
				}
				float num = (float)StunTicks / 30f;
				if (num < 0.1f)
				{
					num = 0.5f;
				}
				DamageInfo dinfo2 = new DamageInfo(DamageDefOf.Stun, num, 999f, ExactRotation.eulerAngles.y, launcher, null, equipmentDef);
				pawn.TakeDamage(dinfo2);
			}
		}
		else
		{
			DamageInfo dinfo3 = new DamageInfo(def.projectile.damageDef, DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef);
			t.TakeDamage(dinfo3);
		}
	}

	private void DoFinalExplosion(IntVec3 pos)
	{
		float explosionRadius = def.projectile.explosionRadius;
		if (!(explosionRadius <= 0f))
		{
			Map map = base.Map;
			DamageDef damageDef = def.projectile.damageDef;
			Thing instigator = launcher;
			int damageAmount = DamageAmount;
			float armorPenetration = ArmorPenetration;
			SoundDef soundExplode = def.projectile.soundExplode;
			ThingDef weapon = equipmentDef;
			ThingDef projectile = def;
			Thing thing = intendedTarget.Thing;
			ThingDef postExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDef;
			float postExplosionSpawnChance = def.projectile.postExplosionSpawnChance;
			int postExplosionSpawnThingCount = def.projectile.postExplosionSpawnThingCount;
			ThingDef preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
			float preExplosionSpawnChance = def.projectile.preExplosionSpawnChance;
			int preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
			float explosionChanceToStartFire = def.projectile.explosionChanceToStartFire;
			bool explosionDamageFalloff = def.projectile.explosionDamageFalloff;
			GenExplosion.DoExplosion(pos, map, explosionRadius, damageDef, instigator, damageAmount, armorPenetration, soundExplode, weapon, projectile, thing, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, null, null, 255, applyDamageToExplosionCellsNeighbors: false, preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount, explosionChanceToStartFire, explosionDamageFalloff);
		}
	}

	private void KnockbackTarget(Pawn p, Vector3 direction)
	{
		float knockbackDist = KnockbackDist;
		IntVec3 position = p.Position;
		IntVec3 intVec = position;
		IntVec3 intVec2 = position;
		for (int i = 1; i <= (int)knockbackDist; i++)
		{
			IntVec3 intVec3 = position + (direction * i).ToIntVec3();
			if (intVec3.InBounds(base.Map) && intVec3.Walkable(base.Map))
			{
				intVec2 = intVec3;
				continue;
			}
			break;
		}
		if (intVec2 != position)
		{
			p.Position = intVec2;
			p.Notify_Teleported();
			FleckMaker.ThrowDustPuff(position, base.Map, 1f);
			FleckMaker.ThrowDustPuff(intVec2, base.Map, 1f);
			if (p.jobs != null)
			{
				p.jobs.StopAll();
			}
		}
	}

	private void ShatterRandomGear(Pawn p)
	{
		if (p.Dead || p.Destroyed || p.RaceProps.IsMechanoid || (p.apparel == null && p.equipment == null))
		{
			return;
		}
		List<Thing> list = new List<Thing>();
		if (p.apparel != null && p.apparel.WornApparelCount > 0)
		{
			list.AddRange(p.apparel.WornApparel);
		}
		if (p.equipment != null && p.equipment.Primary != null)
		{
			list.Add(p.equipment.Primary);
		}
		if (list.TryRandomElement(out var result))
		{
			SoundDefOf.Crunch.PlayOneShot(new TargetInfo(p.Position, p.Map));
			FleckMaker.ThrowMicroSparks(p.DrawPos, p.Map);
			FleckMaker.ThrowDustPuffThick(p.DrawPos, p.Map, 1f, Color.gray);
			if (result is Apparel apparel)
			{
				p.apparel.Remove(apparel);
				apparel.Destroy();
			}
			else if (result is ThingWithComps eq && p.equipment != null)
			{
				p.equipment.DestroyEquipment(eq);
			}
			else if (!result.Destroyed)
			{
				result.Destroy();
			}
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		float num = 0f;
		if (!isSkimming)
		{
			float arcHeightFactor = def.projectile.arcHeightFactor;
			float num2 = (destination - trueOrigin).MagnitudeHorizontal();
			float num3 = (drawLoc - trueOrigin).MagnitudeHorizontal();
			float x = ((num2 > 0f) ? (num3 / num2) : 0f);
			num = arcHeightFactor * GenMath.InverseParabola(x);
		}
		Vector3 vector = drawLoc + new Vector3(0f, 0f, 1f) * num;
		Quaternion rotation = ExactRotation;
		if (def.projectile.spinRate != 0f)
		{
			float num4 = 60f / def.projectile.spinRate;
			rotation = Quaternion.AngleAxis((float)Find.TickManager.TicksGame % num4 / num4 * 360f, Vector3.up);
		}
		if (def.projectile.useGraphicClass)
		{
			Graphic.Draw(vector, base.Rotation, this, rotation.eulerAngles.y);
		}
		else
		{
			Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), vector, rotation, DrawMat, 0);
		}
		if (def.projectile.shadowSize > 0f && num > 0f)
		{
			DrawShadow(drawLoc, num);
		}
		Comps_PostDraw();
	}

	private void DrawShadow(Vector3 drawLoc, float height)
	{
		Material material = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent);
		float num = def.projectile.shadowSize * Mathf.Lerp(1f, 0.6f, height);
		Vector3 s = new Vector3(num, 1f, num);
		Vector3 vector = new Vector3(0f, -0.01f, 0f);
		Matrix4x4 matrix = default(Matrix4x4);
		matrix.SetTRS(drawLoc + vector, Quaternion.identity, s);
		Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
	}
}
