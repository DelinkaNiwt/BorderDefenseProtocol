using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.QuestGen;
using Verse.AI.Group;

namespace GD3
{
	public class QuestNode_UnknownHiveQuest : QuestNode
	{
		public SlateRef<List<ThingDef>> hyperLinks;

		protected override bool TestRunInt(Slate slate)
		{
			if (GDSettings.DeveloperMode)
			{
				return true;
			}
			if (Find.World.GetComponent<MissionComponent>().scriptEnded)
			{
				return false;
			}
			if (GDUtility.QuestExist(GDDefOf.GD_Quest_SendCorpse, GDUtility.QuestStatesSuccess))
			{
				return true;
			}
			return false;
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
					(Reward)new Reward_Unknown(),
				}
			};
			questPart_Choice.choices.Add(choice);
			List<ThingDef> defs = hyperLinks.GetValue(slate);
			if (!defs.NullOrEmpty())
            {
				QuestPart_Hyperlinks questPart_Hyperlinks = new QuestPart_Hyperlinks();
				questPart_Hyperlinks.thingDefs = defs;
				quest.AddPart(questPart_Hyperlinks);
            }
		}
	}
}
