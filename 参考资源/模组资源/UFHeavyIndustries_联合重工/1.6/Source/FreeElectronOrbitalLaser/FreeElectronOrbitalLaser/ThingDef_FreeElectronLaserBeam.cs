using Verse;

namespace FreeElectronOrbitalLaser;

public class ThingDef_FreeElectronLaserBeam : ThingDef
{
	public DamageDef damageDef;

	public float armorPenetration = 0f;

	public float effectRadius = 15f;

	public int strikesPerTick = 4;

	public float haloScaleInner = 15f;

	public float haloScaleOuter = 25f;

	public IntRange flameDamageAmountRange = new IntRange(50, 100);

	public IntRange corpseFlameDamageAmountRange = new IntRange(5, 10);
}
