using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class Verb_Shoot_Fortress : Verb_Shoot
{
	private CompThingContainer_Milian CompThingContainer_Milian => caster.TryGetComp<CompThingContainer_Milian>();

	private CompThingCarrier_Custom Carrier => CompThingContainer_Milian.ContainedThing.TryGetComp<CompThingCarrier_Custom>();

	public override bool Available()
	{
		if (!base.Available())
		{
			return false;
		}
		return Carrier != null && Carrier.IngredientCount > 20;
	}

	protected override bool TryCastShot()
	{
		bool flag = base.TryCastShot();
		Vector3 normalized = (currentTarget.CenterVector3 - caster.Position.ToVector3()).normalized;
		Find.CameraDriver.shaker.DoShake(2f);
		Building_TurretGunFortress building_TurretGunFortress = caster as Building_TurretGunFortress;
		float curRotation = ((Building_SpinTurretGun)building_TurretGunFortress).CurRotation;
		for (int i = 0; i < 20; i++)
		{
			AncotFleckMaker.CustomFleckThrow(((Thing)(object)building_TurretGunFortress).Map, FleckDefOf.AirPuff, ((Thing)(object)building_TurretGunFortress).Position.ToVector3Shifted(), new Color(0.92f, 0.91f, 0.76f), normalized + new Vector3(Rand.Range(-0.05f, 0.05f), 0f, Rand.Range(-0.05f, 0.05f)), Rand.Range(1f, 4f), 0f, curRotation + 180f + Rand.Range(-30f, 30f), Rand.Range(4f, 12f), 0f);
		}
		for (int j = 0; j < 8; j++)
		{
			AncotFleckMaker.CustomFleckThrow(((Thing)(object)building_TurretGunFortress).Map, RimWorld.FleckDefOf.MicroSparksFast, ((Thing)(object)building_TurretGunFortress).Position.ToVector3Shifted(), new Color(0.92f, 0.91f, 0.76f), normalized, Rand.Range(1f, 4f), 0f, curRotation + Rand.Range(-10f, 10f), Rand.Range(1f, 3f), 0f);
		}
		if (flag && CompThingContainer_Milian != null)
		{
			CompThingContainer_Milian.staySec = 20;
		}
		Carrier.TryRemoveThingInCarrier(20);
		return flag;
	}
}
