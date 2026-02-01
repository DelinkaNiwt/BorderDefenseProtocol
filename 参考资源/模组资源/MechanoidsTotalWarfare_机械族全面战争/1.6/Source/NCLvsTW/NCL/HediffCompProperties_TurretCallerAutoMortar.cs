using Verse;

namespace NCL;

public class HediffCompProperties_TurretCallerAutoMortar : HediffCompProperties
{
	public ThingDef turretDef;

	public int turretCount = 1;

	public int spawnRadius = 15;

	public int minSpawnDistance = 5;

	public bool leaveSlag = true;

	public bool canRoofPunch = true;

	public int triggerRadius = 150;

	public ThingDef extraTurretDef;

	public int extraTurretCount = 0;

	public bool extraTurretLeaveSlag = true;

	public HediffCompProperties_TurretCallerAutoMortar()
	{
		compClass = typeof(HediffComp_TurretCallerAutoMortar);
	}
}
