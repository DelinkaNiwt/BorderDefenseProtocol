using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Milira;

public class CompProperties_MilianApparelRender : CompProperties
{
	public CompProperties_MilianApparelRender()
	{
		compClass = typeof(CompMilianApparelRender);
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
	{
		List<ThingDef> list = new List<ThingDef>();
		List<ThingDef> sourceDefs = DefDatabase<ThingDef>.AllDefsListForReading.ToList();
		for (int i = 0; i < sourceDefs.Count; i++)
		{
			ThingDef def = sourceDefs[i];
			int num;
			if (def?.HasComp<CompDressMilian>() ?? false)
			{
				ApparelProperties apparel = def.apparel;
				num = ((apparel != null && apparel.tags?.Contains(req.Def.defName) == true) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			if (num != 0)
			{
				list.Add(def);
			}
		}
		yield return new StatDrawEntry(StatCategoryDefOf.Mechanoid, "Milian.SupportApparel".Translate(), "", "Milian.SupportApparelDesc".Translate(), 0, null, Dialog_InfoCard.DefsToHyperlinks(list));
	}
}
