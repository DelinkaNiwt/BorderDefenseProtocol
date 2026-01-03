using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompAerocraft : ThingComp
{
	public CompProperties_Aerocraft Props => (CompProperties_Aerocraft)props;

	public GraphicData InflightGraphicData => Props.inflightGraphicData;

	public string TakeOffIconPath => Props.takeOffIconPath;

	public string LandingIconPath => Props.landingIconPath;

	public bool RequirePilot => Props.requirePilot;

	public EffecterDef TakeOffEffect => Props.takeOffEffect;

	public float TakeOffFuelCost => Props.takeOffFuelCost;

	public Building_Aerocraft Aerocraft => parent as Building_Aerocraft;

	public override void CompTick()
	{
		if (!Props.flightFlecks.NullOrEmpty() && Aerocraft != null && Aerocraft.FlightState != AerocraftState.Grounded && Aerocraft.IsHashIntervalTick(Props.fleckIntervalTicks))
		{
			for (int i = 0; i < Props.flightFlecks.Count; i++)
			{
				Quaternion quaternion = Quaternion.AngleAxis(Aerocraft.CurDirection, Vector3.up);
				Vector3 vector = quaternion * Props.flightFlecks[i].offset;
				AncotFleckMaker.ThrowTrailFleckUp(parent.DrawPos + vector, Aerocraft.Map, Props.flightFlecks[i].fleckColor, Props.flightFlecks[i].fleckDef);
			}
		}
	}
}
