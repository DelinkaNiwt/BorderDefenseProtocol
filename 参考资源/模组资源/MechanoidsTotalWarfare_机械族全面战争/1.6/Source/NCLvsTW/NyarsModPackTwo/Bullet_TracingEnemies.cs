using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NyarsModPackTwo;

public class Bullet_TracingEnemies : Bullet
{
	private enum TargetType
	{
		None,
		Projectile,
		Pawn
	}

	private static readonly Dictionary<int, Bullet_TracingEnemies> LockedProjectiles = new Dictionary<int, Bullet_TracingEnemies>();

	private static readonly MethodInfo _interceptCheck = typeof(Projectile).GetMethod("CheckForFreeInterceptBetween", BindingFlags.Instance | BindingFlags.NonPublic);

	private readonly object[] _interceptParams = new object[2];

	private readonly List<Thing> _localTargetCache = new List<Thing>();

	private ModExtension_BulletProperties _props;

	private int _flyingTime;

	public Thing trackingTargetThing;

	public IntVec3 trackingCell = IntVec3.Invalid;

	public float flyingAngle;

	public Vector3 trackingPosNow;

	private TargetType currentTargetType = TargetType.None;

	public override Vector3 ExactPosition => trackingPosNow + Vector3.up * def.Altitude;

	public override Quaternion ExactRotation => Quaternion.AngleAxis(flyingAngle, Vector3.up);

	private float TargetAngle => (trackingCell.ToVector3() - trackingPosNow).AngleFlat();

	private ModExtension_BulletProperties Props => _props ?? (_props = def.GetModExtension<ModExtension_BulletProperties>());

	private bool IsHostileProjectile(Thing projectile)
	{
		if (!(projectile is Projectile { Launcher: var launcher }))
		{
			return false;
		}
		return launcher != null && launcher.Faction != null && base.launcher != null && base.launcher.Faction != null && launcher.Faction.HostileTo(base.launcher.Faction);
	}

	protected override void Tick()
	{
		float currentFlyingStep;
		if (_flyingTime < Props.ticksBeforeTracing)
		{
			currentFlyingStep = Props.initialFlyingStep;
		}
		else if (_flyingTime < Props.ticksBeforeTracing + Props.accelerationDuration)
		{
			float progress = (float)(_flyingTime - Props.ticksBeforeTracing) / (float)Props.accelerationDuration;
			currentFlyingStep = Props.initialFlyingStep + progress * (Props.flyingStep - Props.initialFlyingStep);
		}
		else
		{
			currentFlyingStep = Props.flyingStep;
		}
		if (landed)
		{
			return;
		}
		if (_flyingTime >= Props.maxFlyingTime)
		{
			Impact(null);
			return;
		}
		if (_flyingTime >= Props.ticksBeforeTracing && (_flyingTime - Props.ticksBeforeTracing) % Props.ticksBetweenFindTarget == 0)
		{
			UpdateTarget();
		}
		UpdateTargetCell();
		_flyingTime++;
		Vector3 vector = trackingCell.ToVector3();
		float num = vector.x - trackingPosNow.x;
		float num2 = vector.z - trackingPosNow.z;
		float num3 = num * num + num2 * num2;
		if (num3 <= currentFlyingStep * currentFlyingStep * 3f)
		{
			ticksToImpact = 0;
			base.Position = trackingCell;
			if (trackingTargetThing != null && trackingTargetThing.Spawned)
			{
				if (currentTargetType == TargetType.Projectile)
				{
					InterceptProjectile(trackingTargetThing as Projectile);
				}
				else
				{
					Impact(trackingTargetThing);
				}
			}
			else
			{
				Impact(null);
			}
			return;
		}
		Vector3 exactPosition = ExactPosition;
		trackingPosNow += new Vector3((float)Math.Sin(flyingAngle / 180f * 3.14159f), 0f, (float)Math.Cos(flyingAngle / 180f * 3.14159f)) * currentFlyingStep;
		if (!trackingPosNow.InBounds(base.Map))
		{
			ticksToImpact = 0;
			Destroy();
			return;
		}
		Vector3 exactPosition2 = ExactPosition;
		if (Props.trailFleck != null)
		{
			FleckMaker.ConnectingLine(exactPosition, exactPosition2, Props.trailFleck, base.Map, 0.1f);
		}
		if (!(bool)_interceptCheck.Invoke(this, _interceptParams))
		{
			base.Position = trackingPosNow.ToIntVec3();
			Rotate();
		}
	}

