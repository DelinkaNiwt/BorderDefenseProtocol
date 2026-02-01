using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class HLayout : Widget
{
	public override void Update()
	{
		if (!Visible || Geometry.width <= 0f)
		{
			return;
		}
		Rect innerRect = base.InnerRect;
		if (innerRect.width <= 0f)
		{
			return;
		}
		List<Widget> list = Childs.Where((Widget c) => c.Visible).ToList();
		if (!list.Any())
		{
			return;
		}
		if (list.Sum((Widget c) => c.Stretch) <= 0f)
		{
			_ = list.Count;
		}
		float num = innerRect.x;
		float y = innerRect.y;
		float height = innerRect.height;
		float width = innerRect.width;
		float num2 = (float)Mathf.Max(0, list.Count - 1) * Spacing;
		width -= num2;
		if (width <= 0f)
		{
			foreach (Widget item in list)
			{
				item.Geometry = new Rect(num, y, 0f, height);
				item.Update();
			}
			return;
		}
		float[] array = new float[list.Count];
		float num3 = width;
		float num4 = 0f;
		for (int num5 = 0; num5 < list.Count; num5++)
		{
			Widget widget = list[num5];
			float valueOrDefault = widget.MinimalWidth.GetValueOrDefault();
			float num6 = widget.MaximalWidth ?? float.MaxValue;
			float num7 = (array[num5] = Mathf.Max(valueOrDefault, 0f));
			num3 -= num7;
			if (num3 < 0f)
			{
				array[num5] = Mathf.Max(0f, Mathf.Min(num7, width));
				num3 = 0f;
				break;
			}
			if (widget.Stretch > 0f && num7 < num6)
			{
				num4 += widget.Stretch;
			}
		}
		if (num3 > 0f && num4 > 0f)
		{
			for (int num8 = 0; num8 < list.Count; num8++)
			{
				Widget widget2 = list[num8];
				if (widget2.Stretch <= 0f)
				{
					continue;
				}
				float num9 = widget2.MaximalWidth ?? float.MaxValue;
				float num10 = array[num8];
				if (!(num10 >= num9))
				{
					float b = Mathf.Min(widget2.Stretch / num4 * num3, num9 - num10);
					b = Mathf.Max(0f, b);
					array[num8] += b;
					num3 -= b;
					num4 -= widget2.Stretch;
					if (num3 <= 0.001f)
					{
						break;
					}
				}
			}
		}
		for (int num11 = 0; num11 < list.Count; num11++)
		{
			Widget widget3 = list[num11];
			float num12 = array[num11];
			float num13 = height;
			if (widget3.MinimalHeight.HasValue)
			{
				num13 = Mathf.Max(num13, widget3.MinimalHeight.Value);
			}
			if (widget3.MaximalHeight.HasValue)
			{
				num13 = Mathf.Min(num13, widget3.MaximalHeight.Value);
			}
			widget3.Geometry = new Rect(num, y, num12, num13);
			num += num12;
			if (num11 < list.Count - 1)
			{
				num += Spacing;
			}
			widget3.Update();
		}
	}
}
