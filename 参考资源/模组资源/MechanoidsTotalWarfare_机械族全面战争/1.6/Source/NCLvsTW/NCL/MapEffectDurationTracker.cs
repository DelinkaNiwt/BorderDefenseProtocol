using System.Collections.Generic;
using Verse;

namespace NCL;

public class MapEffectDurationTracker : MapComponent
{
	private class EffecterDuration
	{
		public Effecter effecter;

		public int ticksRemaining;

		public EffecterDuration(Effecter effecter, int durationTicks)
		{
			this.effecter = effecter;
			ticksRemaining = durationTicks;
		}

		public bool Tick()
		{
			if (--ticksRemaining <= 0)
			{
				effecter.Cleanup();
				return true;
			}
			return false;
		}
	}

	private List<EffecterDuration> activeEffects = new List<EffecterDuration>();

	public MapEffectDurationTracker(Map map)
		: base(map)
	{
	}

	public override void MapComponentTick()
	{
		base.MapComponentTick();
		for (int i = activeEffects.Count - 1; i >= 0; i--)
		{
			if (activeEffects[i].Tick())
			{
				activeEffects.RemoveAt(i);
			}
		}
	}

	public void AddEffecterForDuration(Effecter effecter, int durationTicks)
	{
		activeEffects.Add(new EffecterDuration(effecter, durationTicks));
	}
}
