using System.Collections.Generic;
using Verse;

namespace NCL;

public class CompWeaponGiver : ThingComp
{
	private int lastCheckTick = -1;

	private List<int> lastUsedTicks;

	private CompProperties_WeaponGiver Props => (CompProperties_WeaponGiver)props;

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
		lastUsedTicks = new List<int>();
		for (int i = 0; i < Props.weaponPool.Count; i++)
		{
			lastUsedTicks.Add(-1);
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		if (Find.TickManager.TicksGame < lastCheckTick + Props.checkIntervalTicks)
		{
			return;
		}
		lastCheckTick = Find.TickManager.TicksGame;
		if (!(parent is Pawn pawn) || pawn.equipment?.Primary != null)
		{
			return;
		}
		if (Props.weaponPool == null || Props.weaponPool.Count == 0)
		{
			Log.Warning($"CompWeaponGiver: No weapons in pool for {pawn}");
			return;
		}
		List<int> availableWeapons = new List<int>();
		for (int i = 0; i < Props.weaponPool.Count; i++)
		{
			int lastUsedTick = lastUsedTicks[i];
			int cooldownTicks = Props.cooldownTicks[i];
			if (lastUsedTick == -1 || Find.TickManager.TicksGame >= lastUsedTick + cooldownTicks)
			{
				availableWeapons.Add(i);
			}
		}
		if (availableWeapons.Count > 0)
		{
			int weaponIndexToGive = ((!Props.randomizeIfMultipleAvailable || availableWeapons.Count <= 1) ? availableWeapons[0] : availableWeapons.RandomElement());
			GiveWeapon(pawn, weaponIndexToGive);
		}
	}

	private void GiveWeapon(Pawn pawn, int weaponIndex)
	{
		ThingDef weaponDef = Props.weaponPool[weaponIndex];
		Thing weapon = ThingMaker.MakeThing(weaponDef);
		pawn.equipment.AddEquipment(weapon as ThingWithComps);
		lastUsedTicks[weaponIndex] = Find.TickManager.TicksGame;
	}
}
