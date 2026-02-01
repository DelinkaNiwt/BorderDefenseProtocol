using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded.Graphics;

public class Graphic_Fleck_Animated : Graphic_FleckCollection
{
	public override void DrawFleck(FleckDrawData drawData, DrawBatch batch)
	{
		GraphicData_Animated graphicData_Animated = (GraphicData_Animated)data;
		float num = ((float?)Current.Game?.tickManager?.TicksGame) ?? 0f;
		int num2 = ((!graphicData_Animated.random) ? (Mathf.FloorToInt(drawData.ageSecs * 60f / (float)graphicData_Animated.ticksPerFrame) % subGraphics.Length) : (Mathf.FloorToInt(num / (float)graphicData_Animated.ticksPerFrame) % subGraphics.Length));
		Graphic_Fleck[] array = subGraphics;
		if (array != null)
		{
			array[num2].DrawFleck(drawData, batch);
		}
	}
}
