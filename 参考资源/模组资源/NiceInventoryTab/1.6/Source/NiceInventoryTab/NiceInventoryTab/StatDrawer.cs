using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class StatDrawer : Widget
{
	public enum FormatMode
	{
		Common,
		Percent,
		Temperature,
		PercentDay
	}

	public static float StatFixedBaseHeight = 20f;

	public string Title;

	public string Descr;

	public StatDef Stat;

	public Func<Pawn, StatDrawer, float> StatWorker;

	public string Format = "0.##";

	public FormatMode Mode;

	public float Value;

	public virtual string FinalValue
	{
		get
		{
			switch (Mode)
			{
			case FormatMode.Percent:
				if (Value < 0.1f)
				{
					return Value.ToStringPercent("0.#");
				}
				return Value.ToStringPercent();
			case FormatMode.PercentDay:
				return Value.ToStringPercent() + Assets.Format_PD;
			case FormatMode.Temperature:
				return Value.ToStringTemperature();
			default:
				return Value.ToString(Format);
			}
		}
	}

	public StatDrawer(string title, string descr, StatDef s = null)
	{
		Title = title;
		Descr = descr;
		Stat = s;
		MinimalHeight = StatFixedBaseHeight;
	}

	public StatDrawer()
	{
		MinimalHeight = StatFixedBaseHeight;
	}

	public StatDrawer SetFormat(string s)
	{
		Format = s;
		return this;
	}

	public StatDrawer SetFormatMode(FormatMode m)
	{
		Mode = m;
		return this;
	}

	public virtual void UpdateValues(Pawn pawn)
	{
		if (StatWorker != null)
		{
			Value = StatWorker(pawn, this);
		}
		else if (Stat != null)
		{
			Value = pawn.GetStatValue(Stat);
		}
		else
		{
			Value = 0f;
		}
	}

	public override void Draw()
	{
		if (Mouse.IsOver(Geometry) && !Descr.NullOrEmpty())
		{
			Assets.DrawHighlightHorizontal(Geometry, 0.2f);
		}
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		GUI.color = Assets.ColorStat;
		Widgets.Label(Geometry, Title + FinalValue);
		Text.Anchor = TextAnchor.UpperLeft;
	}
}
