using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_IceBreath : Ability_ShootProjectile
{
	protected override Projectile ShootProjectile(GlobalTargetInfo target)
	{
		IceBreatheProjectile obj = ((Ability_ShootProjectile)this).ShootProjectile(target) as IceBreatheProjectile;
		obj.ability = (Ability)(object)this;
		return (Projectile)(object)obj;
	}
}
