using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Projectile_PlasmaShell : Projectile_Explosive
{
	public IntRange Fleck_MakeFleckNum = new IntRange(2, 2);

	public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);

	public FloatRange Fleck_Scale = new FloatRange(1.6f, 1.7f);

	public FloatRange Fleck_Speed = new FloatRange(5f, 7f);

	public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);

	public int Fleck_MakeFleckTick;

	public Vector3 lastposition;

	private Vector3 CurretPos(float t)
	{
		return origin + (destination - origin) * t;
	}

	public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
	{
		lastposition = origin;
		base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
	}

	protected override void Tick()
	{
		Fleck_MakeFleckTick = 0;
		Map map = base.Map;
		Vector3 loc = CurretPos(base.DistanceCoveredFraction);
		ThrowTailGlow(loc, map, 0.24f);
		ThrowTailGlow(loc, map, 0.33f);
		ThrowTailGlow(loc, map, 0.31f);
		lastposition = loc;
		base.Tick();
	}

	public static void ThrowTailGlow(Vector3 loc, Map map, float size)
	{
		if (loc.ShouldSpawnMotesAt(map))
		{
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + size * new Vector3((Rand.Value - 0.5f) * 0.23f, 0f, (Rand.Value - 0.5f) * 0.23f), map, FleckDefOf.LightningGlow, Rand.Range(4f, 6f) * size);
			dataStatic.rotationRate = Rand.Range(-3f, 3f);
			dataStatic.velocityAngle = Rand.Range(0, 360);
			dataStatic.velocitySpeed = 1f;
			map.flecks.CreateFleck(dataStatic);
		}
	}
}
