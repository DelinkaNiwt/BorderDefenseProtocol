using UnityEngine;
using Verse;

namespace AncotLibrary;

public class Comproperties_ProjectileTrail : CompProperties
{
	public FleckDef fleckDef;

	public int tickPerTrail = 1;

	public Color color = new Color(1f, 1f, 1f, 1f);

	public Comproperties_ProjectileTrail()
	{
		compClass = typeof(CompProjectileTrail);
	}
}
