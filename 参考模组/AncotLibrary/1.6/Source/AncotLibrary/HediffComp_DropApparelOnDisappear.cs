using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class HediffComp_DropApparelOnDisappear : HediffComp
{
	private HediffCompProperties_DropApparelOnDisappear Props => (HediffCompProperties_DropApparelOnDisappear)props;

	public override void CompPostPostRemoved()
	{
		if (base.Pawn?.apparel?.WornApparel == null)
		{
			return;
		}
		List<Apparel> list = base.Pawn.apparel.WornApparel.Where((Apparel apparel) => apparel.def.apparel?.tags != null && apparel.def.apparel.tags.Any((string tag) => Props.apparelTags.Contains(tag))).ToList();
		foreach (Apparel item in list)
		{
			Messages.Message("Ancot.DropEquipmentOnHediffRemoved".Translate(base.Pawn.Label, parent.Label, item.Label), MessageTypeDefOf.NeutralEvent);
			base.Pawn.apparel.TryDrop(item);
		}
	}
}
