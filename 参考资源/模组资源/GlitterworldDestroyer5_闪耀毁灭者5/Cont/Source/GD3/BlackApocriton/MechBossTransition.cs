using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GD3
{
	public class MechBossTransition : MusicTransition
	{
		public override bool IsTransitionSatisfied()
		{
			for (int i = 0; i < Find.Maps.Count; i++)
			{
				Map map = Find.Maps[i];
				if (map.listerThings.ThingsInGroup(ThingRequestGroup.ThingHolder).Any(t => (t is IThingHolder holder) && holder.GetDirectlyHeldThings() != null && holder.GetDirectlyHeldThings().Any(c => Fit(c))))
                {
					return true;
                }
				if (map.listerThings.AllThings.Any(t => Fit(t)))
				{
					return true;
				}
			}
			return false;
		}

		private bool Fit(Thing thing)
        {
			if (thing is BossMusic music)
            {
				return music.Music == def.sequence.song && music.IsPlaying;
			}
			return false;
        }
	}
}