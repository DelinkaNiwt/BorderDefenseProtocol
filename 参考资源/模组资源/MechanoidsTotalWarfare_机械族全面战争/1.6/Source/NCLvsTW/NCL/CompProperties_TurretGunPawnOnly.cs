using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_TurretGunPawnOnly : CompProperties_TurretGun
{
	public CompProperties_TurretGunPawnOnly()
	{
		compClass = typeof(CompTurretGunPawnOnly);
	}

	public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
	{
		foreach (string item in base.ConfigErrors(parentDef))
		{
			yield return item;
		}
		if (turretDef == null)
		{
			yield return "turretDef must be defined";
		}
	}
}
