using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace WeaponFitting;

public class ThingSetMakers_UniqueWeapon : ThingSetMaker
{
	protected override bool CanGenerateSub(ThingSetMakerParams parms)
	{
		return ModsConfig.OdysseyActive && (!parms.countRange.HasValue || parms.countRange.Value.max > 0) && (!parms.totalMarketValueRange.HasValue || parms.totalMarketValueRange.Value.max > 0f) && AllGeneratableThingsDebugSub(parms).Any();
	}

	protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
	{
		int num = ((!parms.countRange.HasValue) ? 1 : parms.countRange.GetValueOrDefault().RandomInRange);
		if (parms.countRange.HasValue)
		{
			num = Mathf.Max(parms.countRange.Value.RandomInRange, num);
		}
		FloatRange floatRange = parms.totalMarketValueRange ?? new FloatRange(0f, float.MaxValue);
		float num2 = 0f;
		for (int i = 0; i < num; i++)
		{
			bool flag = i == num - 1;
			int num3 = 999;
			Thing thing;
			do
			{
				thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.HasComp<CompUniqueWeapon>() && !x.HasComp<CompEmptyUniqueWeapon>()).RandomElement());
			}
			while (num3-- > 0 && (num2 + thing.MarketValue > floatRange.max || (flag && num2 + thing.MarketValue < floatRange.min)));
			if (num3 <= 0)
			{
				break;
			}
			num2 += thing.MarketValue;
			outThings.Add(thing);
		}
	}

	protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
	{
		return DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.HasComp<CompUniqueWeapon>());
	}
}
