using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_EmptyUniqueWeapon : CompProperties_UniqueWeapon, IPawnWeaponGizmoProvider
{
	public int max_traits = 2;

	public int gizmoOrder = -99;

	public CompProperties_EmptyUniqueWeapon()
	{
		compClass = typeof(CompEmptyUniqueWeapon);
	}

	public IEnumerable<Gizmo> GetWeaponGizmos()
	{
		throw new NotImplementedException();
	}
}