	private void InterceptProjectile(Projectile projectile)
	{
		if (projectile == null || projectile.Destroyed)
		{
			return;
		}
		Vector3 targetPos = projectile.DrawPos;
		bool isSpecialBullet = projectile.def.defName == "Bullet_HellsphereCannonGun";
		if (Props.randomImpactFlecks != null && Props.randomImpactFlecks.Count > 0 && !isSpecialBullet)
		{
			string chosenFleckName = Props.randomImpactFlecks.RandomElement();
			FleckDef impactFleckDef = DefDatabase<FleckDef>.GetNamedSilentFail(chosenFleckName);
			if (impactFleckDef != null)
			{
				FleckMaker.Static(targetPos, base.Map, impactFleckDef, Props.impactScale);
			}
		}
		if (!string.IsNullOrEmpty(Props.secondaryImpactFleck) && !isSpecialBullet)
		{
			FleckDef secondaryFleckDef = DefDatabase<FleckDef>.GetNamedSilentFail(Props.secondaryImpactFleck);
			if (secondaryFleckDef != null)
			{
				float offsetDistance = Props.secondaryImpactScale * 0.8f;
				Vector3 offsetPos = GetOffsetPosition(targetPos, offsetDistance);
				FleckMaker.Static(offsetPos, base.Map, secondaryFleckDef, Props.secondaryImpactScale);
			}
		}
		if (!string.IsNullOrEmpty(Props.tertiaryImpactFleck) && !isSpecialBullet)
		{
			FleckDef tertiaryFleckDef = DefDatabase<FleckDef>.GetNamedSilentFail(Props.tertiaryImpactFleck);
			if (tertiaryFleckDef != null)
			{
				float offsetDistance2 = Props.tertiaryImpactScale * 0.8f;
				Vector3 offsetPos2 = GetOffsetPosition(targetPos, offsetDistance2);
				FleckMaker.Static(offsetPos2, base.Map, tertiaryFleckDef, Props.tertiaryImpactScale);
			}
		}
		if (Props.enableSmokeEffect && !isSpecialBullet)
		{
			float offsetDistance3 = Props.smokeSize * 0.5f;
			Vector3 offsetPos3 = GetOffsetPosition(targetPos, offsetDistance3);
			FleckMaker.ThrowSmoke(offsetPos3, base.Map, Props.smokeSize);
		}
		if (Props.enableFireGlowEffect && !isSpecialBullet)
		{
			float offsetDistance4 = Props.fireGlowSize * 0.5f;
			Vector3 offsetPos4 = GetOffsetPosition(targetPos, offsetDistance4);
			FleckMaker.ThrowFireGlow(offsetPos4, base.Map, Props.fireGlowSize);
		}
		if (Props.interceptSound != null)
		{
			Props.interceptSound.PlayOneShot(new TargetInfo(projectile.Position, base.Map));
		}
		if (isSpecialBullet)
		{
			TriggerBulletExplosion(projectile);
		}
		else
		{
			projectile.Destroy();
		}
		Destroy();
	}

	private Vector3 GetOffsetPosition(Vector3 center, float maxOffset)
	{
		float angle = Rand.Range(0f, 360f);
		float distance = Rand.Range(0f, maxOffset);
		float xOffset = Mathf.Sin(angle * ((float)Math.PI / 180f)) * distance;
		float zOffset = Mathf.Cos(angle * ((float)Math.PI / 180f)) * distance;
		return new Vector3(center.x + xOffset, center.y, center.z + zOffset);
	}

	private void TriggerBulletExplosion(Projectile projectile)
	{
		try
		{
			if (projectile != null && !projectile.Destroyed && projectile.Spawned)
			{
				MethodInfo impactMethod = typeof(Projectile).GetMethod("Impact", BindingFlags.Instance | BindingFlags.NonPublic);
				if (impactMethod != null)
				{
					impactMethod.Invoke(projectile, new object[2] { null, false });
				}
				else
				{
					projectile.Destroy();
				}
				if (!projectile.Destroyed)
				{
					projectile.Destroy();
				}
			}
		}
		catch (Exception arg)
		{
			Log.Error($"处理特殊子弹爆炸时出错: {arg}");
		}
	}

	private void CleanupReferences()
	{
		if (trackingTargetThing != null && currentTargetType == TargetType.Projectile)
		{
			ReleaseProjectileLock(trackingTargetThing.thingIDNumber);
		}
		trackingTargetThing = null;
		trackingCell = IntVec3.Invalid;
		currentTargetType = TargetType.None;
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		CleanupReferences();
		Vector3 hitPos = ExactPosition;
		if (Props.impactFleck != null)
		{
			FleckCreationData fleckData = new FleckCreationData
			{
				def = Props.impactFleck,
				spawnPosition = hitPos,
				scale = 1f,
				rotation = Rand.Range(0, 360),
				ageTicksOverride = -1
			};
			base.Map.flecks.CreateFleck(fleckData);
		}
		if (hitThing != null && !blockedByShield && hitThing is Pawn hitPawn && launcher != null && launcher.Faction != null && hitPawn.Faction != null && hitPawn.Faction.HostileTo(launcher.Faction) && Props.enableExplosionOnHit)
		{
			Vector3 explosionPos = hitPawn.DrawPos;
			GenExplosion.DoExplosion(explosionPos.ToIntVec3(), base.Map, 4.2f, DamageDefOf.Bomb, launcher, 35, 1f);
		}
		base.Impact(hitThing, blockedByShield);
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		CleanupReferences();
		base.Destroy(mode);
	}

	private bool IsEnemy(Pawn pawn)
	{
		return launcher != null && launcher.HostileTo(pawn) && !pawn.Downed;
	}

