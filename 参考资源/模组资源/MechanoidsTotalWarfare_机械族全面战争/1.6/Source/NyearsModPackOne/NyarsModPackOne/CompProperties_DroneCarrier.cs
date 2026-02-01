using Verse;

namespace NyarsModPackOne;

public class CompProperties_DroneCarrier : CompProperties
{
	public ThingDef fixedIngredient;

	public int costPerDrone = 25;

	public int maxIngredientCount = 1000;

	public int cooldownTicks = 60000;

	public int maxDronesPerSpawn = 5;

	public int startingIngredientCount;

	public PawnKindDef droneKind;

	public string gizmoIconPath = "Races/ScutigerDrone_south";

	public CompProperties_DroneCarrier()
	{
		compClass = typeof(CompDroneCarrier);
	}
}
