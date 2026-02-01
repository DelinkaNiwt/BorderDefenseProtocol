using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimTalk.Data;
using RimTalk.UI;
using RimTalk.Util;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalk.Patches;

[StaticConstructorOnStartup]
public static class BioTabPersonalityPatch
{
	[HarmonyPatch(typeof(CharacterCardUtility), "DoTopStack")]
	public static class DoTopStack_Patch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo anchorMethod = AccessTools.Method(typeof(QuestUtility), "AppendInspectStringsFromQuestParts", new Type[3]
			{
				typeof(Action<string, Quest>),
				typeof(ISelectable),
				typeof(int).MakeByRefType()
			});
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.Calls(anchorMethod))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BioTabPersonalityPatch), "AddPersonaElement"));
				}
			}
		}
	}

	private static readonly Texture2D RimTalkIcon = ContentFinder<Texture2D>.Get("UI/RimTalkIcon");

	private static void AddPersonaElement(Pawn pawn)
	{
		if (!pawn.IsColonist && !pawn.IsPrisonerOfColony && !pawn.HasVocalLink())
		{
			return;
		}
		List<GenUI.AnonymousStackElement> tmpStackElements = (List<GenUI.AnonymousStackElement>)AccessTools.Field(typeof(CharacterCardUtility), "tmpStackElements").GetValue(null);
		if (tmpStackElements == null)
		{
			return;
		}
		string personaLabelText = "RimTalk.BioTab.RimTalkPersona".Translate();
		float textWidth = Text.CalcSize(personaLabelText).x;
		float totalLabelWidth = 27f + textWidth + 5f;
		tmpStackElements.Add(new GenUI.AnonymousStackElement
		{
			width = totalLabelWidth,
			drawer = delegate(Rect rect)
			{
				Widgets.DrawOptionBackground(rect, selected: false);
				Widgets.DrawHighlightIfMouseover(rect);
				string personality = PersonaService.GetPersonality(pawn);
				float talkInitiationWeight = PersonaService.GetTalkInitiationWeight(pawn);
				string text = string.Format("{0}\n\n{1}\n\n{2} {3:0.00}", "RimTalk.PersonaEditor.Title".Translate(pawn.LabelShort).Colorize(ColoredText.TipSectionTitleColor), personality, "RimTalk.PersonaEditor.Chattiness".Translate().Colorize(ColoredText.TipSectionTitleColor), talkInitiationWeight);
				TooltipHandler.TipRegion(rect, text);
				Rect rect2 = new Rect(rect.x + 2f, rect.y + 1f, 20f, 20f);
				GUI.DrawTexture(rect2, (Texture)RimTalkIcon);
				Rect rect3 = new Rect(rect2.xMax + 5f, rect.y, textWidth, rect.height);
				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(rect3, personaLabelText);
				Text.Anchor = TextAnchor.UpperLeft;
				if (Widgets.ButtonInvisible(rect))
				{
					Find.WindowStack.Add(new PersonaEditorWindow(pawn));
				}
			}
		});
	}
}
