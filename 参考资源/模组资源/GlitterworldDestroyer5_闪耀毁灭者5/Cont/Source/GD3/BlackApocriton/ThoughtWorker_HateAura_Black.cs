using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace GD3
{
	public class ThoughtWorker_HateAura_Black : ThoughtWorker
	{
		protected override ThoughtState CurrentStateInternal(Pawn p)
		{
			ThoughtWorker_HateAura_Black.HateAuraLevel hateAuraLevel = ThoughtWorker_HateAura_Black.HateAuraLevel.None;
			if (p.Map != null)
			{
				List<Thing> list = p.Map.listerThings.ThingsOfDef(GDDefOf.Mech_BlackApocriton);
				list.SortBy((Thing m) => m.Position.DistanceToSquared(m.Position));
				if (list.Count > 0)
				{
					float num = list[0].Position.DistanceTo(p.Position);
					if (num <= 12.9f)
					{
						hateAuraLevel = ThoughtWorker_HateAura_Black.HateAuraLevel.Intense;
					}
					else if (num <= 32.9f)
					{
						hateAuraLevel = ThoughtWorker_HateAura_Black.HateAuraLevel.Strong;
					}
					else
					{
						hateAuraLevel = ThoughtWorker_HateAura_Black.HateAuraLevel.Distant;
					}
				}
			}
			switch (hateAuraLevel)
			{
				case ThoughtWorker_HateAura_Black.HateAuraLevel.None:
					return false;
				case ThoughtWorker_HateAura_Black.HateAuraLevel.Intense:
					return ThoughtState.ActiveAtStage(0);
				case ThoughtWorker_HateAura_Black.HateAuraLevel.Strong:
					return ThoughtState.ActiveAtStage(1);
				case ThoughtWorker_HateAura_Black.HateAuraLevel.Distant:
					return ThoughtState.ActiveAtStage(2);
				default:
					throw new NotImplementedException();
			}
		}

		private enum HateAuraLevel
		{
			None,
			Intense,
			Strong,
			Distant
		}
	}
}