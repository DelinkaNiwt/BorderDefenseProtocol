using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;

namespace GD3
{
	public class TradeWindow_BlackMech : Window
	{
		private float windowWidth = 0f;
		private float windowHeight = 0f;

		private string[] options = { Find.World.GetComponent<MissionComponent>().scriptEnded ? "GD.BlackMechScriptTreeEnd".Translate() : "GD.BlackMechScriptTree".Translate() , "GD.Back".Translate(), "CloseButton".Translate() };
		private int selectedOption = -1;

		public override Vector2 InitialSize => new Vector2(windowWidth, windowHeight);

		private Vector2 scrollPosition = Vector2.zero;

		public TradeWindow_BlackMech(string title, string description, Map map, Pawn pawn)
		{
			this.title = title;
			this.description = description;
			forcePause = true;
			absorbInputAroundWindow = true;
			closeOnAccept = false;
			closeOnCancel = false;
			soundAppear = SoundDefOf.CommsWindow_Open;
			soundClose = SoundDefOf.CommsWindow_Close;
			//windowWidth = Mathf.Max(300f, Text.CalcSize(title).x + 20f);
			//windowHeight = 150f + Text.CalcHeight(description, windowWidth - 20f) + CloseButSize.y;
			windowWidth = 520f;
			windowHeight = 570f;
			this.map = map;
			this.pawn = pawn;
		}

		public override void DoWindowContents(Rect inRect)
		{
			Rect contentRect = inRect.ContractedBy(10f);
			
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 40f), title);
			
			Text.Font = GameFont.Small;
			Widgets.Label(new Rect(contentRect.x, contentRect.y + Text.LineHeight + 5f, contentRect.width, Text.CalcHeight(description, contentRect.width)), description);

			float recty2 = contentRect.y + Text.LineHeight + Text.CalcHeight(description, contentRect.width) + 5f;

			if (Find.World.GetComponent<MissionComponent>().script_Finished.Contains(300))
            {
				string description2 = "GD.BlackMechSellInt".Translate(Find.World.GetComponent<MissionComponent>().intelligenceAdvanced);
				Widgets.Label(new Rect(contentRect.x, recty2 + 5f, contentRect.width, Text.CalcHeight(description2, contentRect.width)), description2);
				float num2 = recty2 + 5f;
				float num3 = num2 + Text.CalcHeight(description2, contentRect.width) + 5f;
				Widgets.TextFieldNumeric(new Rect(contentRect.x, num3, contentRect.width, Text.LineHeight), ref inputPri, ref inputPriStr);

				List<BlackMechTradeDef> defs = DefDatabase<BlackMechTradeDef>.AllDefsListForReading;
				defs.SortBy(def => def.order);
				float num4 = num3 + Text.LineHeight + 5f;
				Rect outRect = new Rect(contentRect.x, num4, contentRect.width, contentRect.height - num4 - (15f + 2 * Text.LineHeight));
				Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, defs.Count * 5f + defs.Sum(def => def.rectHeight));
				Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

				Rect buttonRect = new Rect(contentRect.x, 0, contentRect.width - 16, 25f);
				for (int i = 0; i < defs.Count; i++)
                {
					BlackMechTradeDef def = defs[i];
					float buttonHeight = Math.Max(def.rectHeight, Text.LineHeight * 2);
					buttonRect.height = buttonHeight;
					if (Widgets.ButtonText(buttonRect, def.label))
					{
						int num = (int)inputPri;
						if (num < def.intelligenceCost || Find.World.GetComponent<MissionComponent>().intelligenceAdvanced < num)
						{
							Messages.Message("GD.IntNotEnough".Translate(), MessageTypeDefOf.NeutralEvent);
						}
						else
						{
							int time = Mathf.FloorToInt(num / def.intelligenceCost);
							Find.World.GetComponent<MissionComponent>().intelligenceAdvanced -= time * def.intelligenceCost;
							for (int j = 0; j < time; j++)
                            {
								def.Worker.Run(pawn, map);
							}
						}
					}
					buttonRect.y += buttonRect.height + 5f;
				}

