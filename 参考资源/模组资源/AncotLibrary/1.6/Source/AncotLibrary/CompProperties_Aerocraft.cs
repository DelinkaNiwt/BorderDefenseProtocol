using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class CompProperties_Aerocraft : CompProperties
{
	public GraphicData inflightGraphicData;

	public string takeOffIconPath;

	public string landingIconPath;

	public bool requirePilot = true;

	public List<AerocraftFleckData> flightFlecks = new List<AerocraftFleckData>();

	public int fleckIntervalTicks = 1;

	public EffecterDef takeOffEffect;

	public float takeOffFuelCost = 0f;

	public CompProperties_Aerocraft()
	{
		compClass = typeof(CompAerocraft);
	}
}
