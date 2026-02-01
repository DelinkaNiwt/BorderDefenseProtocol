using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class VLayout : Widget
{
	public override void Update()
	{
		if (!Visible || Geometry.height <= 0f)
		{
			return;
		}
		Rect innerRect = base.InnerRect;
		if (innerRect.height <= 0f)
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
		float x = innerRect.x;
		float num = innerRect.y;
		float width = innerRect.width;
		float height = innerRect.height;
		float num2 = (float)Mathf.Max(0, list.Count - 1) * Spacing;
		height -= num2;
		if (height <= 0f)
		{
			foreach (Widget item in list)
			{
				item.Geometry = new Rect(x, num, width, 0f);
				item.Update();
			}
			return;
		}
		float[] array = new float[list.Count];
		float num3 = height;
		float num4 = 0f;
		for (int num5 = 0; num5 < list.Count; num5++)
		{
			Widget widget = list[num5];
			float valueOrDefault = widget.MinimalHeight.GetValueOrDefault();
			float num6 = widget.MaximalHeight ?? float.MaxValue;
			float num7 = (array[num5] = Mathf.Max(valueOrDefault, 0f));
			num3 -= num7;
			if (num3 < 0f)
			{
				array[num5] = Mathf.Max(0f, Mathf.Min(num7, height));
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
				float num9 = widget2.MaximalHeight ?? float.MaxValue;
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
			float num13 = width;
			if (widget3.MinimalWidth.HasValue)
			{
				num13 = Mathf.Max(num13, widget3.MinimalWidth.Value);
			}
			if (widget3.MaximalWidth.HasValue)
			{
				num13 = Mathf.Min(num13, widget3.MaximalWidth.Value);
			}
			widget3.Geometry = new Rect(x, num, num13, num12);
			num += num12;
			if (num11 < list.Count - 1)
			{
				num += Spacing;
			}
			widget3.Update();
		}
	}
}
