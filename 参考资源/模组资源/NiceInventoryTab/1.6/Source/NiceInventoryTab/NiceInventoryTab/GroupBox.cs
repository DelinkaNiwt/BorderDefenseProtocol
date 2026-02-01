using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class GroupBox : VLayout
{
	public string Title;

	public static float TitleHeight = 20f;

	public static float BasicMargin = 4f;

	public Widget InLayout => Childs[0];

	public GroupBox(string title, float stretch)
	{
		Title = title;
		Stretch = stretch;
		MarginTop = BasicMargin + TitleHeight;
		MarginLeft = BasicMargin;
		MarginRight = BasicMargin;
		MarginBottom = BasicMargin;
		MinimalHeight = TitleHeight;
	}

	public void ResetMarginsToZero()
	{
		MarginTop = TitleHeight;
		MarginLeft = 0f;
		MarginRight = 0f;
		MarginBottom = 0f;
	}

	public void SetHorizonalLayout(bool Scrollable = false)
	{
		if (Scrollable)
		{
			ResetMarginsToZero();
			HScrollLayout hScrollLayout = new HScrollLayout();
			base.AddChild(hScrollLayout);
			hScrollLayout.MarginTop = BasicMargin;
			hScrollLayout.MarginLeft = BasicMargin;
			hScrollLayout.MarginRight = BasicMargin;
			hScrollLayout.MarginBottom = BasicMargin;
			hScrollLayout.Spacing = 3f;
		}
		else
		{
			base.AddChild(new HLayout());
		}
	}

	public void SetVerticalLayout(bool Scrollable = false)
	{
		if (Scrollable)
		{
			ResetMarginsToZero();
			VScrollLayout vScrollLayout = new VScrollLayout();
			base.AddChild(vScrollLayout);
			vScrollLayout.MarginTop = BasicMargin;
			vScrollLayout.MarginLeft = BasicMargin;
			vScrollLayout.MarginRight = BasicMargin;
			vScrollLayout.MarginBottom = BasicMargin;
			vScrollLayout.Spacing = 3f;
		}
		else
		{
			base.AddChild(new VLayout());
		}
	}

	public override void AddChild(Widget layout)
	{
		if (Childs.Empty())
		{
			SetVerticalLayout();
		}
		InLayout.AddChild(layout);
	}

	public override void Draw()
	{
		if (!Visible)
		{
			return;
		}
		Widgets.DrawBoxSolid(Geometry, Assets.ColorBGL);
		Rect rect = new Rect(Geometry.position, new Vector2(Geometry.width, TitleHeight));
		Widgets.DrawBoxSolid(rect, Assets.ColorBGD);
		Text.Font = GameFont.Small;
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect, Title);
		Text.Anchor = TextAnchor.UpperLeft;
		if (DebugColor.HasValue)
		{
			GUI.color = DebugColor.Value;
			Widgets.DrawBox(base.InnerRect, 2);
		}
		if (Childs.Empty())
		{
			return;
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