				Widgets.EndScrollView();
			}

			for (int i = 0; i < options.Length; i++)
			{
				Rect optionRect = new Rect(contentRect.x, inRect.height - 25f - (options.Length + 1) * Text.LineHeight + (i + 2) * Text.LineHeight, contentRect.width, Text.LineHeight);

				Widgets.DrawHighlightIfMouseover(optionRect);
				Widgets.Label(optionRect, options[i]);

				if (Widgets.ButtonInvisible(optionRect))
				{
					selectedOption = i;
					if (selectedOption == 0)
						this.Option2();
					else if (selectedOption == 1)
						this.Option1();
					else if (selectedOption == 2)
						this.Close();
				}
			}
		}

		public void Option1()
		{
			this.Close();
			Find.WindowStack.Add(new MissionWindow("GD.StationTitle".Translate(), "GD.StationDescription".Translate(Find.World.GetComponent<MissionComponent>().intelligencePrimary, Find.World.GetComponent<MissionComponent>().intelligenceAdvanced), map, pawn));
		}

		public void Option2()
        {
			MechanoidScriptDef tree = Find.World.GetComponent<MissionComponent>().Script;
			if (tree == null)
			{
				return;
			}

			if ((tree.questNeedToFinish != null && !Find.QuestManager.QuestsListForReading.Any((Quest q) => q.root == tree.questNeedToFinish)))
			{
				IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.GiveQuest, Find.World);
				if (!tree.questNeedToFinish.CanRun(parms.points, parms.target))
				{
					Messages.Message("GD.CantGenerateQuest".Translate(), MessageTypeDefOf.RejectInput);
					return;
				}
				QuestUtility.SendLetterQuestAvailable(QuestUtility.GenerateQuestAndMakeAvailable(tree.questNeedToFinish, new IncidentParms
				{
					target = map,
					points = StorytellerUtility.DefaultThreatPointsNow(map)
				}.points));
				return;
			}
			else if (Find.QuestManager.QuestsListForReading.Any((Quest q) => q.root == tree.questNeedToFinish && q.TicksSinceCleanup == -1))
            {
				Messages.Message("GD.QuestNotFinish".Translate(), MessageTypeDefOf.NeutralEvent);
				return;
			}

			if (Find.QuestManager.QuestsListForReading.Any((Quest q) => q.root == tree.questNeedToFinish) && !Find.QuestManager.QuestsListForReading.Any((Quest q) => q.root == tree.questNeedToFinish && q.State == QuestState.EndedSuccess))
			{
				this.Close();
				ScriptTree s0 = tree.failed;
				Find.WindowStack.Add(new CommunicationWindow_BlackMech(s0.title, s0.dialogue, s0.graphic, s0.drawSize, s0.drawOffset, map, pawn, new List<ScriptButton>() { MakeNewButton("Back", "GD.Back", tree.questNeedToFinish), MakeNewButton("End", "CloseButton", tree.questNeedToFinish) }, null, 0));
				return;
			}

			if (!Find.World.GetComponent<MissionComponent>().script_Allowed.Contains(tree.ID))
			{
				this.Close();
				Find.WindowStack.Add(new CommunicationWindow_BlackMech("GD.NeedPayTitle".Translate(), "GD.NeedPayDesc".Translate(TakeIntKind(out int num), tree.priceNum, num), null, 1f, 0f, map, pawn, new List<ScriptButton>() { MakeNewButton("Pay", "GD.NeedPayButton"), MakeNewButton("Back", "GD.Back"), MakeNewButton("End", "CloseButton") }, null, 0));
				return;
			}

			this.Close();
			List<ScriptTree> scriptTree = tree.scriptTree;
			ScriptTree s;
			int i;
			if (tree.branch != null && Find.World.GetComponent<MissionComponent>().BranchDict.TryGetValue(tree.branch, true))
            {
				s = scriptTree[tree.to];
				i = tree.to;
            }
            else
            {
				s = scriptTree[0];
				i = 0;
			}
			Find.WindowStack.Add(new CommunicationWindow_BlackMech(s.title, s.dialogue, s.graphic, s.drawSize, s.drawOffset, map, pawn, s.buttons, scriptTree, i));
			Find.World.GetComponent<MissionComponent>().script_Finished.Add(tree.ID);
		}

		private string TakeIntKind(out int num)
		{
			num = 0;
			if (Find.World.GetComponent<MissionComponent>().Script.priceKind == "Primary")
			{
				num = Find.World.GetComponent<MissionComponent>().intelligencePrimary;
				return "GD.PrimaryInt".Translate();
			}
			else if (Find.World.GetComponent<MissionComponent>().Script.priceKind == "Advanced")
			{
				num = Find.World.GetComponent<MissionComponent>().intelligenceAdvanced;
				return "GD.PrimaryAdv".Translate();
			}
			return "None";
		}

		private ScriptButton MakeNewButton(string action, string text, QuestScriptDef quest = null)
		{
			ScriptButton scriptButton = new ScriptButton();
			scriptButton.action = action;
			scriptButton.text = text;
			scriptButton.quest = quest;
			return scriptButton;
		}

		private float inputPri = 0;

		private string inputPriStr;

		private readonly string title;

		private readonly string description;

		private Map map;

		private Pawn pawn;

		//public override Vector2 InitialSize => new Vector2(620f, 700f);
	}
}