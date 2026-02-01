using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded.Graphics;

public class Projectile_Pointing : Projectile_Explosive
{
	private Vector3 LookTowards => new Vector3(destination.x - origin.x, def.Altitude, destination.z - origin.z + ArcHeightFactor * (4f - 8f * base.DistanceCoveredFraction));

	private float ArcHeightFactor
	{
		get
		{
			float num = def.projectile.arcHeightFactor;
			float num2 = (destination - origin).MagnitudeHorizontalSquared();
			if (num * num > num2 * 0.2f * 0.2f)
			{
				num = Mathf.Sqrt(num2) * 0.2f;
			}
			return num;
		}
	}

	public override Quaternion ExactRotation => Quaternion.LookRotation(LookTowards);
}
