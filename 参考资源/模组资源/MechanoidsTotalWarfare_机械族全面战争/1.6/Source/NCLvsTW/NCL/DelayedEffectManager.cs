using System;
using System.Collections.Generic;
using Verse;

namespace NCL;

public class DelayedEffectManager : MapComponent
{
	private List<DelayedEffect> delayedActions = new List<DelayedEffect>();

	public DelayedEffectManager(Map map)
		: base(map)
	{
	}

	public void AddDelayedAction(DelayedEffect effect)
	{
		delayedActions.Add(effect);
	}

	public override void MapComponentTick()
	{
		base.MapComponentTick();
		for (int i = delayedActions.Count - 1; i >= 0; i--)
		{
			DelayedEffect effect = delayedActions[i];
			int elapsedTicks = Find.TickManager.TicksGame - effect.StartTick;
			if (elapsedTicks >= effect.DelayTicks)
			{
				try
				{
					effect.Action?.Invoke();
				}
				catch (Exception arg)
				{
					Log.Error($"Error executing delayed effect: {arg}");
				}
				delayedActions.RemoveAt(i);
			}
		}
	}
}
