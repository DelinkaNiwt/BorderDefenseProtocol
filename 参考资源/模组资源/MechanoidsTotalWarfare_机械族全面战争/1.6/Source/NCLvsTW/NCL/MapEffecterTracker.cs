using System.Collections.Generic;
using Verse;

namespace NCL;

public class MapEffecterTracker : MapComponent
{
	private class ManagedEffecter
	{
		public Effecter effecter;

		public TargetInfo target;

		public int ticksLeft;

		public ManagedEffecter(Effecter effecter, TargetInfo target, int duration)
		{
			this.effecter = effecter;
			this.target = target;
			ticksLeft = duration;
		}

		public bool Tick()
		{
			if (ticksLeft <= 0)
			{
				return false;
			}
			ticksLeft--;
			effecter.EffectTick(target, target);
			if (ticksLeft <= 0)
			{
				effecter.Cleanup();
				return false;
			}
			return true;
		}
	}

	private List<ManagedEffecter> activeEffecters = new List<ManagedEffecter>();

	public MapEffecterTracker(Map map)
		: base(map)
	{
	}

	public override void MapComponentTick()
	{
		base.MapComponentTick();
		for (int i = activeEffecters.Count - 1; i >= 0; i--)
		{
			if (!activeEffecters[i].Tick())
			{
				activeEffecters.RemoveAt(i);
			}
		}
	}

	public void AddEffecter(Effecter effecter, TargetInfo target, int duration)
	{
		activeEffecters.Add(new ManagedEffecter(effecter, target, duration));
	}
}
