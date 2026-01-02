using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_ApparelReloadable_DeployPawn : CompProperties_ApparelReloadable
{
	public HediffDef hediffAddToSpawnPawn;

	public PawnKindDef spawnPawnKind;

	public int maxPawnsToSpawn = 3;

	public int costPerPawn;

	public FleckDef deployFleck;

	public SoundDef deploySound;

	public int ai_DeployIntervalTick = 0;

	public int cooldownTicks = 0;

	public EffecterDef spawnEffecter;

	public EffecterDef spawnedMechEffecter;

	public bool attachSpawnedEffecter;

	public bool attachSpawnedMechEffecter;

	public bool sortie = true;

	public bool showSortieSwitchGizmo = true;

	public int gizmoOrder = -99;

	public string gizmoLabel1;

	public string gizmoLabel2;

	public string gizmoDesc1;

	public string gizmoDesc2;

	public string gizmoIconPath1 = "AncotLibrary/Gizmos/Switch_I";

	public string gizmoIconPath2 = "AncotLibrary/Gizmos/Switch_II";

	public string iconPath = "AncotLibrary/Gizmos/SwitchA";

	public CompProperties_ApparelReloadable_DeployPawn()
	{
		compClass = typeof(CompApparelReloadable_DeployPawn);
	}
}
