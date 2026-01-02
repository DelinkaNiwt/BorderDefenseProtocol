using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityAICast_FireNearby : CompAbilityEffect
{
	private new CompProperties_AICast_FireNearby Props => (CompProperties_AICast_FireNearby)props;

	public Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		if (Caster.GetAttachment(ThingDefOf.Fire) != null)
		{
			return true;
		}
		int num = GenRadial.NumCellsInRadius(Props.radius);
		for (int i = 0; i < num; i++)
		{
			IntVec3 c = Caster.Position + GenRadial.RadialPattern[i];
			if (!c.InBounds(Caster.Map))
			{
				continue;
			}
			List<Thing> thingList = c.GetThingList(Caster.Map);
			for (int j = 0; j < thingList.Count; j++)
			{
				if (thingList[j] is Fire || thingList[j].HasAttachment(ThingDefOf.Fire))
				{
					return true;
				}
			}
		}
		return false;
	}
}
