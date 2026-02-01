using System;
using UnityEngine;
using Verse;

namespace MoreWidgets;

public class ExpandedTip
{
	public readonly Action<Rect> draw;

	public readonly Func<Vector2> size;

	public readonly int uniqueId;

	public TooltipPriority priority;

	public float delay = 0.45f;

	public ExpandedTip(Action<Rect> draw, Func<Vector2> size, int uniqueId)
	{
		this.draw = draw;
		this.size = size;
		this.uniqueId = uniqueId;
	}
}
