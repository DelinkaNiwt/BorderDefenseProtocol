using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public class Ability_ReverseEngineer : Ability
{
	private static readonly Dictionary<Thing, List<ResearchProjectDef>> researchCache = new Dictionary<Thing, List<ResearchProjectDef>>();

	private static readonly AccessTools.FieldRef<ResearchManager, Dictionary<ResearchProjectDef, float>> progressRef = AccessTools.FieldRefAccess<ResearchManager, Dictionary<ResearchProjectDef, float>>("progress");

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (ResearchProjectDef item in from project in GetResearchFor(targets[0].Thing)
			where project != null && !project.IsFinished
			select project)
		{
			int techprints = Find.ResearchManager.GetTechprints(item);
			if (techprints < item.TechprintCount)
			{
				Find.ResearchManager.AddTechprints(item, techprints - item.TechprintCount);
			}
			progressRef(Find.ResearchManager)[item] = item.baseCost;
			Find.ResearchManager.ReapplyAllMods();
			TaleRecorder.RecordTale(TaleDefOf.FinishedResearchProject, base.pawn, item);
			DiaNode diaNode = new DiaNode("ResearchFinished".Translate(item.LabelCap) + "\n\n" + item.description);
			diaNode.options.Add(DiaOption.DefaultOK);
			Find.WindowStack.Add(new Dialog_NodeTree(diaNode, delayInteractivity: true));
			if (!item.discoveredLetterTitle.NullOrEmpty() && Find.Storyteller.difficulty.AllowedBy(item.discoveredLetterDisabledWhen))
			{
				Find.LetterStack.ReceiveLetter(item.discoveredLetterTitle, item.discoveredLetterText, LetterDefOf.NeutralEvent);
			}
		}
		targets[0].Thing.Destroy();
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (!((Ability)this).ValidateTarget(target, showMessages))
		{
			return false;
		}
		if (!target.HasThing)
		{
			return false;
		}
		List<ResearchProjectDef> researchFor = GetResearchFor(target.Thing);
		if (researchFor.NullOrEmpty())
		{
			if (showMessages)
			{
				Messages.Message("VPE.Research".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		if (researchFor.TrueForAll((ResearchProjectDef project) => project.IsFinished))
		{
			if (showMessages)
			{
				Messages.Message("VPE.AlreadyResearch".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		return true;
	}

	private static List<ResearchProjectDef> GetResearchFor(Thing t)
	{
		if (researchCache.TryGetValue(t, out var value))
		{
			return value;
		}
		value = new List<ResearchProjectDef>();
		if (!t.def.researchPrerequisites.NullOrEmpty())
		{
			value.AddRange(t.def.researchPrerequisites);
		}
		if (t.def.recipeMaker != null)
		{
			if (t.def.recipeMaker.researchPrerequisite != null)
			{
				value.Add(t.def.recipeMaker.researchPrerequisite);
			}
			if (!t.def.recipeMaker.researchPrerequisites.NullOrEmpty())
			{
				value.AddRange(t.def.recipeMaker.researchPrerequisites);
			}
		}
		foreach (RecipeDef allDef in DefDatabase<RecipeDef>.AllDefs)
		{
			if (allDef.products.Any((ThingDefCountClass prod) => prod.thingDef == t.def))
			{
				if (allDef.researchPrerequisite != null)
				{
					value.Add(allDef.researchPrerequisite);
				}
				if (!allDef.researchPrerequisites.NullOrEmpty())
				{
					value.AddRange(allDef.researchPrerequisites);
				}
			}
		}
		value.RemoveAll((ResearchProjectDef proj) => proj == null);
		return researchCache[t] = value;
	}
}
