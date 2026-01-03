using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test;

public class CMC_Projectile_EMPshell : Projectile
{
	public override bool AnimalsFleeImpact => true;

	private Vector3 CurretPos(float t)
	{
		return origin + (destination - origin) * t;
	}

	protected override void DrawAt(Vector3 position, bool flip = false)
	{
		Vector3 vector = CurretPos(base.DistanceCoveredFraction - 0.01f);
		position = CurretPos(base.DistanceCoveredFraction);
		Quaternion rotation = Quaternion.LookRotation(position - vector);
		if (base.DistanceCoveredFraction > 0.04f)
		{
			Vector3 position2 = position;
			position2.y = AltitudeLayer.Projectile.AltitudeFor();
			Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), position2, rotation, DrawMat, 0);
			Comps_PostDraw();
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
		CellRect cellRect = CellRect.CenteredOn(base.Position, 3);
		cellRect.ClipInsideMap(map);
		for (int i = 0; i < 2; i++)
		{
			DomultiEMPExplosion(cellRect.RandomCell, map, 1.1f);
		}
	}

	protected override void Tick()
	{
		if (landed)
		{
			return;
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

	protected void DomultiEMPExplosion(IntVec3 pos, Map map, float radius)
	{
		GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.EMP, launcher, 30, ArmorPenetration, null, equipmentDef, def, intendedTarget.Thing);
	}
}
