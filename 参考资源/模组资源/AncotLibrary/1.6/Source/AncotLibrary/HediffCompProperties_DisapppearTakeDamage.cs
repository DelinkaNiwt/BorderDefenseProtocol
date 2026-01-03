using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class HediffCompProperties_DisapppearTakeDamage : HediffCompProperties
{
	public List<DamageDef> damageDefs;

	public EffecterDef disapppearEffecter;

	public HediffCompProperties_DisapppearTakeDamage()
	{
		compClass = typeof(HediffComp_DisapppearTakeDamage);
	}
}
