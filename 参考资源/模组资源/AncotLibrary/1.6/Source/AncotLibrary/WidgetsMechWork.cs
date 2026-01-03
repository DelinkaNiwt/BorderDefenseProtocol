using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public static class WidgetsMechWork
{
	public const float WorkBoxSize = 25f;

	public static readonly Texture2D WorkBoxBGTex_Awful = ContentFinder<Texture2D>.Get("UI/Widgets/WorkBoxBG_Awful");

	public static readonly Texture2D WorkBoxBGTex_Bad = ContentFinder<Texture2D>.Get("UI/Widgets/WorkBoxBG_Bad");

	public static readonly Texture2D WorkBoxBGTex_Mech = ContentFinder<Texture2D>.Get("AncotLibrary/UI/WorkBoxBG_Mech");

	public static readonly Texture2D WorkBoxBGTex_Excellent = ContentFinder<Texture2D>.Get("UI/Widgets/WorkBoxBG_Excellent");

	public static readonly Texture2D WorkBoxCheckTex = ContentFinder<Texture2D>.Get("UI/Widgets/WorkBoxCheck");

	public static readonly Texture2D PassionWorkboxMinorIcon = ContentFinder<Texture2D>.Get("UI/Icons/PassionMinorGray");

	public static readonly Texture2D PassionWorkboxMajorIcon = ContentFinder<Texture2D>.Get("UI/Icons/PassionMajorGray");

	public static readonly Texture2D WorkBoxOverlay_Warning = ContentFinder<Texture2D>.Get("UI/Widgets/WorkBoxOverlay_Warning");

	public static readonly Texture2D WorkBoxOverlay_PreceptWarning = ContentFinder<Texture2D>.Get("UI/Widgets/WorkBoxOverlay_PreceptWarning");

	public static Color ColorOfPriority(int prio)
	{
		return prio switch
		{
			1 => new Color(0f, 1f, 0f), 
			2 => new Color(1f, 0.9f, 0.5f), 
			3 => new Color(0.8f, 0.7f, 0.5f), 
			4 => new Color(0.74f, 0.74f, 0.74f), 
			_ => Color.grey, 
		};
	}

	public static void DrawWorkBoxFor(float x, float y, Pawn p, WorkTypeDef wType, bool incapableBecauseOfCapacities)
	{
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		if (p.WorkTypeIsDisabled(wType))
		{
			return;
		}
		Rect rect = new Rect(x, y, 25f, 25f);
		if (incapableBecauseOfCapacities)
		{
			GUI.color = new Color(1f, 0.3f, 0.3f);
		}
		DrawWorkBoxBackground(rect, p, wType);
		GUI.color = Color.white;
		if (Find.PlaySettings.useWorkPriorities)
		{
			int priority = p.workSettings.GetPriority(wType);
			if (priority > 0)
			{
				Text.Anchor = TextAnchor.MiddleCenter;
				GUI.color = ColorOfPriority(priority);
				Widgets.Label(rect.ContractedBy(-3f), priority.ToStringCached());
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperLeft;
			}
			if ((int)Event.current.type != 0 || !Mouse.IsOver(rect))
			{
				return;
			}
			bool flag = p.workSettings.WorkIsActive(wType);
			if (Event.current.button == 0)
			{
				int num = p.workSettings.GetPriority(wType) - 1;
				if (num < 0)
				{
					num = 4;
				}
				p.workSettings.SetPriority(wType, num);
				SoundDefOf.DragSlider.PlayOneShotOnCamera();
			}
			if (Event.current.button == 1)
			{
				int num2 = p.workSettings.GetPriority(wType) + 1;
				if (num2 > 4)
				{
					num2 = 0;
				}
				p.workSettings.SetPriority(wType, num2);
				SoundDefOf.DragSlider.PlayOneShotOnCamera();
			}
			if (!flag && p.workSettings.WorkIsActive(wType) && p.Ideo != null && p.Ideo.IsWorkTypeConsideredDangerous(wType))
			{
				Messages.Message("MessageIdeoOpposedWorkTypeSelected".Translate(p, wType.gerundLabel), p, MessageTypeDefOf.CautionInput, historical: false);
				SoundDefOf.DislikedWorkTypeActivated.PlayOneShotOnCamera();
			}
			Event.current.Use();
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.WorkTab, KnowledgeAmount.SpecificInteraction);
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.ManualWorkPriorities, KnowledgeAmount.SmallInteraction);
			return;
		}
		if (p.workSettings.GetPriority(wType) > 0)
		{
			GUI.DrawTexture(rect, (Texture)WorkBoxCheckTex);
		}
		if (!Widgets.ButtonInvisible(rect))
		{
			return;
		}
		if (p.workSettings.GetPriority(wType) > 0)
		{
			p.workSettings.SetPriority(wType, 0);
			SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
		}
		else
		{
			p.workSettings.SetPriority(wType, 3);
			SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
			if (p.Ideo != null && p.Ideo.IsWorkTypeConsideredDangerous(wType))
			{
				Messages.Message("MessageIdeoOpposedWorkTypeSelected".Translate(p, wType.gerundLabel), p, MessageTypeDefOf.CautionInput, historical: false);
				SoundDefOf.DislikedWorkTypeActivated.PlayOneShotOnCamera();
			}
		}
		PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.WorkTab, KnowledgeAmount.SpecificInteraction);
	}

	public static string TipForPawnWorker(Pawn p, WorkTypeDef wDef, bool incapableBecauseOfCapacities)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text = wDef.gerundLabel.CapitalizeFirst().Colorize(ColoredText.TipSectionTitleColor);
		int priority = p.workSettings.GetPriority(wDef);
		text = text + ": " + ((string)("Priority" + priority).Translate()).Colorize(ColorOfPriority(priority));
		stringBuilder.AppendLine(text);
		if (p.WorkTypeIsDisabled(wDef))
		{
			string value = "CannotDoThisWork".Translate(p.LabelShort, p);
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}

	private static void DrawWorkBoxBackground(Rect rect, Pawn p, WorkTypeDef workDef)
	{
		Texture2D workBoxBGTex_Mech = WorkBoxBGTex_Mech;
		GUI.DrawTexture(rect, (Texture)workBoxBGTex_Mech);
	}
}
