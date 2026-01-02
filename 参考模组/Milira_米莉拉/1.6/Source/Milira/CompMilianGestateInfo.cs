using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Milira;

public class CompMilianGestateInfo : ThingComp
{
	private CompProperties_MilianGestateInfo Props => (CompProperties_MilianGestateInfo)props;

	public Building_MechGestator Gestator => parent as Building_MechGestator;

	public Pawn Milian => Gestator.GestatingMech;

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		ThingDefCountClass t = Gestator?.ActiveMechBill?.recipe?.products?.FirstOrDefault();
		Command_Action command_Action = new Command_Action
		{
			defaultLabel = "Milian.CheckMilianApparelInGestator".Translate(),
			defaultDesc = "Milian.CheckMilianApparelInGestatorDesc".Translate(),
			icon = MiliraIcon.MilianCheckApparel,
			action = delegate
			{
				Find.WindowStack.Add(new Dialog_NodeTree(CheckMilianApparel(t.thingDef)));
			}
		};
		if (t == null)
		{
			command_Action.Disable("Milian.CheckMilianApparelInGestator_Disabled".Translate());
		}
		yield return command_Action;
	}

	public static DiaNode CheckMilianApparel(ThingDef milianDef)
	{
		TaggedString text = "Milian.CheckMilianApparelInGestatorText".Translate(milianDef.LabelCap);
		List<ThingDef> list = new List<ThingDef>();
		List<ThingDef> list2 = DefDatabase<ThingDef>.AllDefsListForReading.ToList();
		for (int i = 0; i < list2.Count; i++)
		{
			ThingDef thingDef = list2[i];
			if (thingDef != null && thingDef.HasComp<CompDressMilian>())
			{
				ApparelProperties apparel = thingDef.apparel;
				if (apparel != null && apparel.tags?.Contains(milianDef.defName) == true)
				{
					list.Add(thingDef);
				}
			}
		}
		DiaNode diaNode = new DiaNode(text);
		List<Dialog_InfoCard.Hyperlink> list3 = Dialog_InfoCard.DefsToHyperlinks(list).ToList();
		for (int j = 0; j < list3.Count; j++)
		{
			DiaOption item = new DiaOption(list[j].LabelCap)
			{
				hyperlink = list3[j]
			};
			diaNode.options.Add(item);
		}
		DiaOption item2 = new DiaOption("Ancot.Finish".Translate())
		{
			resolveTree = true
		};
		diaNode.options.Add(item2);
		return diaNode;
	}
}
