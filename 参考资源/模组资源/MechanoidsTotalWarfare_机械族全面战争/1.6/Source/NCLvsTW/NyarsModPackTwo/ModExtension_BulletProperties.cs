using System.Collections.Generic;
using Verse;

namespace NyarsModPackTwo;

public class ModExtension_BulletProperties : DefModExtension
{
	public bool trackEnemyProjectiles = false;

	public bool ignoreGroundProjectiles = true;

	public bool ignoreAirProjectiles = false;

	public bool enableExplosionOnHit = false;

	public float explosionRadius = 4.2f;

	public int explosionDamage = 35;

	public FleckDef trailFleck;

	public FleckDef impactFleck;

	public int accelerationDuration = 120;

	public float initialFlyingStep = 0.4f;

	public int ticksBeforeTracing = 30;

	public int maxFlyingTime = 900;

	public float rotatingStep = 6f;

	public float flyingStep = 0.4f;

	public int ticksBetweenFindTarget = 30;

	public List<string> randomImpactFlecks = new List<string>();

	public float impactScale = 1f;

	public string secondaryImpactFleck = "NCL_Fleck_BurnerUsedEmber";

	public float secondaryImpactScale = 1f;

	public string tertiaryImpactFleck = "NCL_Fleck_BurnerUsedEmber";

	public float tertiaryImpactScale = 1f;

	public bool enableSmokeEffect = true;

	public float smokeSize = 1f;

	public bool enableFireGlowEffect = true;

	public float fireGlowSize = 1f;

	public SoundDef interceptSound;

	public float projectileTrackingMultiplier = 1f;
}
