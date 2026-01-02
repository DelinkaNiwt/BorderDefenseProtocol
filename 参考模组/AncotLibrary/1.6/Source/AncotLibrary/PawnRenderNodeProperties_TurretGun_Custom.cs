using Verse;

namespace AncotLibrary;

public class PawnRenderNodeProperties_TurretGun_Custom : PawnRenderNodeProperties
{
	public bool combatDrone = false;

	public bool drawUndrafted = true;

	public bool isApparel = false;

	public bool alwaysDraw = false;

	public PawnRenderNodeProperties_TurretGun_Custom()
	{
		nodeClass = typeof(PawnRenderNode_TurretGun_Custom);
		workerClass = typeof(PawnRenderNodeWorker_TurretGun_Custom);
	}
}
