using System.Collections.Generic;
using Verse;

namespace NCL;

public class CompProperties_WeaponGiver : CompProperties
{
	public List<ThingDef> weaponPool;

	public List<int> cooldownTicks;

	public int checkIntervalTicks = 60;

	public bool randomizeIfMultipleAvailable = false;

	public CompProperties_WeaponGiver()
	{
		compClass = typeof(CompWeaponGiver);
	}

	public override void ResolveReferences(ThingDef parentDef)
	{
		base.ResolveReferences(parentDef);
		if (weaponPool != null && weaponPool.Count > 3)
		{
			Log.Warning("CompProperties_WeaponGiver: weaponPool has more than 3 weapons, truncating to first 3.");
			weaponPool = weaponPool.GetRange(0, 3);
		}
		if (cooldownTicks == null || cooldownTicks.Count != weaponPool.Count)
		{
			cooldownTicks = new List<int>();
			for (int i = 0; i < weaponPool.Count; i++)
			{
				cooldownTicks.Add(300);
			}
		}
	}
}
