using Verse;

namespace AncotLibrary;

public class PawnRenderNodeProperties_CombatPlatform : PawnRenderNodeProperties
{
	public bool combatDrone = false;

	public bool drawUndrafted = true;

	public bool isApparel = false;

	public PawnRenderNodeProperties_CombatPlatform()
	{
		nodeClass = typeof(PawnRenderNode_CombatPlatform);
		workerClass = typeof(PawnRenderNodeWorker_CombatPlatform);
	}
}
