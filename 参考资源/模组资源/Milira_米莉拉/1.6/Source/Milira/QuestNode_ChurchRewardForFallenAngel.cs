using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Milira;

public class QuestNode_ChurchRewardForFallenAngel : QuestNode
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
		Reward_Goodwill reward_Goodwill = new Reward_Goodwill();
		Faction faction = QuestGen.slate.Get<Faction>("askerFaction");
		reward_Goodwill.faction = faction;
		reward_Goodwill.amount = 20;
		List<Thing> list = new List<Thing>();
		ThingDef apparel_PsychicShockLance = MiliraDefOf.Apparel_PsychicShockLance;
		Thing thing = ThingMaker.MakeThing(apparel_PsychicShockLance);
		thing.stackCount = 2;
		list.Add(thing);
		ThingDef milira_SolarCrystal = MiliraDefOf.Milira_SolarCrystal;
		Thing thing2 = ThingMaker.MakeThing(milira_SolarCrystal);
		thing2.stackCount = 25;
		list.Add(thing2);
		ThingDef psychicAmplifier = MiliraDefOf.PsychicAmplifier;
		Thing thing3 = ThingMaker.MakeThing(psychicAmplifier);
		thing3.stackCount = 1;
		list.Add(thing3);
		ThingDef plasteel = MiliraDefOf.Plasteel;
		Thing thing4 = ThingMaker.MakeThing(plasteel);
		thing4.stackCount = 200;
		list.Add(thing4);
		Reward_Items reward_Items = new Reward_Items();
		reward_Items.items.AddRange(list);
		QuestPart_Choice questPart_Choice = quest.RewardChoice();
		QuestPart_Choice.Choice choice = new QuestPart_Choice.Choice
		{
			rewards = 
			{
				(Reward)reward_Goodwill,
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
