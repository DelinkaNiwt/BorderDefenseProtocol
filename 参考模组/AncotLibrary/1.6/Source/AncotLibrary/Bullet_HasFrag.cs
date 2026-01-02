using UnityEngine;
using Verse;

namespace AncotLibrary;

public class Bullet_HasFrag : Projectile_Custom
{
	public Projectile_Frag_Extension Props_Frag => def.GetModExtension<Projectile_Frag_Extension>();

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Map map = base.Map;
		IntVec3 position = base.Position;
		Vector3 v = destination - origin;
		v.Normalize();
		float num = v.ToAngleFlat();
		Thing thing = equipment;
		base.Impact(hitThing, blockedByShield);
		if (!blockedByShield || Props_Frag.canSpawnOnShield)
		{
			for (int i = 0; i < Props_Frag.fragAmounts; i++)
			{
				float num2 = Rand.Range(-Props_Frag.angle / 2, Props_Frag.angle / 2);
				float angle = num + num2;
				FragHit(map, position, angle, Props_Frag.radius, hitThing);
			}
		}
	}

	public void FragHit(Map map, IntVec3 pos, float angle, float radius, Thing equipment)
	{
		IntVec3 intVec = (pos.ToVector3() + radius * Vector3Utility.FromAngleFlat(angle)).ToIntVec3();
		Projectile projectile = (Projectile)GenSpawn.Spawn(Props_Frag.frag, pos, map);
		projectile.Launch(launcher, intVec, intVec, ProjectileHitFlags.All, preventFriendlyFire: true, equipment);
	}
}
