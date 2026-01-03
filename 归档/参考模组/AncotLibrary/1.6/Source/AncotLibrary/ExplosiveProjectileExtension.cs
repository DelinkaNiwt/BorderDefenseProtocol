using UnityEngine;
using Verse;

namespace AncotLibrary;

public class ExplosiveProjectileExtension : DefModExtension
{
	public FleckDef trailFleck;

	public int trailFreauency = 1;

	public Color trailColor = new Color(1f, 1f, 1f);

	public bool doTrail = false;

	public bool doExtraExplosion = false;

	public DamageDef extraExplosionDamageType;

	public int extraExplosionDamageAmount;

	public float extraExplosionArmorPenetration;

	public int extraExplosionCount = 1;

	public float extraExplosionRadius;

	public int extraExplosionRandomPositionRadius = 0;
}
