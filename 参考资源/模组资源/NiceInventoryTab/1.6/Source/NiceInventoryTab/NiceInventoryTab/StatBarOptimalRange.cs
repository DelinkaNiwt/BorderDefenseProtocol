using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class StatBarOptimalRange : StatBar
{
	public (float min, float max)? Optimal;

	public StatBarOptimalRange(string title, string descr, Func<Pawn, StatDrawer, float> v_worker, Func<Pawn, float> max_worker, Assets.IconColor ic, FloatRef tsep = null, FloatRef digitSep = null)
		: base(title, descr, v_worker, max_worker, ic, tsep, digitSep)
	{
	}

	public override void UpdateValues(Pawn pawn)
	{
		base.UpdateValues(pawn);
		Optimal = null;
		Descr = "NIT_MaxRangeOfAttackTip".Translate() + "\n\n";
		Thing pawnWeapon = DamageUtility.GetPawnWeapon(pawn);
		if (pawnWeapon == null || !pawnWeapon.def.IsRangedWeapon)
		{
			return;
		}
		float range_Internal = DamageUtility.GetRange_Internal(pawnWeapon, null, pawn);
		float item = DamageUtility.FindOptimalRange(pawnWeapon, pawn).percent;
		float num = Mathf.Max(Utils.Snap(item * 0.15f, 0.01f), 0.025f);
		float threshold = item - num;
		List<(float, float)> list = DamageUtility.FindRangesAboveThreshold(pawnWeapon, threshold, pawn).ToList();
		if (list.Count > 0)
		{
			(float, float) tuple = list.Min();
			(float, float) tuple2 = list.Max();
			Optimal = (tuple.Item1 / Value, tuple2.Item1 / Value);
			float f = (Mathf.Min(tuple.Item2, tuple2.Item2) + item) * 0.5f;
			Descr += "NIT_OptimalRangeTip".Translate(tuple.Item1, tuple2.Item1, f.ToStringPercent(), (num * 0.5f).ToStringPercent());
			float num2 = (tuple2.Item1 + range_Internal) / 2f;
			if (Mathf.Abs(num2 - tuple2.Item1) > 3f)
			{
				Descr += "\n";
				Descr += "NIT_AtRangeTip".Translate(num2, DamageUtility.GetAdjustedHitChanceFactor(pawnWeapon, num2, pawn).ToStringPercent());
			}
			if (Mathf.Abs(range_Internal - tuple2.Item1) > 3f)
			{
				Descr += "\n";
				Descr += "NIT_AtRangeTip".Translate(range_Internal, DamageUtility.GetAdjustedHitChanceFactor(pawnWeapon, range_Internal, pawn).ToStringPercent());
			}
		}
	}

	public override void DrawBar(Rect bar)
	{
		float num = Value / MaxValue;
		if (!(num <= 0f))
		{
			Rect rect = bar.LeftPart(num);
			Color color = (GUI.color = Settings.ColorCorrect(ColorBar));
			Assets.DrawTilingTexture(rect, Assets.DiagTiledTex, 64f, Vector2.zero);
			if (Optimal.HasValue)
			{
				float xMin = rect.xMin;
				float width = rect.width;
				rect.xMin = xMin + width * Optimal.Value.min;
				rect.xMax = xMin + width * Optimal.Value.max;
				Widgets.DrawBoxSolid(rect, color);
				Widgets.DrawBoxSolid(new Rect(rect.x - 2f, rect.y, 2f, rect.height), Assets.ColorBG);
				Widgets.DrawBoxSolid(new Rect(rect.xMax, rect.y, 2f, rect.height), Assets.ColorBG);
			}
		}
	}
}
