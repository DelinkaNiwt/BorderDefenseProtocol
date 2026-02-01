using UnityEngine;
using Verse;

namespace NCLWorm;

public class NCL_ConfocalLaser : Mote
{
	private readonly SimpleCurve curve = new SimpleCurve
	{
		Points = 
		{
			new CurvePoint(0f, 0f),
			new CurvePoint(0.7f, 22.5f),
			new CurvePoint(1f, 45f)
		}
	};

	private float Height => curve.Evaluate(base.AgeSecs);

	public override Vector3 DrawPos => base.DrawPos + Vector3.forward.RotatedBy(exactRotation + Height);
}
