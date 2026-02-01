using Verse;

namespace TY_Mora_Gene_B.com;

public class BezierMissileProperties : CompProperties
{
	public float maxTrackingRadius = 30f;

	public float maxTurnAngle = 45f;

	public float turnRatePerTick = 1.2f;

	public bool canSwitchTargets = true;

	public float targetSwitchChance = 0.1f;

	public float minTargetSwitchDistance = 5f;

	public float agilePhaseStart = 0.3f;

	public float agilePhaseEnd = 0.8f;

	public float heightMultiplier = 1f;

	public Projectile_BezierTrackingMissile.MissileTrajectoryType trajectoryType = Projectile_BezierTrackingMissile.MissileTrajectoryType.BezierCurve;

	public float searchRadius = 30f;

	public float trajectoryAmplitude = 1f;

	public float trajectoryFrequency = 1f;

	public float spiralRadius = 3f;

	public float spiralTightness = 0.5f;

	public float tailWidth = 1.2f;

	public float tailLength = 3f;

	public float smokeChance = 0.7f;

	public int flecksPerBurst = 3;

	public int fleckInterval = 2;

	public BezierMissileProperties()
	{
		compClass = typeof(CompBezierMissile);
	}
}
