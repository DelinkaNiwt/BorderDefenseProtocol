using UnityEngine;
using Verse;

namespace ATFieldGenerator;

public class CompProperties_AbsoluteTerrorField : CompProperties
{
	public int energyMax = 500;

	public float energyGainPerTick = 10f;

	public float energyLossPerDamage = 1f;

	public int cooldownTicks = 500;

	public float restartEnergyPct = 0.2f;

	public float radiusDefault = 5f;

	public float radiusMax = 125f;

	public float radiusMin = 5f;

	public string shieldTexturePath;

	public Color shieldColor = new Color(0.5f, 0.8f, 0.9f, 0.5f);

	public Color shieldColorReflect = new Color(1f, 0.3f, 0.3f, 0.4f);

	public float damageScaleBase = 0.5f;

	public float damageScaleFactor = 50f;

	public float damageScaleMax = 10.5f;

	public ThingDef interceptMoteDef;

	public float reflectEnergyCostFactor = 3f;

	public float reflectScatterFactor = 0.25f;

	public float reflectMissRadius = 3f;

	public string iconPathReflect;

	public string iconPathExplosion;

	public string iconPathSkyfall;

	public string iconPathTeleport;

	public bool suppressExplosions = true;

	public bool redirectSkyfallers = true;

	public bool blockSolarFlare = true;

	public bool antiTeleport = true;

	public float maxDamagePerHit = -1f;

	public CompProperties_AbsoluteTerrorField()
	{
		compClass = typeof(Comp_AbsoluteTerrorField);
	}
}
