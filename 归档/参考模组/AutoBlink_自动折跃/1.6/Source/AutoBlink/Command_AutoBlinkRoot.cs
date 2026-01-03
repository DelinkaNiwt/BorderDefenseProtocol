using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AutoBlink;

[StaticConstructorOnStartup]
public class Command_AutoBlinkRoot : Command_Toggle
{
	public CompAutoBlink Comp;

	private static readonly Texture2D CooldownBarTex = SolidColorMaterials.NewSolidColorTexture(new Color32(9, 107, 203, 72));

	private static readonly Texture2D AutoToggleOnTex = ContentFinder<Texture2D>.Get("UI/AutoToggleIcon");

	private bool _drawAsCooldown;

	private static readonly List<CompAutoBlink> tmpSelectedComps = new List<CompAutoBlink>();

	private int CooldownTicksRemaining
	{
		get
		{
			int ticksGame = Find.TickManager.TicksGame;
			if (Find.Selector.NumSelected <= 1)
			{
				int a = Comp.Props.blinkIntervalTicks - (ticksGame - Comp.lastBlinkTick);
				return Mathf.Max(a, 0);
			}
			List<CompAutoBlink> list = SelectedComps();
			int num = 0;
			foreach (CompAutoBlink item in list)
			{
				int num2 = item.Props.blinkIntervalTicks - (ticksGame - item.lastBlinkTick);
				if (num2 > num)
				{
					num = num2;
				}
			}
			return Mathf.Max(num, 0);
		}
	}

	public override bool InheritInteractionsFrom(Gizmo other)
	{
		return false;
	}

	private static List<CompAutoBlink> SelectedComps()
	{
		tmpSelectedComps.Clear();
		foreach (object selectedObject in Find.Selector.SelectedObjects)
		{
			if (!(selectedObject is Pawn pawn))
			{
				continue;
			}
			foreach (ThingComp allComp in pawn.AllComps)
			{
				if (allComp is CompAutoBlink compAutoBlink && compAutoBlink.Props.drawGizmo)
				{
					tmpSelectedComps.Add(compAutoBlink);
				}
			}
		}
		return tmpSelectedComps;
	}

	public Command_AutoBlinkRoot()
	{
		isActive = delegate
		{
			List<CompAutoBlink> list = SelectedComps();
			return list.Count > 0 && list.All((CompAutoBlink c) => c.autoBlinkMaster);
		};
		toggleAction = delegate
		{
		};
	}

	public override bool GroupsWith(Gizmo other)
	{
		if (Find.Selector.NumSelected <= 1)
		{
			return false;
		}
		return other is Command_AutoBlinkRoot;
	}

