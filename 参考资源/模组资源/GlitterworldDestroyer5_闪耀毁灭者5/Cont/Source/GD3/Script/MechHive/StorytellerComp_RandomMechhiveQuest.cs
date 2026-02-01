using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace GD3
{
	public class StorytellerCompProperties_RandomMechhiveQuest : StorytellerCompProperties_OnOffCycle
	{
		public List<QuestScriptDef> quests;

		public StorytellerCompProperties_RandomMechhiveQuest()
		{
			compClass = typeof(StorytellerComp_RandomMechhiveQuest);
		}
	}
	public class StorytellerComp_RandomMechhiveQuest : StorytellerComp_OnOffCycle
	{
		protected new StorytellerCompProperties_RandomMechhiveQuest Props => (StorytellerCompProperties_RandomMechhiveQuest)props;

		public override IncidentParms GenerateParms(IncidentCategoryDef incCat, IIncidentTarget target)
		{
			IncidentParms incidentParms = base.GenerateParms(incCat, target);
			if (TryGetQuest(incidentParms.points, target, out QuestScriptDef result))
            {
				incidentParms.questScriptDef = result;
				return incidentParms;
            }
			return null;
		}

		private bool TryGetQuest(float points, IIncidentTarget target, out QuestScriptDef chosen)
		{
			return Props.quests.Where((QuestScriptDef x) => x.IsRootRandomSelected && x.CanRun(points, target)).TryRandomElementByWeight((QuestScriptDef x) => NaturalRandomQuestChooser.GetNaturalRandomSelectionWeight(x, points, target.StoryState), out chosen);
		}
	}
}
