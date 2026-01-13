using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurbojetBackpack;

public class Projectile_BezierMissile : Bullet
{
	private Vector3 originPos;

	private Vector3 controlPoint1;

	private Vector3 controlPoint2;

	private bool initialized = false;

	private int ticksFlying = 0;

	private int estimatedTotalTicks = 60;

	private Vector3 previousDrawPos;

	private Vector3 myExactPosition;

	private Quaternion bezierRotation = Quaternion.identity;

	private BarrageExtension Ext => def.GetModExtension<BarrageExtension>();

	public override Vector3 ExactPosition => myExactPosition;

	public override Vector3 DrawPos => myExactPosition;

	public override Quaternion ExactRotation => bezierRotation;

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			myExactPosition = base.Position.ToVector3Shifted();
			previousDrawPos = myExactPosition;
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref originPos, "originPos");
		Scribe_Values.Look(ref controlPoint1, "controlPoint1");
		Scribe_Values.Look(ref controlPoint2, "controlPoint2");
		Scribe_Values.Look(ref ticksFlying, "ticksFlying", 0);
		Scribe_Values.Look(ref estimatedTotalTicks, "estimatedTotalTicks", 60);
		Scribe_Values.Look(ref initialized, "initialized", defaultValue: false);
		Scribe_Values.Look(ref myExactPosition, "myExactPosition");
	}

	private void InitializeBezier()
	{
		originPos = origin;
		float innerRadius = ((Ext != null) ? Ext.curveVarianceMin : 2f);
		float outerRadius = ((Ext != null) ? Ext.curveVarianceMax : 10f);
		Vector3 vector = (origin + destination) / 2f;
		controlPoint1 = origin + Rand.InsideAnnulusVector3(innerRadius, outerRadius);
		controlPoint2 = vector + Rand.InsideAnnulusVector3(innerRadius, outerRadius);
		float num = TurbojetBezierUtils.EstimateCurveLength(originPos, controlPoint1, controlPoint2, destination);
		float num2 = def.projectile.SpeedTilesPerTick;
		if (num2 <= 0f)
		{
			num2 = 1f;
		}
		estimatedTotalTicks = Mathf.CeilToInt(num / num2);
		if (estimatedTotalTicks < 2)
		{
			estimatedTotalTicks = 2;
		}
		bezierRotation = Quaternion.LookRotation((destination - origin).Yto0());
		myExactPosition = originPos;
		initialized = true;
	}

	protected override void Tick()
	{
		if (!initialized)
		{
			InitializeBezier();
		}
		if (base.Destroyed || base.Map == null)
		{
			return;
		}
		ticksFlying++;
		float num = Mathf.Clamp01((float)ticksFlying / (float)estimatedTotalTicks);
		Vector3 vector = TurbojetBezierUtils.CalculateThreePowerBezierPoint(num, originPos, controlPoint1, controlPoint2, destination);
		if (!CheckShieldIntercept(myExactPosition, vector) && (def.projectile.flyOverhead || !CheckCollisionAlongPath(myExactPosition, vector)))
		{
			myExactPosition = vector;
			IntVec3 intVec = myExactPosition.ToIntVec3();
			if (intVec != base.Position && intVec.InBounds(base.Map))
			{
				base.Position = intVec;
			}
			Vector3 v = TurbojetBezierUtils.CalculateBezierTangent(num, originPos, controlPoint1, controlPoint2, destination);
			if (v.sqrMagnitude > 0.001f)
			{
				bezierRotation = Quaternion.LookRotation(v.Yto0());
			}
			DrawComplexTrails(myExactPosition);
			previousDrawPos = myExactPosition;
			if (num >= 1f)
			{
				ImpactSomething();
			}
		}
	}

	private bool CheckShieldIntercept(Vector3 lastExactPos, Vector3 newExactPos)
	{
		List<Thing> list = base.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
		for (int i = 0; i < list.Count; i++)
		{
			CompProjectileInterceptor compProjectileInterceptor = list[i].TryGetComp<CompProjectileInterceptor>();
			if (compProjectileInterceptor.CheckIntercept(this, lastExactPos, newExactPos))
			{
				Impact(null, blockedByShield: true);
				return true;
			}
		}
		return false;
	}

	private bool CheckCollisionAlongPath(Vector3 start, Vector3 end)
	{
		if ((end - start).sqrMagnitude < 0.001f)
		{
			return false;
		}
		IntVec3 start2 = start.ToIntVec3();
		IntVec3 end2 = end.ToIntVec3();
		foreach (IntVec3 item in GenSight.PointsOnLineOfSight(start2, end2))
		{
			if (!item.InBounds(base.Map) || (item == origin.ToIntVec3() && ticksFlying < 5))
			{
				continue;
			}
			List<Thing> thingList = item.GetThingList(base.Map);
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				if (thing != this && thing != launcher && (thing.def.Fillage == FillCategory.Full || thing == intendedTarget.Thing || thing is Pawn))
				{
					Impact(thing);
					return true;
				}
			}
		}
		return false;
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Map map = base.Map;
		if (blockedByShield || map == null)
		{
			base.Impact(hitThing, blockedByShield);
			return;
		}
		ThingDef postExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDef;
		float postExplosionSpawnChance = ((postExplosionSpawnThingDef != null) ? def.projectile.postExplosionSpawnChance : 0f);
		if (def.projectile.explosionRadius > 0f)
		{
			IntVec3 position = base.Position;
			float explosionRadius = def.projectile.explosionRadius;
			DamageDef damageDef = def.projectile.damageDef;
			Thing instigator = launcher;
			int damageAmount = DamageAmount;
			float armorPenetration = ArmorPenetration;
			SoundDef soundExplode = def.projectile.soundExplode;
			ThingDef weapon = equipment?.def;
			ThingDef projectile = def;
			Thing thing = intendedTarget.Thing;
			GasType? postExplosionGasType = def.projectile.postExplosionGasType;
			bool applyDamageToExplosionCellsNeighbors = def.projectile.applyDamageToExplosionCellsNeighbors;
			GenExplosion.DoExplosion(position, map, explosionRadius, damageDef, instigator, damageAmount, armorPenetration, soundExplode, weapon, projectile, thing, postExplosionSpawnThingDef, postExplosionSpawnChance, 1, postExplosionGasType, null, 255, applyDamageToExplosionCellsNeighbors);
		}
		else
		{
			base.Impact(hitThing, blockedByShield);
		}
		if (Ext != null)
		{
			if (Ext.extraStun)
			{
				float radius = ((Ext.stunRadius > 0f) ? Ext.stunRadius : def.projectile.explosionRadius);
				GenExplosion.DoExplosion(base.Position, map, radius, DamageDefOf.Stun, launcher, Ext.stunAmount);
			}
			if (Ext.extraFire)
			{
				float radius2 = ((Ext.fireRadius > 0f) ? Ext.fireRadius : def.projectile.explosionRadius);
				GenExplosion.DoExplosion(base.Position, map, radius2, DamageDefOf.Flame, launcher, Ext.fireDamageAmount, 0f, null, null, null, null, ThingDefOf.Filth_Ash, 0.2f);
			}
		}
		Destroy();
	}

	private void DrawComplexTrails(Vector3 currentPos)
	{
		if (Ext == null || Ext.tailLayers.NullOrEmpty())
		{
			if (ticksFlying % 3 == 0)
			{
				FleckMaker.ThrowSmoke(currentPos, base.Map, 0.6f);
			}
			return;
		}
		Map map = base.Map;
		for (int i = 0; i < Ext.tailLayers.Count; i++)
		{
			TailData tailData = Ext.tailLayers[i];
			if (ticksFlying % Math.Max(1, tailData.interval) != 0)
			{
				continue;
			}
			Vector3 vector = currentPos;
			if (tailData.offsetZ != 0f)
			{
				vector.z += tailData.offsetZ;
			}
			if (tailData.drawConnectingLine)
			{
				float num = Vector3.Distance(previousDrawPos, vector);
				int value = Mathf.CeilToInt(num * 1f);
				value = Mathf.Clamp(value, 1, 20);
				for (int j = 0; j <= value; j++)
				{
					float t = (float)j / (float)value;
					Vector3 pos = Vector3.Lerp(previousDrawPos, vector, t);
					CreateSingleFleck(tailData, pos, map);
				}
			}
			else
			{
				CreateSingleFleck(tailData, vector, map);
			}
		}
	}

	private void CreateSingleFleck(TailData tail, Vector3 pos, Map map)
	{
		if (tail.fleckDef != null)
		{
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(pos, map, tail.fleckDef, tail.scale);
			if (tail.color.HasValue)
			{
				dataStatic.instanceColor = tail.color.Value;
			}
			map.flecks.CreateFleck(dataStatic);
		}
		else
		{
			if (tail.moteDef == null)
			{
				return;
			}
			MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(tail.moteDef);
			if (moteThrown != null)
			{
				moteThrown.Scale = tail.scale;
				moteThrown.exactPosition = pos;
				if (tail.color.HasValue)
				{
					moteThrown.instanceColor = tail.color.Value;
				}
				moteThrown.SetVelocity(0f, 0f);
				GenSpawn.Spawn(moteThrown, pos.ToIntVec3(), map);
			}
		}
	}
}
