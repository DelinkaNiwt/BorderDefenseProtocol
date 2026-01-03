using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AlienRace;

public class HARStatueContainer : IExposable
{
	public static readonly string loadKey = typeof(HARStatueContainer).FullName;

	public ThingDef_AlienRace alienRace;

	public PawnKindDef kindDef;

	public AlienPartGenerator.AlienComp alienComp;

	public List<AlienPartGenerator.ExposableValueTuple<TraitDef, int>> traits = new List<AlienPartGenerator.ExposableValueTuple<TraitDef, int>>();

	public void ExposeData()
	{
		Scribe_Defs.Look(ref alienRace, "alienRace");
		Scribe_Defs.Look(ref kindDef, "kindDef");
		Scribe_Collections.Look(ref traits, "traits", LookMode.Deep);
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			if (traits == null)
			{
				traits = new List<AlienPartGenerator.ExposableValueTuple<TraitDef, int>>();
			}
			alienComp = Activator.CreateInstance<AlienPartGenerator.AlienComp>();
		}
		alienComp.PostExposeData();
	}
}
