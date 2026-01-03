using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_TurretGun_Custom : CompProperties
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

	public CompProperties_TurretGun_Custom()
	{
		compClass = typeof(CompTurretGun_Custom);
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
	{
		string str = statLabelPostfix;
		yield return new StatDrawEntry(StatCategoryDefOf.PawnCombat, "Ancot.Turret".Translate() + str, turretDef.LabelCap, "Ancot.TurretDesc".Translate(), 5600, null, Gen.YieldSingle(new Dialog_InfoCard.Hyperlink(turretDef)));
	}

	public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
	{
		if (renderNodeProperties.NullOrEmpty())
		{
			yield break;
		}
		foreach (PawnRenderNodeProperties renderNodeProperty in renderNodeProperties)
		{
			if (!typeof(PawnRenderNode_TurretGun_Custom).IsAssignableFrom(renderNodeProperty.nodeClass))
			{
				yield return "contains nodeClass which is not PawnRenderNode_FloatTurret or subclass thereof.";
			}
		}
	}
}
