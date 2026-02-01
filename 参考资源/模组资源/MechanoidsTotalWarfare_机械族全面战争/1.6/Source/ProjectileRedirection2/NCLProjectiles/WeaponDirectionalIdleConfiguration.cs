using Verse;

namespace NCLProjectiles;

public class WeaponDirectionalIdleConfiguration
{
	public WeaponIdleConfiguration north = new WeaponIdleConfiguration();

	public WeaponIdleConfiguration east = new WeaponIdleConfiguration();

	public WeaponIdleConfiguration west = new WeaponIdleConfiguration
	{
		angle = 217f,
		flipped = true
	};

	public WeaponIdleConfiguration south = new WeaponIdleConfiguration();

	public WeaponIdleConfiguration GetConfigurationForRotation(Rot4 rotation)
	{
		return rotation.AsInt switch
		{
			0 => north, 
			1 => east, 
			3 => west, 
			_ => south, 
		};
	}

	public void ApplyConfiguration(ref float angle, ref bool flipped, Rot4 rotation)
	{
		WeaponIdleConfiguration configurationForRotation = GetConfigurationForRotation(rotation);
		if (configurationForRotation != null)
		{
			angle = configurationForRotation.angle;
			flipped = configurationForRotation.flipped;
		}
	}
}
