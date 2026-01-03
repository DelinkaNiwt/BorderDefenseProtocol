using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test;

public class Projectile_GigaEMPShell : CMC_Projectile_EMPshell
{
	private int tickcount = 0;

	protected override void Tick()
	{
		tickcount++;
		if (landed)
		{
			return;
		}
		if (tickcount < 4)
		{
			FleckMaker.ThrowFireGlow(DrawPos, base.Map, 1.5f);
			FleckMaker.ThrowMicroSparks(DrawPos, base.Map);
			FleckMaker.ThrowDustPuff(base.Position, base.Map, 2f);
		}
		Vector3 exactPosition = ExactPosition;
		ticksToImpact--;
		if (!ExactPosition.InBounds(base.Map))
		{
			ticksToImpact++;
			base.Position = ExactPosition.ToIntVec3();
			Destroy();
			return;
		}
		Vector3 exactPosition2 = ExactPosition;
		base.Position = ExactPosition.ToIntVec3();
		if (ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && def.projectile.soundImpactAnticipate != null)
		{
			def.projectile.soundImpactAnticipate.PlayOneShot(this);
		}
		if (ticksToImpact <= 0)
		{
			if (base.DestinationCell.InBounds(base.Map))
			{
				base.Position = base.DestinationCell;
			}
			ImpactSomething();
		}
	}

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
		CellRect cellRect = CellRect.CenteredOn(base.Position, 6);
		cellRect.ClipInsideMap(map);
		for (int i = 0; i < 2; i++)
		{
			DomultiEMPExplosion(cellRect.RandomCell, map, 4.3f);
		}
	}
}
