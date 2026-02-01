using System.Collections.Generic;
using Verse;

namespace NCLWorm;

public class CompProperties_WeaponList : CompProperties
{
	public List<ThingDefCountClass> weaponList;

	public int checkIntervalTicks = 60;

	public bool randomizeIfMultipleAvailable = false;

	public CompProperties_WeaponList()
	{
		compClass = typeof(CompWeaponList);
	}
}