	private void UpdateTarget()
	{
		trackingTargetThing = null;
		currentTargetType = TargetType.None;
		if (currentTargetType == TargetType.Projectile && trackingTargetThing != null)
		{
			ReleaseProjectileLock(trackingTargetThing.thingIDNumber);
		}
		if (Props.trackEnemyProjectiles)
		{
			List<Thing> projectiles = base.Map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile);
			List<Thing> availableProjectiles = new List<Thing>();
			foreach (Thing proj in projectiles)
			{
				if (proj == null || !proj.Spawned || proj.Map != base.Map || !IsHostileProjectile(proj))
				{
					continue;
				}
				bool isAirProjectile = proj.def.projectile.flyOverhead;
				if ((!isAirProjectile || !Props.ignoreAirProjectiles) && (isAirProjectile || !Props.ignoreGroundProjectiles))
				{
					int projectileId = proj.thingIDNumber;
					if (!LockedProjectiles.ContainsKey(projectileId) || LockedProjectiles[projectileId] == this)
					{
						availableProjectiles.Add(proj);
					}
				}
			}
			if (availableProjectiles.Count > 0)
			{
				Thing closestProjectile = availableProjectiles.OrderBy((Thing p) => (p.Position - base.Position).LengthHorizontalSquared).FirstOrDefault();
				if (closestProjectile != null)
				{
					trackingTargetThing = closestProjectile;
					currentTargetType = TargetType.Projectile;
					LockProjectile(closestProjectile.thingIDNumber);
					return;
				}
			}
		}
		_localTargetCache.Clear();
		foreach (Pawn pawn in base.Map.mapPawns.AllPawnsSpawned)
		{
			if (pawn != null && pawn.Spawned && pawn.Map == base.Map && IsEnemy(pawn))
			{
				_localTargetCache.Add(pawn);
			}
		}
		if (_localTargetCache.Count > 0)
		{
			_localTargetCache.Sort((Thing a, Thing b) => (a.Position - base.Position).LengthHorizontalSquared.CompareTo((b.Position - base.Position).LengthHorizontalSquared));
			trackingTargetThing = _localTargetCache[0];
			currentTargetType = TargetType.Pawn;
		}
	}

	private void LockProjectile(int projectileId)
	{
		if (LockedProjectiles.ContainsKey(projectileId))
		{
			LockedProjectiles[projectileId] = this;
		}
		else
		{
			LockedProjectiles.Add(projectileId, this);
		}
	}

	private void ReleaseProjectileLock(int projectileId)
	{
		if (LockedProjectiles.ContainsKey(projectileId) && LockedProjectiles[projectileId] == this)
		{
			LockedProjectiles.Remove(projectileId);
		}
	}

	private void UpdateTargetCell()
	{
		if (trackingTargetThing == null || trackingTargetThing.Destroyed || !trackingTargetThing.Spawned || trackingTargetThing.Map != base.Map)
		{
			trackingCell = IntVec3.Invalid;
			return;
		}
		float precisionFactor = ((currentTargetType == TargetType.Projectile) ? 0.8f : 0.6f);
		Vector3 predictedPos = trackingTargetThing.DrawPos;
		if (trackingTargetThing is Pawn pawn)
		{
			if (pawn.pather != null && pawn.pather.Moving)
			{
				Vector3 moveDir = (pawn.pather.Destination.Cell.ToVector3() - pawn.DrawPos).normalized;
				predictedPos += moveDir * Props.flyingStep * precisionFactor;
			}
		}
		else if (trackingTargetThing is Projectile { def: not null } projectile && projectile.def.projectile != null)
		{
			Vector3 projDir = projectile.ExactRotation * Vector3.forward;
			predictedPos += projDir * projectile.def.projectile.SpeedTilesPerTick * 2f;
		}
		trackingCell = predictedPos.ToIntVec3();
	}

	private void Rotate()
	{
		float rotationMultiplier = ((currentTargetType == TargetType.Projectile) ? 1.5f : 1f);
		if (!(trackingCell == IntVec3.Invalid))
		{
			float targetAngle = TargetAngle;
			float num = flyingAngle - targetAngle;
			float num2 = Props.rotatingStep * rotationMultiplier * ((_flyingTime < 60 + Props.ticksBeforeTracing) ? 1f : ((float)(_flyingTime - 60 - Props.ticksBeforeTracing) / 15f + 1f));
			if (num > 180f)
			{
				num -= 360f;
			}
			if (num < -180f)
			{
				num += 360f;
			}
			if (num > num2)
			{
				flyingAngle -= num2;
			}
			else if (num < 0f - num2)
			{
				flyingAngle += num2;
			}
			else
			{
				flyingAngle = targetAngle;
			}
			flyingAngle %= 360f;
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref _flyingTime, "_flyingTime", 0);
		Scribe_References.Look(ref trackingTargetThing, "trackingTargetThing");
		Scribe_Values.Look(ref trackingCell, "trackingCell");
		Scribe_Values.Look(ref flyingAngle, "flyingAngle", 0f);
		Scribe_Values.Look(ref trackingPosNow, "trackingPosNow");
		Scribe_Values.Look(ref currentTargetType, "currentTargetType", TargetType.None);
	}
}
