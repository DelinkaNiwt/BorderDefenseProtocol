using Verse;

namespace ECT;

public class ModExtension_HeavyArmourPiercer : DefModExtension
{
	public float hitWidthRadius = 1.5f;

	public SoundDef skimHitSound;

	public FleckDef sparkFleckDef;

	public IntRange sparkCountRange = new IntRange(5, 10);

	public FloatRange sparkSpeedRange = new FloatRange(10f, 20f);

	public float sparkSpreadAngle = 15f;

	public float knockbackDistance = 3f;

	public int stunDuration = 120;

	public int maxHitsPerTarget = 1;

	public int maxWallPenetrations = 2;

	public float lightningLength = 20f;

	public int lightningDuration = 20;

	public int lightningGrowthTicks = 5;

	public float lightningVariance = 1f;

	public float lightningWidth = 2.5f;

	public float maxHomingAngle = 45f;
}
