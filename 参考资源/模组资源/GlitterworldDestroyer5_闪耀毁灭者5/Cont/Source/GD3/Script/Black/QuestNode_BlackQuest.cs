using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.QuestGen;
using Verse.AI.Group;

namespace GD3
{
	public class QuestNode_BlackQuest : QuestNode
	{

		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			QuestPart_Choice questPart_Choice = quest.RewardChoice();
			QuestPart_Choice.Choice choice = new QuestPart_Choice.Choice
			{
				rewards =
				{
					(Reward)new Reward_BlackMech(),
					(Reward)new Reward_Intelligence(),
				}
			};
			questPart_Choice.choices.Add(choice);
			Faction faction = Find.FactionManager.FirstFactionOfDef(GDDefOf.BlackMechanoid);
			slate.Set("faction", faction);
			string text = QuestGenUtility.HardcodedSignalWithQuestID("faction.BecameHostileToPlayer");
			quest.End(QuestEndOutcome.Fail, 0, null, text, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true, true);
		}
	}
}
