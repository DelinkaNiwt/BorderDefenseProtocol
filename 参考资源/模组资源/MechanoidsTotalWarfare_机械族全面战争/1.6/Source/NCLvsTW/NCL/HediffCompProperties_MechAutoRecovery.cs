using System.Collections.Generic;
using Verse;

namespace NCL;

public class HediffCompProperties_MechAutoRecovery : HediffCompProperties
{
	public int tickMultiflier = 1800;

	public float healPoint = 1f;

	public List<HediffDef> healHediffDefs = new List<HediffDef>();

	public HediffCompProperties_MechAutoRecovery()
	{
		compClass = typeof(HediffComp_MechAutoRecovery);
	}
}