	public override void ProcessInput(Event ev)
	{
		bool flag = Find.Selector.NumSelected <= 1;
		List<CompAutoBlink> validComps;
		if (ev.button == 1)
		{
			if (ev.shift)
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
				List<CompAutoBlink> compsList = (flag ? new List<CompAutoBlink> { Comp } : SelectedComps());
				Vector2 mousePositionOnUIInverted = UI.MousePositionOnUIInverted;
				Find.WindowStack.Add(new Window_AutoBlinkDetail(compsList, mousePositionOnUIInverted));
				return;
			}
			if (flag)
			{
				Comp.autoBlinkMaster = !Comp.autoBlinkMaster;
			}
			else
			{
				List<CompAutoBlink> list = SelectedComps();
				bool autoBlinkMaster = !list.First().autoBlinkMaster;
				foreach (CompAutoBlink item in list)
				{
					item.autoBlinkMaster = autoBlinkMaster;
				}
			}
			SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
		}
		else if (ev.button == 0)
		{
			if (CooldownTicksRemaining > 0)
			{
				SoundDefOf.ClickReject.PlayOneShotOnCamera();
				return;
			}
			List<CompAutoBlink> source = (flag ? new List<CompAutoBlink> { Comp } : SelectedComps());
			validComps = source.Where((CompAutoBlink c) => c.parent is Pawn p && !IsIncapPawn(p)).ToList();
			if (validComps.Count == 0)
			{
				SoundDefOf.ClickReject.PlayOneShotOnCamera();
				return;
			}
			int num = validComps.Max((CompAutoBlink c) => (c.Props.maxDistanceToBlink > 0) ? c.Props.maxDistanceToBlink : 400);
			TargetingParameters targetParams = new TargetingParameters
			{
				canTargetLocations = true,
				canTargetSelf = false,
				canTargetBuildings = false,
				canTargetFires = false,
				canTargetPawns = false
			};
			Find.Targeter.BeginTargeting(targetParams, OnTargetChosen, null, Validator);
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
		}
		else
		{
			base.ProcessInput(ev);
		}
		static bool IsIncapPawn(Pawn p)
		{
			return p.DevelopmentalStage.Baby() || p.Downed || p.Deathresting;
		}
		void OnTargetChosen(LocalTargetInfo t)
		{
			IntVec3 cell = t.Cell;
			foreach (CompAutoBlink item2 in validComps)
			{
				item2.BlinkToCellDirect(cell);
			}
		}
		bool Validator(LocalTargetInfo t)
		{
			if (!t.IsValid || !t.Cell.Standable(Find.CurrentMap))
			{
				return false;
			}
			return validComps.Any((CompAutoBlink c) => c.CanBlinkToCell(t.Cell));
		}
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		_drawAsCooldown = CooldownTicksRemaining > 0;
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		GizmoResult result = GizmoOnGUIInt(rect, parms);
		Rect rect2 = new Rect(rect.x + rect.width - 24f, rect.y, 24f, 24f);
		if (isActive() && !disabled)
		{
			GUI.DrawTexture(rect2, (Texture)AutoToggleOnTex);
		}
		else
		{
			GUI.DrawTexture(rect2, (Texture)Widgets.CheckboxOffTex);
		}
		int cooldownTicksRemaining = CooldownTicksRemaining;
		if (cooldownTicksRemaining > 0)
		{
			int blinkIntervalTicks = SelectedComps().First().Props.blinkIntervalTicks;
			float value = Mathf.InverseLerp(blinkIntervalTicks, 0f, cooldownTicksRemaining);
			Widgets.FillableBar(rect, Mathf.Clamp01(value), CooldownBarTex, null, doBorder: false);
			Text.Font = GameFont.Tiny;
			string text = cooldownTicksRemaining.ToStringTicksToPeriod();
			Vector2 vector = Text.CalcSize(text);
			Rect rect3 = new Rect(rect.x + (rect.width - vector.x) / 2f, rect.y, vector.x, vector.y);
			Rect rect4 = rect3.ExpandedBy(8f, 0f);
			Text.Anchor = TextAnchor.UpperCenter;
			GUI.DrawTexture(rect4, (Texture)TexUI.GrayTextBG);
			Widgets.Label(rect3, text);
			Text.Anchor = TextAnchor.UpperLeft;
		}
		return result;
	}

	public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
	{
		Texture tex = icon ?? BaseContent.BadTex;
		Material mat = (_drawAsCooldown ? TexUI.GrayscaleGUI : buttonMat);
		rect.position += new Vector2(iconOffset.x * rect.size.x, iconOffset.y * rect.size.y);
		if (_drawAsCooldown)
		{
			GUI.color = IconDrawColor.SaturationChanged(0f);
		}
		else
		{
			GUI.color = IconDrawColor;
		}
		if (parms.lowLight)
		{
			GUI.color = GUI.color.ToTransparent(0.6f);
		}
		Widgets.DrawTextureFitted(rect, tex, iconDrawScale * 0.85f, iconProportions, iconTexCoords, iconAngle, mat);
		GUI.color = Color.white;
	}
}
