using UnityEngine;
using Verse;

namespace NCLProjectiles;

public interface IInterceptorSource
{
	IntVec3 GetSourceCell();

	Vector3 GetSourcePosition();

	int GetBaseWidth();

	float GetRadius();

	float GetGridRadius();

	bool CanIntercept(Thing thing, Vector3 origin, Vector3 position);

	bool RejectInterception(Thing thing, Vector3 origin);

	bool CanInterceptBombardment(Map map, float damage, IntVec3 cell);

	void NotifyIntercept(Thing thing);

	void NotifyInterceptBombardment(Map map, float damage, IntVec3 cell);

	bool ShouldDrawField(ref CellRect cameraRect);

	void DrawField();
}
