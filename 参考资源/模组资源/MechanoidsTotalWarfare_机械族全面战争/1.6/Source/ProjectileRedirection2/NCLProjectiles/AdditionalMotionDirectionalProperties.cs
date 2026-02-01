using Verse;

namespace NCLProjectiles;

public class AdditionalMotionDirectionalProperties
{
	public FloatRange amplitude = FloatRange.Zero;

	public string function;

	public IntRange period = new IntRange(60, 60);

	public IntRange periodOffset = IntRange.Zero;

	public AdditionalMotionDirectional CreateInstance()
	{
		return new AdditionalMotionDirectional
		{
			amplitude = amplitude.RandomInRange,
			function = AnimationUtility.GetFunctionByName(function),
			period = period.RandomInRange,
			periodOffset = periodOffset.RandomInRange
		};
	}
}
