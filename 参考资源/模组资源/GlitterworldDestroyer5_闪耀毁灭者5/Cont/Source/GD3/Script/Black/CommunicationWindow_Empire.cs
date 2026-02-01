using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;

namespace GD3
{
	public class CommunicationWindow_Empire : Window
	{
		private float windowWidth = 0f;
		private float windowHeight = 0f;

		private string[] options = { "GD.Back".Translate() , "CloseButton".Translate() };
		private int selectedOption = -1;

		public override Vector2 InitialSize => new Vector2(windowWidth, windowHeight);

		public CommunicationWindow_Empire(string title, string description, Map map, Pawn pawn)
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
			// 绘制标题
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 40f), title);
			// 绘制描述
			Text.Font = GameFont.Small;
			Widgets.Label(new Rect(contentRect.x, contentRect.y + Text.LineHeight + 5f, contentRect.width, Text.CalcHeight(description, contentRect.width)), description);

			float recty2 = contentRect.y + Text.LineHeight + Text.CalcHeight(description, contentRect.width) + 5f;

			string description2 = "GD.EmpireSellInt1".Translate(Find.World.GetComponent<MissionComponent>().intelligencePrimary);
			Widgets.Label(new Rect(contentRect.x, recty2 + 5f, contentRect.width, Text.CalcHeight(description2, contentRect.width)), description2);
			Widgets.TextFieldNumeric(new Rect(contentRect.x, recty2 + 5f + Text.CalcHeight(description2, contentRect.width) + 5f, contentRect.width, Text.LineHeight), ref inputPri, ref inputPriStr);
			if (!Faction.OfPlayer.HostileTo(Find.FactionManager.FirstFactionOfDef(FactionDefOf.Empire)))
            {
				if (Widgets.ButtonText(new Rect(0f, recty2 + 5f + Text.CalcHeight(description2, contentRect.width) + Text.LineHeight + 10f, inRect.width - 5f, 25f), "GD.SellIntButton_1Silver".Translate()))
				{
					int num = (int)inputPri;
					if (num < 1000 || Find.World.GetComponent<MissionComponent>().intelligencePrimary < num)
                    {
						Messages.Message("GD.IntNotEnough".Translate(), MessageTypeDefOf.NeutralEvent);
                    }
                    else
                    {
						int time = Mathf.FloorToInt(num / 1000);
						Find.World.GetComponent<MissionComponent>().intelligencePrimary -= time * 1000;
						int numThing = time * 200;
						Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
						silver.stackCount = numThing;
						TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(map), map, silver);
						SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
					}
				}
				if (Widgets.ButtonText(new Rect(0f, recty2 + 5f + Text.CalcHeight(description2, contentRect.width) + Text.LineHeight + 10f + 30f, inRect.width - 5f, 25f), "GD.SellIntButton_1Royal".Translate()))
				{
					int num = (int)inputPri;
					if (num < 1000 || Find.World.GetComponent<MissionComponent>().intelligencePrimary < num)
					{
						Messages.Message("GD.IntNotEnough".Translate(), MessageTypeDefOf.NeutralEvent);
					}
					else
					{
						int time = Mathf.FloorToInt(num / 1000);
						Find.World.GetComponent<MissionComponent>().intelligencePrimary -= time * 1000;
						int numThing = time * 2;
						pawn.royalty.GainFavor(Find.FactionManager.FirstFactionOfDef(FactionDefOf.Empire), numThing);
						SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
					}
				}
			}
			if (Widgets.ButtonText(new Rect(0f, recty2 + 5f + Text.CalcHeight(description2, contentRect.width) + Text.LineHeight + 10f + 60f, inRect.width - 5f, 25f), "GD.SellIntButton_1Relation".Translate()))
			{
				int num = (int)inputPri;
				if (num < 1000 || Find.World.GetComponent<MissionComponent>().intelligencePrimary < num)
				{
					Messages.Message("GD.IntNotEnough".Translate(), MessageTypeDefOf.NeutralEvent);
				}
				else
				{
					int time = Mathf.FloorToInt(num / 1000);
					Find.World.GetComponent<MissionComponent>().intelligencePrimary -= time * 1000;
					int numThing = time * 8;
					Faction.OfPlayer.TryAffectGoodwillWith(Find.FactionManager.FirstFactionOfDef(FactionDefOf.Empire), numThing, canSendMessage: true, canSendHostilityLetter: true, null);
					SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
				}
			}

			float recty3 = recty2 + 5f + Text.LineHeight + Text.CalcHeight(description2, contentRect.width) + Text.LineHeight + 5f + 10f + 60f;

			string description3 = "GD.EmpireSellInt2".Translate(Find.World.GetComponent<MissionComponent>().intelligenceAdvanced);
			Widgets.Label(new Rect(contentRect.x, recty3 + 5f + 5f, contentRect.width, Text.CalcHeight(description3, contentRect.width)), description3);
			Widgets.TextFieldNumeric(new Rect(contentRect.x, recty3 + 5f + Text.CalcHeight(description3, contentRect.width) + 5f, contentRect.width, Text.LineHeight), ref inputAdv, ref inputAdvStr);
			if (!Faction.OfPlayer.HostileTo(Find.FactionManager.FirstFactionOfDef(FactionDefOf.Empire)))
            {
				if (Widgets.ButtonText(new Rect(0f, recty3 + 5f + Text.CalcHeight(description3, contentRect.width) + Text.LineHeight + 10f, inRect.width - 5f, 25f), "GD.SellIntButton_2Silver".Translate()))
				{
					int num = (int)inputAdv;
					if (num < 1000 || Find.World.GetComponent<MissionComponent>().intelligenceAdvanced < num)
					{
						Messages.Message("GD.IntNotEnough".Translate(), MessageTypeDefOf.NeutralEvent);
					}
					else
					{
						int time = Mathf.FloorToInt(num / 1000);
						Find.World.GetComponent<MissionComponent>().intelligenceAdvanced -= time * 1000;
						int numThing = time * 500;
						Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
						silver.stackCount = numThing;
						TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(map), map, silver);
						SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
					}
				}
				if (Widgets.ButtonText(new Rect(0f, recty3 + 5f + Text.CalcHeight(description3, contentRect.width) + Text.LineHeight + 10f + 30f, inRect.width - 5f, 25f), "GD.SellIntButton_2Royal".Translate()))
				{
					int num = (int)inputAdv;
					if (num < 1000 || Find.World.GetComponent<MissionComponent>().intelligenceAdvanced < num)
					{
						Messages.Message("GD.IntNotEnough".Translate(), MessageTypeDefOf.NeutralEvent);
					}
					else
					{
						int time = Mathf.FloorToInt(num / 1000);
						Find.World.GetComponent<MissionComponent>().intelligenceAdvanced -= time * 1000;
						int numThing = time * 5;
						pawn.royalty.GainFavor(Find.FactionManager.FirstFactionOfDef(FactionDefOf.Empire), numThing);
						SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
					}
				}
			}
			if (Widgets.ButtonText(new Rect(0f, recty3 + 5f + Text.CalcHeight(description3, contentRect.width) + Text.LineHeight + 10f + 60f, inRect.width - 5f, 25f), "GD.SellIntButton_2Relation".Translate()))
			{
				int num = (int)inputAdv;
				if (num < 1000 || Find.World.GetComponent<MissionComponent>().intelligenceAdvanced < num)
				{
					Messages.Message("GD.IntNotEnough".Translate(), MessageTypeDefOf.NeutralEvent);
				}
				else
				{
					int time = Mathf.FloorToInt(num / 1000);
					Find.World.GetComponent<MissionComponent>().intelligenceAdvanced -= time * 1000;
					int numThing = time * 15;
					Faction.OfPlayer.TryAffectGoodwillWith(Find.FactionManager.FirstFactionOfDef(FactionDefOf.Empire), numThing, canSendMessage: true, canSendHostilityLetter: true, null);
					SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
				}
			}

			if (Faction.OfPlayer.HostileTo(Find.FactionManager.FirstFactionOfDef(FactionDefOf.Empire)))
            {
				Pawn pawn = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Empire).leader;
				float recty4 = recty3 + 5f + Text.LineHeight + Text.CalcHeight(description3, contentRect.width) + Text.LineHeight + 5f + 10f + 60f;
				string description4 = "GD.EmpireSellHostile".Translate(pawn.NameShortColored);
				Widgets.Label(new Rect(contentRect.x, recty4 + 5f, contentRect.width, Text.CalcHeight(description4, contentRect.width)), description4);
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
						this.Option1();
					else if (selectedOption == 1)
						this.Close();
				}
			}
		}

		public void Option1()
        {
			this.Close();
			Find.WindowStack.Add(new MissionWindow("GD.StationTitle".Translate(), "GD.StationDescription".Translate(Find.World.GetComponent<MissionComponent>().intelligencePrimary, Find.World.GetComponent<MissionComponent>().intelligenceAdvanced), map, pawn));
		}

		private float inputPri = 0;

		private float inputAdv = 0;

		private string inputPriStr;

		private string inputAdvStr;

		private readonly string title;

		private readonly string description;

		private Map map;

		private Pawn pawn;

		//public override Vector2 InitialSize => new Vector2(620f, 700f);
	}
}