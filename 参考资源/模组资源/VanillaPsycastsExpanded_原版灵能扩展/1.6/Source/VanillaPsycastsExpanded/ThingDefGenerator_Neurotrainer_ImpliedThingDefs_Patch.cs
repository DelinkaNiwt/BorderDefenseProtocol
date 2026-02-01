using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class ThingDefGenerator_Neurotrainer_ImpliedThingDefs_Patch
{
	public static Func<string, bool, ThingDef> BaseNeurotrainer = AccessTools.Method(typeof(ThingDefGenerator_Neurotrainer), "BaseNeurotrainer").CreateDelegate<Func<string, bool, ThingDef>>();

	public static void Postfix(ref IEnumerable<ThingDef> __result, bool hotReload)
	{
		__result = __result.Where((ThingDef def) => !def.defName.StartsWith(ThingDefGenerator_Neurotrainer.PsytrainerDefPrefix)).Concat(ImpliedThingDefs(hotReload));
	}

	public static IEnumerable<ThingDef> ImpliedThingDefs(bool hotReload)
	{
		foreach (AbilityDef allDef in DefDatabase<AbilityDef>.AllDefs)
		{
			AbilityExtension_Psycast abilityExtension_Psycast = allDef.Psycast();
			if (abilityExtension_Psycast != null)
			{
				ThingDef thingDef = BaseNeurotrainer(ThingDefGenerator_Neurotrainer.PsytrainerDefPrefix + "_" + ((Def)(object)allDef).defName, hotReload);
				thingDef.label = "PsycastNeurotrainerLabel".Translate(((Def)(object)allDef).label);
				thingDef.description = "PsycastNeurotrainerDescription".Translate(allDef.Named("PSYCAST"), $"[{abilityExtension_Psycast.path.LabelCap}]\n{((Def)(object)allDef).description}".Named("PSYCASTDESCRIPTION"));
				thingDef.comps.Add(new CompProperties_Usable
				{
					compClass = typeof(CompUsable),
					useJob = JobDefOf.UseNeurotrainer,
					useLabel = "PsycastNeurotrainerUseLabel".Translate(((Def)(object)allDef).label)
				});
				thingDef.comps.Add((CompProperties)(object)new CompProperties_UseEffect_Psytrainer
				{
					ability = allDef
				});
				thingDef.statBases.Add(new StatModifier
				{
					stat = StatDefOf.MarketValue,
					value = Mathf.Round(500f + 300f * (float)abilityExtension_Psycast.level)
				});
				thingDef.thingCategories = new List<ThingCategoryDef> { ThingCategoryDefOf.NeurotrainersPsycast };
				thingDef.thingSetMakerTags = new List<string> { "RewardStandardLowFreq" };
				thingDef.modContentPack = ((Def)(object)allDef).modContentPack;
				thingDef.descriptionHyperlinks = new List<DefHyperlink>
				{
					new DefHyperlink((Def)(object)allDef)
				};
				thingDef.stackLimit = 1;
				yield return thingDef;
			}
		}
	}
}
