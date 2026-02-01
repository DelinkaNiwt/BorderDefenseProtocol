using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class PositionTracker : IExposable
{
	public Vector3 previousVisualPosition = Vector3.zero;

	public Vector3 currentVisualPosition = Vector3.zero;

	public Quaternion currentVisualRotation = Quaternion.identity;

	public float currentVisualAngle;

	public void PostSpawnSetup(Vector3 initialPosition)
	{
		currentVisualPosition = initialPosition;
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref previousVisualPosition, "previousVisualPosition");
		Scribe_Values.Look(ref currentVisualPosition, "currentVisualPosition");
	}

	public void Tick(Vector3 newPosition)
	{
		previousVisualPosition = currentVisualPosition;
		currentVisualPosition = newPosition;
		CalculateRotation();
	}

	public void CalculateRotation()
	{
		if (currentVisualPosition != previousVisualPosition)
		{
			currentVisualRotation = Quaternion.LookRotation(currentVisualPosition - previousVisualPosition);
			currentVisualAngle = currentVisualRotation.eulerAngles.y;
		}
	}
}
