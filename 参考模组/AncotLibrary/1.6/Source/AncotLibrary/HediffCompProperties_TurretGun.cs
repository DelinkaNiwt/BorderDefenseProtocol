using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class HediffCompProperties_TurretGun : HediffCompProperties
{
	public ThingDef turretDef;

	public float angleOffset;

	public bool autoAttack = true;

	public bool attackUndrafted = true;

	public int consumeChargeAmountPerShot = 0;

	public FloatGraph float_yAxis;

	public FloatGraph float_xAxis;

	public List<PawnRenderNodeProperties> renderNodeProperties;

	public string saveKeysPrefix;

	public string statLabelPostfix;

	public string gizmoLable;

	public string gizmoDesc;

	public string gizmoIconPath;

	public HediffCompProperties_TurretGun()
	{
		compClass = typeof(HediffComp_TurretGun);
	}
}
