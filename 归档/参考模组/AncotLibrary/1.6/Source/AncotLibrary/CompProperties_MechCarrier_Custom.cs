using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class CompProperties_MechCarrier_Custom : CompProperties_ThingCarrier_Custom
{
	public HediffDef hediffAddToSpawnPawn;

	public int costPerPawn = 10;

	public PawnKindDef spawnPawnKind;

	public int cooldownTicks = 900;

	public int maxPawnsToSpawn = 3;

	public EffecterDef spawnEffecter;

	public EffecterDef spawnedMechEffecter;

	public bool attachSpawnedEffecter;

	public bool attachSpawnedMechEffecter;

	public bool killSpawnedPawnIfParentDied = true;

	public bool recoverable = false;

	public float recoverFactor = 0.5f;

	public string iconPath = "AncotLibrary/Gizmos/SwitchA";

	public string iconPathRecover = "AncotLibrary/Gizmos/SwitchA";

	public CompProperties_MechCarrier_Custom()
	{
		compClass = typeof(CompMechCarrier_Custom);
	}

	public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
	{
		foreach (string item in base.ConfigErrors(parentDef))
		{
			yield return item;
		}
	}
}
