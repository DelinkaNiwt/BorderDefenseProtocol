using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class CMC_Projectile_ZRPlasmaPulse : Projectile_PlasmaShell
{
	public override bool AnimalsFleeImpact => true;

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Map map = base.Map;
		base.Impact(hitThing, blockedByShield);
		IntVec3 position = base.Position;
		Map map2 = map;
		double num = def.projectile.explosionRadius;
		DamageDef bomb = DamageDefOf.Bomb;
		Thing instigator = launcher;
		int damageAmount = DamageAmount;
		double num2 = ArmorPenetration;
		ThingDef weapon = equipmentDef;
		ThingDef projectile = def;
		Thing thing = intendedTarget.Thing;
		GasType? postExplosionGasType = null;
		float? num3 = null;
		FloatRange? floatRange = null;
		float radius = (float)num;
		float armorPenetration = (float)num2;
		float? direction = num3;
		FloatRange? affectedAngle = floatRange;
		GenExplosion.DoExplosion(position, map2, radius, bomb, instigator, damageAmount, armorPenetration, null, weapon, projectile, thing, null, 0f, 1, postExplosionGasType, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, direction, null, affectedAngle);
		CellRect cellRect = CellRect.CenteredOn(base.Position, 5);
		cellRect.ClipInsideMap(map);
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 2; j++)
			{
				DomultiEMPExplosion(cellRect.RandomCell, map, 1f);
			}
			DomultiFlameExplosion(cellRect.RandomCell, map, 1f);
			DomultiBombExplosion(cellRect.RandomCell, map, 1.5f);
		}
	}

	protected void DomultiBombExplosion(IntVec3 pos, Map map, float radius)
	{
		GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.Bomb, launcher, DamageAmount, ArmorPenetration, null, equipmentDef, def, intendedTarget.Thing);
	}

	protected void DomultiFlameExplosion(IntVec3 pos, Map map, float radius)
	{
		GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.Flame, launcher, DamageAmount, ArmorPenetration, null, equipmentDef, def, intendedTarget.Thing);
	}

	protected void DomultiEMPExplosion(IntVec3 pos, Map map, float radius)
	{
		GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.EMP, launcher, DamageAmount, ArmorPenetration, null, equipmentDef, def, intendedTarget.Thing);
	}
}
