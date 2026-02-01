using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[StaticConstructorOnStartup]
internal class MeditationFocusAutopatcher
{
	static MeditationFocusAutopatcher()
	{
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			if (allDef.thingClass != null && typeof(Building_ResearchBench).IsAssignableFrom(allDef.thingClass))
			{
				ThingDef thingDef = allDef;
				if (thingDef.comps == null)
				{
					thingDef.comps = new List<CompProperties>();
				}
				allDef.comps.Add(new CompProperties_MeditationFocus
				{
					statDef = StatDefOf.MeditationFocusStrength,
					focusTypes = new List<MeditationFocusDef> { VPE_DefOf.VPE_Science },
					offsets = new List<FocusStrengthOffset>
					{
						new FocusStrengthOffset_ResearchSpeed
						{
							offset = 0.5f
						}
					}
				});
				thingDef = allDef;
				if (thingDef.statBases == null)
				{
					thingDef.statBases = new List<StatModifier>();
				}
				allDef.statBases.Add(new StatModifier
				{
					stat = StatDefOf.MeditationFocusStrength,
					value = 0f
				});
			}
			if (allDef.techLevel == TechLevel.Archotech)
			{
				ThingDef thingDef = allDef;
				if (thingDef.comps == null)
				{
					thingDef.comps = new List<CompProperties>();
				}
				allDef.comps.Add(new CompProperties_MeditationFocus
				{
					statDef = StatDefOf.MeditationFocusStrength,
					focusTypes = new List<MeditationFocusDef> { VPE_DefOf.VPE_Archotech },
					offsets = new List<FocusStrengthOffset>
					{
						new FocusStrengthOffset_NearbyOfTechlevel
						{
							radius = 4.9f,
							techLevel = TechLevel.Archotech
						}
					}
				});
				thingDef = allDef;
				if (thingDef.statBases == null)
				{
					thingDef.statBases = new List<StatModifier>();
				}
				allDef.statBases.Add(new StatModifier
				{
					stat = StatDefOf.MeditationFocusStrength,
					value = 0f
				});
			}
		}
	}
}
