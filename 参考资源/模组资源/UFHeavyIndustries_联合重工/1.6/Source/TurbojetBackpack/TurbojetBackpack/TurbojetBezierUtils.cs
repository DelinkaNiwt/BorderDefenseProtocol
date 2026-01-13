using UnityEngine;

namespace TurbojetBackpack;

public static class TurbojetBezierUtils
{
	public static Vector3 CalculateThreePowerBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
	{
		float num = 1f - t;
		return num * num * num * p0 + 3f * (num * num) * t * p1 + 3f * num * (t * t) * p2 + t * t * t * p3;
	}

	public static Vector3 CalculateBezierTangent(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
	{
		float num = 1f - t;
		return 3f * num * num * (p1 - p0) + 6f * num * t * (p2 - p1) + 3f * t * t * (p3 - p2);
	}

	public static float EstimateCurveLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
	{
		float magnitude = (p3 - p0).magnitude;
		float num = (p0 - p1).magnitude + (p1 - p2).magnitude + (p2 - p3).magnitude;
		return (num + magnitude) / 2f;
	}
}
