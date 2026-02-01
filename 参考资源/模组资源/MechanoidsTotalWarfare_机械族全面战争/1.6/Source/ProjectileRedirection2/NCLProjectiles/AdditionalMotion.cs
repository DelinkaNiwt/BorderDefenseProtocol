using UnityEngine;

namespace NCLProjectiles;

public class AdditionalMotion
{
	public AdditionalMotionDirectional horizontal;

	public AdditionalMotionDirectional vertical;

	public Vector3 Resolve(int tick)
	{
		float x = horizontal?.Resolve(tick) ?? 0f;
		float y = 0f;
		return new Vector3(x, y, vertical?.Resolve(tick) ?? 0f);
	}
}
