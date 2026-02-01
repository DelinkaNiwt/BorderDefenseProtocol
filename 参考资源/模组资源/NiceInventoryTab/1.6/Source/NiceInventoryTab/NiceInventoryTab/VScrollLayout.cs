using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class VScrollLayout : Widget
{
	public static readonly float ScrollSize = 14f;

	public static readonly float ScrollHOffset = 16f;

	private Vector2 scrollPosition = Vector2.zero;

	private float contentHeight;

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
			contentHeight = 0f;
			return;
		}
		if (list.Sum((Widget c) => c.Stretch) <= 0f)
		{
			_ = list.Count;
		}
		float marginLeft = MarginLeft;
		float num = MarginTop;
		float width = innerRect.width;
		Mathf.Max(0, list.Count - 1);
		_ = Spacing;
		float[] array = new float[list.Count];
		float num2 = 0f;
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			Widget widget = list[num3];
			float valueOrDefault = widget.MinimalHeight.GetValueOrDefault();
			float num4 = widget.MaximalHeight ?? float.MaxValue;
			float num5 = (array[num3] = Mathf.Max(valueOrDefault, 0f));
			if (widget.Stretch > 0f && num5 < num4)
			{
				num2 += widget.Stretch;
			}
		}
		if (num2 > 0f)
		{
			float num6 = 10000f;
			for (int num7 = 0; num7 < list.Count; num7++)
			{
				Widget widget2 = list[num7];
				if (!(widget2.Stretch <= 0f))
				{
					float num8 = widget2.MaximalHeight ?? float.MaxValue;
					float num9 = array[num7];
					if (!(num9 >= num8))
					{
						float b = Mathf.Min(widget2.Stretch / num2 * num6, num8 - num9);
						b = Mathf.Max(0f, b);
						array[num7] += b;
					}
				}
			}
		}
		contentHeight = 0f;
		for (int num10 = 0; num10 < list.Count; num10++)
		{
			Widget widget3 = list[num10];
			float num11 = array[num10];
			float num12 = width;
			if (widget3.MinimalWidth.HasValue)
			{
				num12 = Mathf.Max(num12, widget3.MinimalWidth.Value);
			}
			if (widget3.MaximalWidth.HasValue)
			{
				num12 = Mathf.Min(num12, widget3.MaximalWidth.Value);
			}
			widget3.Geometry = new Rect(marginLeft, num, num12, num11);
			num += num11;
			if (num10 < list.Count - 1)
			{
				num += Spacing;
			}
			widget3.Update();
		}
		contentHeight = num + MarginBottom;
		if (contentHeight - ScrollHOffset > base.InnerRect.height)
		{
			for (int num13 = 0; num13 < list.Count; num13++)
			{
				list[num13].Geometry.xMax -= ScrollSize;
			}
		}
	}

	public override void Draw()
	{
		if (!Visible)
		{
			return;
		}
		Rect innerRect = base.InnerRect;
		if (DebugColor.HasValue)
		{
			GUI.color = DebugColor.Value;
			Widgets.DrawBox(Geometry, 2);
			GUI.color = Color.white;
		}
		float num = innerRect.width;
		if (contentHeight - ScrollHOffset > base.InnerRect.height)
		{
			num -= ScrollSize;
		}
		scrollPosition = GUI.BeginScrollView(Geometry, scrollPosition, new Rect(0f, 0f, num, contentHeight), false, false);
		foreach (Widget child in Childs)
		{
			if (child.Visible)
			{
				child.Draw();
			}
		}
		GUI.EndScrollView();
	}
}
