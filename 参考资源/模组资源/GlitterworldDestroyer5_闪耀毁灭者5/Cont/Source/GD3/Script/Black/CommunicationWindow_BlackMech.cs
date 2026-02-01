using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;

namespace GD3
{
	public class CommunicationWindow_BlackMech : Window
	{
		private float windowWidth = 0f;
		private float windowHeight = 0f;
		private string graphic = null;
		private float drawSize;
		private float drawOffset;

		private List<ScriptButton> options;

		private List<ScriptTree> scriptTree;
		private int index;

		public override Vector2 InitialSize => new Vector2(windowWidth, windowHeight);

		public CommunicationWindow_BlackMech(string title, string description, string graphic, float drawSize, float drawOffset, Map map, Pawn pawn, List<ScriptButton> options, List<ScriptTree> scriptTree, int index)
		{
			this.title = title;
			this.description = description;
			this.graphic = graphic;
			this.drawSize = drawSize;
			this.drawOffset = drawOffset;
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
			this.options = options;
			this.scriptTree = scriptTree;
			this.index = index;
		}

        public override void PreOpen()
        {
            base.PreOpen();
			if (graphic != null)
            {
				Find.WindowStack.Add(new GraphicWindow(graphic, drawSize, drawOffset, windowWidth, windowHeight));
            }
        }

        public override void DoWindowContents(Rect inRect)
		{
			Rect contentRect = inRect.ContractedBy(10f);
			
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 40f), title);
			
			Text.Font = GameFont.Small;
			Widgets.Label(new Rect(contentRect.x, contentRect.y + Text.LineHeight + 5f, contentRect.width, Text.CalcHeight(description, contentRect.width)), description.Translate(pawn.NameShortColored));

			for (int i = 0; i < options.Count; i++)
			{
				Rect optionRect = new Rect(contentRect.x, inRect.height - 25f - (options.Count + 1) * Text.LineHeight + (i + 2) * Text.LineHeight, contentRect.width, Text.LineHeight);

				Widgets.DrawHighlightIfMouseover(optionRect);
				Widgets.Label(optionRect, options[i].text.Translate());

				if (Widgets.ButtonInvisible(optionRect))
				{
					if (options[i].action == "End")
                    {
						this.CloseAll();
						if (options[i].quest != null)
                        {
							this.GenerateQuest(map, options[i].quest);
                        }
						if (scriptTree[0].Parent.ID == 1000 && Find.World.GetComponent<MissionComponent>().BranchDict != null && !Find.World.GetComponent<MissionComponent>().BranchDict.TryGetValue("WillMilitorDie", true))
                        {
							Thing thing = ThingMaker.MakeThing(GDDefOf.GD_MilitorDoll);
							thing.SetFaction(Faction.OfPlayer);
							IntVec3 pos = DropCellFinder.TradeDropSpot(map);
							TradeUtility.SpawnDropPod(pos, map, thing);
							Messages.Message("GD.DollReward".Translate(), new LookTargets(pos, map), MessageTypeDefOf.PositiveEvent);
						}
						if (scriptTree[0].Parent.ID == 1200)
						{
							Find.World.GetComponent<MissionComponent>().scriptEnded = true;

							Thing thing = ThingMaker.MakeThing(GDDefOf.BlackNanoChip);
							IntVec3 pos = DropCellFinder.TradeDropSpot(map);
							TradeUtility.SpawnDropPod(pos, map, thing);
							Messages.Message("GD.ChipReward".Translate(), new LookTargets(pos, map), MessageTypeDefOf.PositiveEvent);
							Find.LetterStack.ReceiveLetter("GD.ScriptEnd".Translate(), "GD.ScriptEndDesc".Translate(), LetterDefOf.PositiveEvent);
							Find.World.GetComponent<MainComponent>().list_str.Add("ScriptFinished");
						}
					}
					else if (options[i].action == "ContinueWithBranch")
					{
						this.CloseAll();
						if (options[i].quest != null)
						{
							this.GenerateQuest(map, options[i].quest);
						}
						if (scriptTree == null)
						{
							return;
						}
						if (options[i].jumpTo != -1)
						{
							ScriptTree s = scriptTree[options[i].jumpTo];
							if (s != null)
							{
								Find.WindowStack.Add(new CommunicationWindow_BlackMech(s.title, s.dialogue, s.graphic, s.drawSize, s.drawOffset, map, pawn, s.buttons, scriptTree, options[i].jumpTo));
							}
						}
						else
						{
							ScriptTree s = scriptTree[index + 1];
							if (s != null)
							{
								Find.WindowStack.Add(new CommunicationWindow_BlackMech(s.title, s.dialogue, s.graphic, s.drawSize, s.drawOffset, map, pawn, s.buttons, scriptTree, index + 1));
							}
						}
						if (GDSettings.DeveloperMode)
                        {
							Log.Warning((Find.World.GetComponent<MissionComponent>().BranchDict != null).ToString());
                        }
						if (Find.World.GetComponent<MissionComponent>().BranchDict == null)
                        {
							Find.World.GetComponent<MissionComponent>().BranchDict = new Dictionary<string, bool>();
						}
						Find.World.GetComponent<MissionComponent>().BranchDict.Add(options[i].branch, options[i].to);
					}
					else if (options[i].action == "Back")
					{
						this.CloseAll();
						Find.WindowStack.Add(new MissionWindow("GD.StationTitle".Translate(), "GD.StationDescription".Translate(Find.World.GetComponent<MissionComponent>().intelligencePrimary, Find.World.GetComponent<MissionComponent>().intelligenceAdvanced), map, pawn));
						if (options[i].quest != null)
						{
							this.GenerateQuest(map, options[i].quest);
						}
					}
					else if (options[i].action == "Continue")
					{
						this.CloseAll();
						if (options[i].quest != null)
						{
							this.GenerateQuest(map, options[i].quest);
						}
						if (scriptTree == null)
                        {
							return;
                        }
						if (options[i].jumpTo != -1)
                        {
							ScriptTree s = scriptTree[options[i].jumpTo];
							if (s != null)
                            {
								Find.WindowStack.Add(new CommunicationWindow_BlackMech(s.title, s.dialogue, s.graphic, s.drawSize, s.drawOffset, map, pawn, s.buttons, scriptTree, options[i].jumpTo));
							}
						}
                        else
                        {
							ScriptTree s = scriptTree[index + 1];
							if (s != null)
							{
								Find.WindowStack.Add(new CommunicationWindow_BlackMech(s.title, s.dialogue, s.graphic, s.drawSize, s.drawOffset, map, pawn, s.buttons, scriptTree, index + 1));
							}
						}
					}
					else if (options[i].action == "Pay")
					{
						
						if (IfIntEnough(out string kind))
                        {
							if (kind == "Primary")
                            {
								Find.World.GetComponent<MissionComponent>().intelligencePrimary -= Find.World.GetComponent<MissionComponent>().Script.priceNum;
                            }
							else if (kind == "Advanced")
							{
								Find.World.GetComponent<MissionComponent>().intelligenceAdvanced -= Find.World.GetComponent<MissionComponent>().Script.priceNum;
							}
							Find.World.GetComponent<MissionComponent>().script_Allowed.Add(Find.World.GetComponent<MissionComponent>().Script.ID);
							this.CloseAll();
							Find.WindowStack.Add(new MissionWindow("GD.StationTitle".Translate(), "GD.StationDescription".Translate(Find.World.GetComponent<MissionComponent>().intelligencePrimary, Find.World.GetComponent<MissionComponent>().intelligenceAdvanced), map, pawn));
							Messages.Message("GD.PaymentSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
							SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
						}
                        else
                        {
							Messages.Message("GD.IntNotEnough".Translate(), MessageTypeDefOf.NeutralEvent);
                        }
					}
				}
			}
		}

