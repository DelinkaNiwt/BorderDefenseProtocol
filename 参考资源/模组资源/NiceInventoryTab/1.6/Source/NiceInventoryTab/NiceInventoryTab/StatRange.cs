using System;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class StatRange : StatDrawer
{
	public Func<Pawn, StatDrawer, (float, float)> StatRangeWorker;

	public float ValueSecond;

	public string FinalString = string.Empty;

	public string AltTitle = string.Empty;

	public override string FinalValue
	{
		get
		{
			switch (Mode)
			{
			case FormatMode.Percent:
				if (Value < 0.1f)
				{
					return Value.ToStringPercent("0.#") + " ~ " + ValueSecond.ToStringPercent("0.#");
				}
				return Value.ToStringPercent() + " ~ " + ValueSecond.ToStringPercent();
			case FormatMode.Temperature:
				return Value.ToStringTemperature() + " ~ " + ValueSecond.ToStringTemperature();
			default:
				return Value.ToString(Format) + " ~ " + ValueSecond.ToString(Format);
			}
		}
	}

	public StatRange(string title, string descr, Func<Pawn, StatDrawer, (float, float)> worker)
	{
		Title = title;
		Descr = descr;
		StatRangeWorker = worker;
		SetFixedHeight(StatDrawer.StatFixedBaseHeight);
	}

	public override void UpdateValues(Pawn pawn)
	{
		(float, float) tuple = StatRangeWorker(pawn, this);
		float item = tuple.Item1;
		float item2 = tuple.Item2;
		Value = item;
		ValueSecond = item2;
		FinalString = Title + FinalValue;
		Text.Font = GameFont.Small;
		if (Utils.CalcWidth(FinalString) > Geometry.width)
		{
			FinalString = AltTitle + FinalValue;
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
		Widgets.Label(Geometry, FinalString);
		Text.Anchor = TextAnchor.UpperLeft;
	}
}
