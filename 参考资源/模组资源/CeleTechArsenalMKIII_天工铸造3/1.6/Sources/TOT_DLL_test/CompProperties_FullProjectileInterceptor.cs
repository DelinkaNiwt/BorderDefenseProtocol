using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CompProperties_FullProjectileInterceptor : CompProperties
{
	public bool interceptGroundProjectiles = true;

	public bool interceptAirProjectiles = true;

	public int startingTicksToReset = 600;

	public int cooldownTicks = 500;

	public bool interceptNonHostileProjectiles = false;

	public bool drawWithNoSelection = true;

	public int chargeIntervalTicks;

	public int chargeDurationTicks;

	public float minIdleAlpha = 0f;

	public int hitPoints = -1;

	public int rechargeHitPointsIntervalTicks = 240;

	public string gizmoTipKey;

	public bool hitPointsRestoreInstantlyAfterCharge = false;

	public Color color = Color.white;

	public EffecterDef reactivateEffect;

	public EffecterDef interceptEffect;

	public SoundDef activeSound;

	public CompProperties_FullProjectileInterceptor()
	{
		compClass = typeof(CompFullProjectileInterceptor);
	}
}
