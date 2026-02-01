using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GD3
{
	public class MiraculousFlowerTransition : MusicTransition
	{
		public override bool IsTransitionSatisfied()
		{
			return GDUtility.ExtraDrawer.pointer != null && GDUtility.ExtraDrawer.pointer.Spawned && GDUtility.ExtraDrawer.pointer.endTicker >= 0;
		}
	}
}