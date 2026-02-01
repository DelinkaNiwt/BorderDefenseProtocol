using System.Collections.Generic;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class PsySet : IExposable, IRenameable
{
	public HashSet<AbilityDef> Abilities = new HashSet<AbilityDef>();

	public string Name;

	public string RenamableLabel
	{
		get
		{
			return Name;
		}
		set
		{
			Name = value;
		}
	}

	public string BaseLabel => Name;

	public string InspectLabel => Name;

	public void ExposeData()
	{
		Scribe_Values.Look(ref Name, "name");
		Scribe_Collections.Look(ref Abilities, "abilities");
	}
}
