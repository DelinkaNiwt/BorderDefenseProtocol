using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompProperties_ApparelReloadable_Custom : CompProperties_ApparelVerbOwnerCharged
{
	public ThingDef ammoDef;

	public int ammoCountToRefill;

	public int ammoCountPerCharge;

	public int baseReloadTicks = 60;

	public bool replenishAfterCooldown;

	public SoundDef soundReload;

	public int gizmoOrder = -99;

	public Color? verbGizmoOverrideColor;

	public string verbGizmoDesc;

	public bool showResourceBar = true;

	public bool maxReloadConfigurable = true;

	public int? initialCharges;

	[MustTranslate]
	public string cooldownGerund = "on cooldown";

	public NamedArgument CooldownVerbArgument => cooldownGerund.CapitalizeFirst().Named("COOLDOWNGERUND");

	public CompProperties_ApparelReloadable_Custom()
	{
		compClass = typeof(CompApparelReloadable_Custom);
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
	{
		foreach (StatDrawEntry item in base.SpecialDisplayStats(req))
		{
			yield return item;
		}
		if (!req.HasThing)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "Stat_Thing_ReloadMaxCharges_Name".Translate(base.ChargeNounArgument), maxCharges.ToString(), "Stat_Thing_ReloadMaxCharges_Desc".Translate(base.ChargeNounArgument), 2749);
		}
	}
}
