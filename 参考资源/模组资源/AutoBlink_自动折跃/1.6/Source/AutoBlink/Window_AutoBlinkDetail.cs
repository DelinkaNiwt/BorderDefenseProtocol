using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AutoBlink;

public class Window_AutoBlinkDetail : Window
{
	private readonly List<CompAutoBlink> comps;

	private readonly Vector2 initPos;

	private const float WinW = 240f;

	private const float RowH = 24f;

	private const float IconSize = 24f;

	private const float CheckSize = 24f;

	private const float PaddingX = 3f;

	private const float GapY = 3f;

	private const float SliderH = 36f;

	private CompAutoBlink First => comps[0];

	public override Vector2 InitialSize
	{
		get
		{
			int num = 3;
			int maxRef = ((First.Props.maxDistanceToBlink > 0) ? First.Props.maxDistanceToBlink : 400);
			bool flag = comps.All((CompAutoBlink c) => ((c.Props.maxDistanceToBlink > 0) ? c.Props.maxDistanceToBlink : 400) == maxRef);
			float y = 32f + 24f * (float)num + 3f * (float)(num - 1) + (flag ? 39f : 0f) + 6f;
			return new Vector2(240f, y);
		}
	}

	public Window_AutoBlinkDetail(List<CompAutoBlink> compsList, Vector2 mousePosition)
	{
		comps = compsList.Where((CompAutoBlink c) => c.Props.drawGizmo).ToList();
		if (comps.Count == 0)
		{
			Close();
			return;
		}
		initPos = mousePosition;
		closeOnClickedOutside = true;
		draggable = false;
		absorbInputAroundWindow = false;
		preventCameraMotion = false;
		layer = WindowLayer.GameUI;
	}

	public override void PreOpen()
	{
		base.PreOpen();
		Vector2 initialSize = InitialSize;
		float x = initPos.x;
		float y = initPos.y - initialSize.y;
		windowRect = new Rect(x, y, initialSize.x, initialSize.y);
	}

	public override void DoWindowContents(Rect inRect)
	{
		float num = inRect.y;
		(Texture2D, string, Func<CompAutoBlink, bool>, Action<CompAutoBlink, bool>)[] array = new(Texture2D, string, Func<CompAutoBlink, bool>, Action<CompAutoBlink, bool>)[2]
		{
			(ContentFinder<Texture2D>.Get("UI/Commands/Draft"), "AutoBlink.Drafted_Label".Translate(), (CompAutoBlink c) => c.autoBlinkDrafted, delegate(CompAutoBlink c, bool v)
			{
				c.autoBlinkDrafted = v;
			}),
			(ContentFinder<Texture2D>.Get("UI/Buttons/AutoRebuild"), "AutoBlink.Idle_Label".Translate(), (CompAutoBlink c) => c.autoBlinkIdle, delegate(CompAutoBlink c, bool v)
			{
				c.autoBlinkIdle = v;
			})
		};
		(Texture2D, string, Func<CompAutoBlink, bool>, Action<CompAutoBlink, bool>)[] array2 = array;
		for (int num2 = 0; num2 < array2.Length; num2++)
		{
			(Texture2D, string, Func<CompAutoBlink, bool>, Action<CompAutoBlink, bool>) tuple = array2[num2];
			Rect rowRect = new Rect(inRect.x, num, inRect.width, 24f);
			DrawRow(rowRect, tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
			num += 27f;
		}
		Rect rect = new Rect(inRect.x, num, inRect.width, 24f);
		Widgets.DrawHighlightIfMouseover(rect);
		bool flag = comps.All((CompAutoBlink c) => c.jumpAsFarAsPossible);
		bool flag2 = comps.All((CompAutoBlink c) => !c.jumpAsFarAsPossible);
		bool? flag3 = (flag ? new bool?(true) : (flag2 ? new bool?(false) : ((bool?)null)));
		bool checkOn = flag3 ?? true;
		Rect rect2 = new Rect(rect.x + 3f, rect.y, rect.width - 6f - 24f, rect.height);
		Widgets.Label(rect2, "AutoBlink.JumpAsFarAsPossible_Label".Translate());
		Widgets.Checkbox(new Rect(rect.xMax - 24f - 3f, rect.y + (rect.height - 24f) * 0.5f, 24f, 24f).position, ref checkOn);
		if (Widgets.ButtonInvisible(rect))
		{
			bool jumpAsFarAsPossible = flag3 != true;
			foreach (CompAutoBlink comp in comps)
			{
				comp.jumpAsFarAsPossible = jumpAsFarAsPossible;
			}
			SoundDefOf.Click.PlayOneShotOnCamera();
		}
		num += 27f;
		int maxRef = ((First.Props.maxDistanceToBlink > 0) ? First.Props.maxDistanceToBlink : 400);
		if (!comps.All((CompAutoBlink c) => ((c.Props.maxDistanceToBlink > 0) ? c.Props.maxDistanceToBlink : 400) == maxRef))
		{
			return;
		}
		Rect rect3 = new Rect(inRect.x + 3f, num, inRect.width - 6f, 36f);
		int currentMinBlinkDist = First.CurrentMinBlinkDist;
		int num3 = ((First.Props.cellsBeforeTarget > 0) ? First.Props.cellsBeforeTarget : 2);
		float f = Widgets.HorizontalSlider(rect3, currentMinBlinkDist, num3, maxRef, middleAlignment: true, "AutoBlink.MinDist_Format".Translate(currentMinBlinkDist), num3.ToString(), maxRef.ToString());
		int num4 = Mathf.RoundToInt(f);
		if (num4 == currentMinBlinkDist)
		{
			return;
		}
		foreach (CompAutoBlink comp2 in comps)
		{
			comp2.CurrentMinBlinkDist = num4;
		}
		SoundDefOf.DragSlider.PlayOneShotOnCamera();
	}

	private void DrawRow(Rect rowRect, Texture2D icon, string labelText, Func<CompAutoBlink, bool> getter, Action<CompAutoBlink, bool> setter)
	{
		Widgets.DrawHighlightIfMouseover(rowRect);
		Rect rect = new Rect(rowRect.x + 3f, rowRect.y + (rowRect.height - 24f) * 0.5f, 24f, 24f);
		GUI.DrawTexture(rect, (Texture)icon);
		Rect rect2 = new Rect(rect.xMax + 3f, rowRect.y, rowRect.width - 24f - 24f - 9f, rowRect.height);
		Widgets.Label(rect2, labelText);
		bool flag = true;
		bool flag2 = true;
		foreach (CompAutoBlink comp in comps)
		{
			if (getter(comp))
			{
				flag2 = false;
			}
			else
			{
				flag = false;
			}
			if (!flag && !flag2)
			{
				break;
			}
		}
		bool? flag3 = (flag ? new bool?(true) : (flag2 ? new bool?(false) : ((bool?)null)));
		bool checkOn = flag3 ?? true;
		Widgets.Checkbox(new Rect(rowRect.xMax - 24f - 3f, rowRect.y + (rowRect.height - 24f) * 0.5f, 24f, 24f).position, ref checkOn);
		if (!Widgets.ButtonInvisible(rowRect))
		{
			return;
		}
		bool arg = flag3 != true;
		foreach (CompAutoBlink comp2 in comps)
		{
			setter(comp2, arg);
		}
		SoundDefOf.Click.PlayOneShotOnCamera();
	}
}
