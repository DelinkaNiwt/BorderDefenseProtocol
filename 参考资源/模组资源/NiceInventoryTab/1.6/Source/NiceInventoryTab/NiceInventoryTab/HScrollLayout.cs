using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class HScrollLayout : Widget
{
	public static readonly float ScrollSize = 12f;

	private Vector2 scrollPosition = Vector2.zero;

	private float contentWidth;

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
			contentWidth = 0f;
			return;
		}
		if (list.Sum((Widget c) => c.Stretch) <= 0f)
		{
			_ = list.Count;
		}
		float num = MarginLeft;
		float marginTop = MarginTop;
		float height = innerRect.height;
		Mathf.Max(0, list.Count - 1);
		_ = Spacing;
		float[] array = new float[list.Count];
		float num2 = 0f;
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			Widget widget = list[num3];
			float valueOrDefault = widget.MinimalWidth.GetValueOrDefault();
			float num4 = widget.MaximalWidth ?? float.MaxValue;
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
					float num8 = widget2.MaximalWidth ?? float.MaxValue;
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
		contentWidth = 0f;
		for (int num10 = 0; num10 < list.Count; num10++)
		{
			Widget widget3 = list[num10];
			float num11 = array[num10];
			float num12 = height;
			if (widget3.MinimalHeight.HasValue)
			{
				num12 = Mathf.Max(num12, widget3.MinimalHeight.Value);
			}
			if (widget3.MaximalHeight.HasValue)
			{
				num12 = Mathf.Min(num12, widget3.MaximalHeight.Value);
			}
			widget3.Geometry = new Rect(num, marginTop, num11, num12);
			num += num11;
			if (num10 < list.Count - 1)
			{
				num += Spacing;
			}
			widget3.Update();
		}
		contentWidth = num + MarginRight;
		if (contentWidth > Geometry.width)
		{
			for (int num13 = 0; num13 < list.Count; num13++)
			{
				list[num13].Geometry.yMax -= ScrollSize;
			}
		}
	}

	public override void Draw()
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Invalid comparison between Unknown and I4
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
		Event current = Event.current;
		if (Geometry.Contains(current.mousePosition) && (int)current.type == 6)
		{
			scrollPosition.x += current.delta.y * 20f;
			scrollPosition.x = Mathf.Clamp(scrollPosition.x, 0f, contentWidth - Geometry.width);
			current.Use();
		}
		float num = innerRect.height;
		if (contentWidth > Geometry.width)
		{
			num -= ScrollSize;
		}
		scrollPosition = GUI.BeginScrollView(Geometry, scrollPosition, new Rect(0f, 0f, contentWidth, num), false, false);
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
