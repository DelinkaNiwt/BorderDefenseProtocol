using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded.Graphics;

public class Graphic_Animated : Graphic_Collection
{
	private readonly int offset = Rand.Range(1, 1000);

	public override Material MatSingle => CurFrame?.MatSingle;

	private Graphic CurFrame
	{
		get
		{
			Graphic[] array = subGraphics;
			if (array == null)
			{
				return null;
			}
			return array[Mathf.FloorToInt(((((float?)Current.Game?.tickManager?.TicksGame) ?? 0f) + (float)offset) / (float)((GraphicData_Animated)data).ticksPerFrame) % subGraphics.Length];
		}
	}

	public int SubGraphicCount => subGraphics.Length - 1;

	public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
	{
		if (thing is IAnimationOneTime animationOneTime)
		{
			int num = animationOneTime.CurrentIndex();
			Graphic[] array = subGraphics;
			if (array != null)
			{
				array[num]?.DrawWorker(loc, rot, thingDef, thing, extraRotation);
			}
		}
		else
		{
			CurFrame?.DrawWorker(loc, rot, thingDef, thing, extraRotation);
		}
	}
}
