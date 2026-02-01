using Verse;

namespace NCL;

public class CompProperties_AdvancedAmmo : CompProperties
{
	public GG_Properties_RandomProjectile projectile1;

	public GG_Properties_RandomProjectile projectile2;

	public GG_Properties_RandomProjectile projectile3;

	public ThingDef secondaryProjectile0;

	public ThingDef secondaryProjectile1;

	public ThingDef secondaryProjectile2;

	public ThingDef secondaryProjectile3;

	public int primaryProjectileCount0 = 1;

	public int secondaryProjectileCount0 = 1;

	public int primaryProjectileCount1 = 1;

	public int secondaryProjectileCount1 = 1;

	public int primaryProjectileCount2 = 1;

	public int secondaryProjectileCount2 = 1;

	public int primaryProjectileCount3 = 1;

	public int secondaryProjectileCount3 = 1;

	public SoundDef secondarySoundCast;

	public SoundDef secondarySoundCastTail;

	public bool isBonusShot;

	public bool isSimultaneousShot;

	public string customTexturePath0;

	public string customTexturePath1;

	public string customTexturePath2;

	public string customTexturePath3;

	public CompProperties_AdvancedAmmo()
	{
		compClass = typeof(Comp_AdvancedAmmo);
	}
}
