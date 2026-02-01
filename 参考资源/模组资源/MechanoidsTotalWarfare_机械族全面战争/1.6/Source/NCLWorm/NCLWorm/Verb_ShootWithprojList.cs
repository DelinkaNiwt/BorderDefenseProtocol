using System.Collections.Generic;
using System.Linq;
using Verse;

namespace NCLWorm;

public class Verb_ShootWithprojList : Verb_Shoot
{
	private int lii = 0;

	public List<ThingDef> things => (from item in base.EquipmentSource.def.descriptionHyperlinks
		where item.def is ThingDef
		select (ThingDef)item.def).ToList();

	public override ThingDef Projectile
	{
		get
		{
			lii++;
			if (lii >= things.Count)
			{
				lii = 0;
			}
			return things[lii];
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
	}
}
