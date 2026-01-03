using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompProperties_TurretGun_Building : CompProperties
{
	public ThingDef turretDef;

	public float angleOffset = 0f;

	public float turretDrawScale = 1f;

	public Vector2 turretDrawOffset = new Vector2(0f, 0f);

	public bool autoAttack = true;

	public float consumeChargeAmountPerShot = 0f;

	public string saveKeysPrefix;

	public string statLabelPostfix;

	public string gizmoLable;

	public string gizmoDesc;

	public string gizmoIconPath;

	public CompProperties_TurretGun_Building()
	{
		compClass = typeof(CompTurretGun_Building);
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
	{
		string str = statLabelPostfix;
		yield return new StatDrawEntry(StatCategoryDefOf.PawnCombat, "Ancot.Turret".Translate() + str, turretDef.LabelCap, "Ancot.TurretDesc".Translate(), 5600, null, Gen.YieldSingle(new Dialog_InfoCard.Hyperlink(turretDef)));
	}
}
