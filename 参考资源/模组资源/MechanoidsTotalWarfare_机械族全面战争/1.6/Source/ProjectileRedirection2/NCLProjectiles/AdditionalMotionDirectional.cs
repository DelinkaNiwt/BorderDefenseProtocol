using System;

namespace NCLProjectiles;

public class AdditionalMotionDirectional
{
	public float amplitude;

	public Func<float, float> function;

	public int period;

	public int periodOffset;

	private float value;

	public float Resolve(int tick)
	{
		value = (float)(tick + periodOffset % period) / (float)period;
		return amplitude * ((function == null) ? value : function(value));
	}
}
