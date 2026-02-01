using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class Widget
{
	public List<Widget> Childs = new List<Widget>();

	public float Stretch = 1f;

	public Rect Geometry;

	public bool Visible = true;

	public float? MinimalHeight;

	public float? MinimalWidth;

	public float? MaximalHeight;

	public float? MaximalWidth;

	public Color? DebugColor;

	public float Spacing = 6f;

	public float MarginLeft;

	public float MarginRight;

	public float MarginTop;

	public float MarginBottom;

	protected Rect InnerRect => new Rect(Geometry.x + MarginLeft, Geometry.y + MarginTop, Mathf.Max(0f, Geometry.width - MarginLeft - MarginRight), Mathf.Max(0f, Geometry.height - MarginTop - MarginBottom));

	public void SetFixedHeight(float h)
	{
		MinimalHeight = h;
		MaximalHeight = h;
	}

	public void SetFixedWidth(float w)
	{
		MinimalWidth = w;
		MaximalWidth = w;
	}

	public virtual void AddChild(Widget layout)
	{
		Childs.Add(layout);
	}

	public virtual void InsertChild(Widget layout)
	{
		Childs.Insert(0, layout);
	}

	public virtual void Update()
	{
		Rect innerRect = InnerRect;
		foreach (Widget child in Childs)
		{
			child.Geometry = innerRect;
			child.Update();
		}
	}

	public virtual void Draw()
	{
		if (!Visible)
		{
			return;
		}
		if (DebugColor.HasValue)
		{
			GUI.color = DebugColor.Value;
			Widgets.DrawBox(Geometry, 2);
		}
		foreach (Widget child in Childs)
		{
			if (child.Visible)
			{
				child.Draw();
			}
		}
	}
}
