using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Milira;

public class QuestNode_ChurchReward_Missionary : QuestNode
{
	public SlateRef<string> letterLabel;

	public SlateRef<string> letterText;

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}

	protected override void RunInt()
	{
		Quest quest = QuestGen.quest;
		Slate slate = QuestGen.slate;
		Faction faction = QuestGen.slate.Get<Faction>("askerFaction");
		Reward_RoyalFavor reward_RoyalFavor = new Reward_RoyalFavor();
		reward_RoyalFavor.faction = faction;
		reward_RoyalFavor.amount = 1;
		List<Thing> list = new List<Thing>();
		ThingDef milira_InformationLetterI_ChurchIntrodction = MiliraDefOf.Milira_InformationLetterI_ChurchIntrodction;
		Thing thing = ThingMaker.MakeThing(milira_InformationLetterI_ChurchIntrodction);
		thing.stackCount = 1;
		list.Add(thing);
		ThingDef milira_InformationLetterI_Milira = MiliraDefOf.Milira_InformationLetterI_Milira;
		Thing thing2 = ThingMaker.MakeThing(milira_InformationLetterI_Milira);
		thing2.stackCount = 1;
		list.Add(thing2);
		Reward_Items reward_Items = new Reward_Items();
		reward_Items.items.AddRange(list);
		QuestPart_Choice questPart_Choice = quest.RewardChoice();
		QuestPart_Choice.Choice choice = new QuestPart_Choice.Choice
		{
			rewards = 
			{
				(Reward)reward_RoyalFavor,
				(Reward)reward_Items
			}
		};
		questPart_Choice.choices.Add(choice);
		RewardsGeneratorParams parms = default(RewardsGeneratorParams);
		int num = 0;
		foreach (Reward reward in choice.rewards)
		{
			foreach (QuestPart item in reward.GenerateQuestParts(num, parms, letterLabel.GetValue(slate), letterText.GetValue(slate), null, null))
			{
				QuestGen.quest.AddPart(item);
				choice.questParts.Add(item);
				num++;
			}
		}
	}
}
