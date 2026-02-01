using System.Collections.Generic;
using Verse;

namespace NCL;

public class CompProperties_MechEmployable : CompProperties
{
	public float silverPerDay = 100f;

	private Dictionary<Thing, float> enemyRecords;

	public CompProperties_MechEmployable()
	{
		compClass = typeof(Comp_MechEmployable);
		enemyRecords = new Dictionary<Thing, float>();
	}
}
