using System;
using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded.UI;

[StaticConstructorOnStartup]
public class PsychicStatusGizmo : Gizmo
{
	private static readonly Color PainBoostColor = new Color(0.2f, 0.65f, 0.35f);

	private static readonly Texture2D EntropyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.46f, 0.34f, 0.35f));

	private static readonly Texture2D EntropyBarTexAdd = SolidColorMaterials.NewSolidColorTexture(new Color(0.78f, 0.72f, 0.66f));

	private static readonly Texture2D OverLimitBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.75f, 0.2f, 0.15f));

	private static readonly Texture2D PsyfocusBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.42f, 0.43f));

	private static readonly Texture2D PsyfocusBarTexReduce = SolidColorMaterials.NewSolidColorTexture(new Color(0.65f, 0.83f, 0.83f));

	private static readonly Texture2D PsyfocusBarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.43f, 0.54f, 0.55f));

	private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

	private static readonly Texture2D PsyfocusTargetTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.74f, 0.97f, 0.8f));

	private readonly Texture2D LimitedTex;

	private readonly Pawn_PsychicEntropyTracker tracker;

	private readonly Texture2D UnlimitedTex;

	private bool draggingPsyfocusBar;

	private float selectedPsyfocusTarget = -1f;

	public PsychicStatusGizmo(Pawn_PsychicEntropyTracker tracker)
	{
		this.tracker = tracker;
		Order = -100f;
		LimitedTex = ContentFinder<Texture2D>.Get("UI/Icons/EntropyLimit/Limited");
		UnlimitedTex = ContentFinder<Texture2D>.Get("UI/Icons/EntropyLimit/Unlimited");
	}

	private static void DrawThreshold(Rect rect, float percent, float entropyValue)
	{
		Rect rect2 = new Rect
		{
			x = rect.x + 3f + (rect.width - 8f) * percent,
			y = rect.y + rect.height - 9f,
			width = 2f,
			height = 6f
		};
		if (entropyValue < percent)
		{
			GUI.DrawTexture(rect2, (Texture)BaseContent.GreyTex);
		}
		else
		{
			GUI.DrawTexture(rect2, (Texture)BaseContent.BlackTex);
		}
	}

	private static void DrawPsyfocusTarget(Rect rect, float percent)
	{
		float num = Mathf.Round((rect.width - 8f) * percent);
		GUI.DrawTexture(new Rect
		{
			x = rect.x + 3f + num,
			y = rect.y,
			width = 2f,
			height = rect.height
		}, (Texture)PsyfocusTargetTex);
		float num2 = UIScaling.AdjustCoordToUIScalingFloor(rect.x + 2f + num);
		float xMax = UIScaling.AdjustCoordToUIScalingCeil(num2 + 4f);
		Rect obj = new Rect
		{
			y = rect.y - 3f,
			height = 5f,
			xMin = num2,
			xMax = xMax
		};
		GUI.DrawTexture(obj, (Texture)PsyfocusTargetTex);
		Rect rect2 = obj;
		rect2.y = rect.yMax - 2f;
		GUI.DrawTexture(rect2, (Texture)PsyfocusTargetTex);
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		//IL_05b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0601: Invalid comparison between Unknown and I4
		//IL_064b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0651: Invalid comparison between Unknown and I4
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Rect rect2 = rect.ContractedBy(6f);
		float num = Mathf.Repeat(Time.time, 0.85f);
		Gizmo lastMouseOverGizmo = MapGizmoUtility.LastMouseOverGizmo;
		AbilityExtension_Psycast abilityExtension_Psycast = ((Def)(object)((Command_Ability)(((lastMouseOverGizmo is Command_Ability) ? lastMouseOverGizmo : null)?)).ability?.def)?.GetModExtension<AbilityExtension_Psycast>();
		float num2 = ((num < 0.1f) ? (num / 0.1f) : ((!(num >= 0.25f)) ? 1f : (1f - (num - 0.25f) / 0.6f)));
		float num3 = num2;
		Widgets.DrawWindowBackground(rect);
		Text.Font = GameFont.Small;
		Rect rect3 = rect2;
		rect3.y += 6f;
		rect3.height = Text.LineHeight;
		Widgets.Label(rect3, "PsychicEntropyShort".Translate());
		Rect rect4 = rect2;
		rect4.y += 38f;
		rect4.height = Text.LineHeight;
		Widgets.Label(rect4, "PsyfocusLabelGizmo".Translate());
		Rect rect5 = rect2;
		rect5.x += 63f;
		rect5.y += 6f;
		rect5.width = 100f;
		rect5.height = 22f;
		float entropyRelativeValue = tracker.EntropyRelativeValue;
		Widgets.FillableBar(rect5, Mathf.Min(entropyRelativeValue, 1f), EntropyBarTex, EmptyBarTex, doBorder: true);
		if (tracker.EntropyValue > tracker.MaxEntropy)
		{
			Widgets.FillableBar(rect5, Mathf.Min(entropyRelativeValue - 1f, 1f), OverLimitBarTex, EntropyBarTex, doBorder: true);
		}
		if (abilityExtension_Psycast != null)
		{
			float entropyUsedByPawn = abilityExtension_Psycast.GetEntropyUsedByPawn(tracker.Pawn);
			if (entropyUsedByPawn > float.Epsilon)
			{
				Rect rect6 = rect5.ContractedBy(3f);
				float width = rect6.width;
				float num4 = tracker.EntropyToRelativeValue(tracker.EntropyValue + entropyUsedByPawn);
				float num5 = entropyRelativeValue;
				if (num5 > 1f)
				{
					num5 -= 1f;
					num4 -= 1f;
				}
				rect6.xMin = UIScaling.AdjustCoordToUIScalingFloor(rect6.xMin + num5 * width);
				rect6.width = UIScaling.AdjustCoordToUIScalingFloor(Mathf.Max(Mathf.Min(num4, 1f) - num5, 0f) * width);
				GUI.color = new Color(1f, 1f, 1f, num3 * 0.7f);
				GenUI.DrawTextureWithMaterial(rect6, EntropyBarTexAdd, null);
				GUI.color = Color.white;
			}
		}
		if (tracker.EntropyValue > tracker.MaxEntropy)
		{
			foreach (KeyValuePair<PsychicEntropySeverity, float> entropyThreshold in Pawn_PsychicEntropyTracker.EntropyThresholds)
			{
				if (entropyThreshold.Value > 1f && entropyThreshold.Value < 2f)
				{
					DrawThreshold(rect5, entropyThreshold.Value - 1f, entropyRelativeValue);
				}
			}
		}
		string label = tracker.EntropyValue.ToString("F0") + " / " + tracker.MaxEntropy.ToString("F0");
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect5, label);
		Text.Anchor = TextAnchor.UpperLeft;
		Text.Font = GameFont.Tiny;
		GUI.color = Color.white;
		Rect rect7 = rect2;
		rect7.width = 175f;
		rect7.height = 38f;
		TooltipHandler.TipRegion(rect7, delegate
		{
			float f = tracker.EntropyValue / tracker.RecoveryRate;
			return string.Format("PawnTooltipPsychicEntropyStats".Translate(), Mathf.Round(tracker.EntropyValue), Mathf.Round(tracker.MaxEntropy), tracker.RecoveryRate.ToString("0.#"), Mathf.Round(f)) + "\n\n" + "PawnTooltipPsychicEntropyDesc".Translate();
		}, Gen.HashCombineInt(tracker.GetHashCode(), 133858));
		Rect rect8 = rect2;
		rect8.x += 63f;
		rect8.y += 38f;
		rect8.width = 100f;
		rect8.height = 22f;
		bool flag = Mouse.IsOver(rect8);
		Widgets.FillableBar(rect8, Mathf.Min(tracker.CurrentPsyfocus, 1f), flag ? PsyfocusBarHighlightTex : PsyfocusBarTex, EmptyBarTex, doBorder: true);
		if (abilityExtension_Psycast != null)
		{
			float psyfocusUsedByPawn = abilityExtension_Psycast.GetPsyfocusUsedByPawn(tracker.Pawn);
			if (psyfocusUsedByPawn > float.Epsilon)
			{
				Rect rect9 = rect8.ContractedBy(3f);
				float num6 = Mathf.Max(tracker.CurrentPsyfocus - psyfocusUsedByPawn, 0f);
				float width2 = rect9.width;
				rect9.xMin = UIScaling.AdjustCoordToUIScalingFloor(rect9.xMin + num6 * width2);
				rect9.width = UIScaling.AdjustCoordToUIScalingCeil((tracker.CurrentPsyfocus - num6) * width2);
				GUI.color = new Color(1f, 1f, 1f, num3);
				GenUI.DrawTextureWithMaterial(rect9, PsyfocusBarTexReduce, null);
				GUI.color = Color.white;
			}
		}
		for (int num7 = 1; num7 < Pawn_PsychicEntropyTracker.PsyfocusBandPercentages.Count - 1; num7++)
		{
			DrawThreshold(rect8, Pawn_PsychicEntropyTracker.PsyfocusBandPercentages[num7], tracker.CurrentPsyfocus);
		}
		float num8 = Mathf.Clamp(Mathf.Round((Event.current.mousePosition.x - (rect8.x + 3f)) / (rect8.width - 8f) * 16f) / 16f, 0f, 1f);
		Event current2 = Event.current;
		if ((int)current2.type == 0 && current2.button == 0 && flag)
		{
			selectedPsyfocusTarget = num8;
			draggingPsyfocusBar = true;
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.MeditationDesiredPsyfocus, KnowledgeAmount.Total);
			SoundDefOf.DragSlider.PlayOneShotOnCamera();
			current2.Use();
		}
		if ((int)current2.type == 3 && current2.button == 0 && draggingPsyfocusBar && flag)
		{
			if (Math.Abs(num8 - selectedPsyfocusTarget) > float.Epsilon)
			{
				SoundDefOf.DragSlider.PlayOneShotOnCamera();
			}
			selectedPsyfocusTarget = num8;
			current2.Use();
		}
		if ((int)current2.type == 1 && current2.button == 0 && draggingPsyfocusBar)
		{
			if (selectedPsyfocusTarget >= 0f)
			{
				tracker.SetPsyfocusTarget(selectedPsyfocusTarget);
			}
			selectedPsyfocusTarget = -1f;
			draggingPsyfocusBar = false;
			current2.Use();
		}
		UIHighlighter.HighlightOpportunity(rect8, "PsyfocusBar");
		DrawPsyfocusTarget(rect8, draggingPsyfocusBar ? selectedPsyfocusTarget : tracker.TargetPsyfocus);
		GUI.color = Color.white;
		Rect rect10 = rect2;
		rect10.y += 38f;
		rect10.width = 175f;
		rect10.height = 38f;
		TooltipHandler.TipRegion(rect10, () => tracker.PsyfocusTipString(selectedPsyfocusTarget), Gen.HashCombineInt(tracker.GetHashCode(), 133873));
		if (tracker.Pawn.IsColonistPlayerControlled)
		{
			Rect rect11 = new Rect(rect2.x + (rect2.width - 32f), rect2.y + (rect2.height / 2f - 32f + 4f), 32f, 32f);
			if (Widgets.ButtonImage(rect11, tracker.limitEntropyAmount ? LimitedTex : UnlimitedTex))
			{
				tracker.limitEntropyAmount = !tracker.limitEntropyAmount;
				if (tracker.limitEntropyAmount)
				{
					SoundDefOf.Tick_Low.PlayOneShotOnCamera();
				}
				else
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera();
				}
			}
			TooltipHandler.TipRegionByKey(rect11, "PawnTooltipPsychicEntropyLimit");
		}
		if (TryGetPainMultiplier(tracker.Pawn, out var painMultiplier))
		{
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			string recoveryBonus = (painMultiplier - 1f).ToStringPercent("F0");
			float widthCached = recoveryBonus.GetWidthCached();
			Rect rect12 = rect2;
			rect12.x += rect2.width - widthCached / 2f - 16f;
			rect12.y += 38f;
			rect12.width = widthCached;
			rect12.height = Text.LineHeight;
			GUI.color = PainBoostColor;
			Widgets.Label(rect12, recoveryBonus);
			GUI.color = Color.white;
			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.UpperLeft;
			TooltipHandler.TipRegion(rect12.ContractedBy(-1f), () => "PawnTooltipPsychicEntropyPainFocus".Translate(tracker.Pawn.health.hediffSet.PainTotal.ToStringPercent("F0"), recoveryBonus), Gen.HashCombineInt(tracker.GetHashCode(), 133878));
		}
		return new GizmoResult(GizmoState.Clear);
	}

	private static bool TryGetPainMultiplier(Pawn pawn, out float painMultiplier)
	{
		List<StatPart> parts = StatDefOf.PsychicEntropyRecoveryRate.parts;
		for (int i = 0; i < parts.Count; i++)
		{
			if (parts[i] is StatPart_Pain statPart_Pain)
			{
				painMultiplier = statPart_Pain.PainFactor(pawn);
				return true;
			}
		}
		painMultiplier = 0f;
		return false;
	}

	public override float GetWidth(float maxWidth)
	{
		return 212f;
	}
}
