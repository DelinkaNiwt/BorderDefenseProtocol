using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompProperties_PhysicalShield : CompProperties
{
	public Color shieldBarColor = new Color(0.75f, 0.75f, 0.75f);

	public HediffDef holdShieldHediff;

	public List<Tool> tools;

	public string graphicPath_Holding;

	public string graphicPath_Ready;

	public string graphicPath_Disabled;

	public int gizmoOrder = -99;

	public string barGizmoLabel;

	public string gizmoLabel = "Hold shield";

	public string gizmoDesc = "hold shield if possible.";

	public string gizmoIconPath = "AncotLibrary/Gizmos/SwitchShield";

	public bool blocksRangedWeapons = true;

	public bool alwaysHoldShield = false;

	public bool recordLastHarmTickWhenBlocked = true;

	public bool affectedByStuff = true;

	public EffecterDef blockEffecter;

	public EffecterDef breakEffecter;

	public An_ShieldSize size = An_ShieldSize.Small;

	public CompProperties_PhysicalShield()
	{
		compClass = typeof(CompPhysicalShield);
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
	{
		yield return new StatDrawEntry(AncotDefOf.Ancot_Shield, "Ancot.BlocksRangedWeapons".Translate(), blocksRangedWeapons ? "Ancot.True".Translate() : "Ancot.False".Translate(), "Ancot.BlocksRangedWeaponsDesc".Translate(), 900);
		yield return new StatDrawEntry(AncotDefOf.Ancot_Shield, "Ancot.ShieldSize".Translate(), $"Ancot.ShieldSize_{size}".Translate(), "Ancot.ShieldSizeDesc".Translate(), 1100);
	}
}
