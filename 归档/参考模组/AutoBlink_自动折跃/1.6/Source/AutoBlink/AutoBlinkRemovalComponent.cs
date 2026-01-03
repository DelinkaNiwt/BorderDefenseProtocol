using System;
using System.Collections.Generic;
using Verse;

namespace AutoBlink;

public class AutoBlinkRemovalComponent : GameComponent
{
	private readonly List<CompAutoBlink> queue = new List<CompAutoBlink>(16);

	public List<CompAutoBlink> Queue => queue;

	public AutoBlinkRemovalComponent(Game _)
	{
	}

	public override void GameComponentTick()
	{
		if (queue.Count == 0)
		{
			return;
		}
		for (int num = queue.Count - 1; num >= 0; num--)
		{
			CompAutoBlink compAutoBlink = queue[num];
			if (!(compAutoBlink?.parent is Pawn pawn))
			{
				queue.RemoveAt(num);
			}
			else
			{
				try
				{
					pawn.AllComps?.Remove(compAutoBlink);
					queue.RemoveAt(num);
				}
				catch (Exception arg)
				{
					Log.Error(string.Format("[AutoBlink] Failed to remove comp from {0}: {1}", pawn?.Name?.ToStringShort ?? "unknown pawn", arg));
					queue.RemoveAt(num);
				}
			}
		}
	}

	public void EnqueueForRemoval(CompAutoBlink comp)
	{
		if (comp != null && !queue.Contains(comp))
		{
			queue.Add(comp);
		}
	}
}
