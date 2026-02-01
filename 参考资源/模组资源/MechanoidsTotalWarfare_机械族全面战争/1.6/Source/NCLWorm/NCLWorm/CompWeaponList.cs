using System.Collections.Generic;
using System.Linq;
using Verse;

namespace NCLWorm;

public class CompWeaponList : ThingComp
{
	public List<ThingDefCountClass> weaponUsed = new List<ThingDefCountClass>();

	private CompProperties_WeaponList Props => (CompProperties_WeaponList)props;

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
		foreach (ThingDefCountClass weapon in Props.weaponList)
		{
			ThingDefCountClass thingDefCountClass = weapon;
			thingDefCountClass.count = -1;
			weaponUsed.Add(thingDefCountClass);
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		if (Find.TickManager.TicksGame % Props.checkIntervalTicks != 0 || !(parent is Pawn pawn) || pawn.equipment?.Primary != null)
		{
			return;
		}
		List<ThingDef> list = new List<ThingDef>();
		foreach (ThingDefCountClass item in weaponUsed)
		{
			if (item.count + Props.weaponList.First((ThingDefCountClass t) => t.thingDef == item.thingDef).count <= Find.TickManager.TicksGame)
			{
				list.Add(item.thingDef);
			}
		}
		if (!list.NullOrEmpty())
		{
			GiveWeapon(pawn, list.RandomElement());
		}
	}

	private void GiveWeapon(Pawn pawn, ThingDef weaponIndex)
	{
		ThingWithComps newEq = (ThingWithComps)ThingMaker.MakeThing(weaponIndex);
		pawn.equipment.AddEquipment(newEq);
		weaponUsed.First((ThingDefCountClass t) => t.thingDef == weaponIndex).count = Find.TickManager.TicksGame;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Collections.Look(ref weaponUsed, "weaponUsed", LookMode.Deep);
	}
}
