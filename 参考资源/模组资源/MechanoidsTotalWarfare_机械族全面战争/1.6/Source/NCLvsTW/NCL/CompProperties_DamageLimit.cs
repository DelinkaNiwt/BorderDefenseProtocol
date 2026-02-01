using System.Collections.Generic;
using Verse;

namespace NCL;

public class CompProperties_DamageLimit : CompProperties
{
	public float maxDamage = 200f;

	public List<DamageDef> excludedDamageTypes;

	public CompProperties_DamageLimit()
	{
		compClass = typeof(CompDamageLimit);
	}
}
