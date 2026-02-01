using System.Collections.Generic;
using Verse;

namespace NCL;

public class CompProperties_MechAutoRecovery : CompProperties
{
	public int tickInterval = 1800;

	public float healAmount = 1f;

	public List<HediffDef> healHediffDefs = new List<HediffDef>();

	public CompProperties_MechAutoRecovery()
	{
		compClass = typeof(Comp_MechAutoRecovery);
	}
}
