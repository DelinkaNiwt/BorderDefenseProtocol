namespace NCLProjectiles;

public class AdditionalMotionProperties
{
	public AdditionalMotionDirectionalProperties horizontal;

	public AdditionalMotionDirectionalProperties vertical;

	public AdditionalMotion CreateInstance()
	{
		if (horizontal == null && vertical == null)
		{
			return null;
		}
		AdditionalMotion additionalMotion = new AdditionalMotion();
		if (horizontal != null)
		{
			additionalMotion.horizontal = horizontal.CreateInstance();
		}
		if (vertical != null)
		{
			additionalMotion.vertical = vertical.CreateInstance();
		}
		return additionalMotion;
	}
}
