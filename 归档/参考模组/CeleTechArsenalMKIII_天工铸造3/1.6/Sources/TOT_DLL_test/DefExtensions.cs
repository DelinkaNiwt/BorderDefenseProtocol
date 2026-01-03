using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class DefExtensions : DefModExtension
{
	public static List<ThingDef> ProjectileDefs = new List<ThingDef>();

	public override void ResolveReferences(Def parentDef)
	{
		ProjectileDefs = DefDatabase<ThingDef>.AllDefsListForReading.Where((ThingDef x) => x.projectile != null && (x.projectile.flyOverhead || x.projectile.explosionRadius > 0f)).ToList();
		Log.Message(">>>CMC projectile ref resolved");
	}
}