		public void CloseAll()
        {
			this.Close();
			IList<Window> l = Find.WindowStack.Windows;
			for (int i = 0; i < l.Count; i++)
            {
				Window w = l[i];
				if (w is GraphicWindow)
				{
					w.Close();
				}
			}
        }

		public void GenerateQuest(Map map, QuestScriptDef quest)
        {
			/*IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.GiveQuest, map as IIncidentTarget ?? Find.World);
			if (!quest.CanRun(parms.points, parms.target))
            {
				Messages.Message("GD.CantGenerateQuest".Translate(), MessageTypeDefOf.RejectInput);
				return;
            }*/
			Map mapNew = map ?? Find.RandomSurfacePlayerHomeMap;
			if (mapNew?.Tile.LayerDef?.isSpace ?? false)
            {
				Messages.Message("GD.CantGenerateQuest".Translate(), MessageTypeDefOf.RejectInput);
				return;
			}
			QuestUtility.SendLetterQuestAvailable(QuestUtility.GenerateQuestAndMakeAvailable(quest, new IncidentParms
			{
				target = mapNew,
				points = StorytellerUtility.DefaultThreatPointsNow(mapNew)
			}.points));
		}

		private bool IfIntEnough(out string kind)
        {
			kind = "NotEnough";
			if (Find.World.GetComponent<MissionComponent>().Script.priceKind == "Primary" && Find.World.GetComponent<MissionComponent>().intelligencePrimary >= Find.World.GetComponent<MissionComponent>().Script.priceNum)
            {
				kind = "Primary";
				return true;
            }
			else if (Find.World.GetComponent<MissionComponent>().Script.priceKind == "Advanced" && Find.World.GetComponent<MissionComponent>().intelligenceAdvanced >= Find.World.GetComponent<MissionComponent>().Script.priceNum)
			{
				kind = "Advanced";
				return true;
			}
			return false;
		}

		private readonly string title;

		private readonly string description;

		private Map map;

		private Pawn pawn;

		//public override Vector2 InitialSize => new Vector2(620f, 700f);
	}
}