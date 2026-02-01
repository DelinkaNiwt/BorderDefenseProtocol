using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NyarsModPackTwo;

public class CompProperties_ShootRandomBullet : CompProperties_AbilityEffect
{
	public List<ThingDef> bullets;

	public IntRange castCount = new IntRange(1, 1);

	public CompProperties_ShootRandomBullet()
	{
		compClass = typeof(CompAbilityEffect_ShootRandomBullet);
	}
}
