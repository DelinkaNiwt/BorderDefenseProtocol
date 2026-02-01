using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Harmonist;

[HotSwappable]
public class Ability_TransmuteItem : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			Thing thing = globalTargetInfo.Thing;
			Map map = thing.Map;
			float value = thing.MarketValue * (float)thing.stackCount;
			IntVec3 position = thing.Position;
			List<ThingDef> source = (from thingDef2 in DefDatabase<ThingDef>.AllDefs
				where IsValid(thingDef2)
				let marketValue = thingDef2.BaseMarketValue
				let thingDef = thingDef2
				let count = Mathf.FloorToInt(value / thingDef.BaseMarketValue)
				where marketValue <= value
				where count <= thingDef.stackLimit
				where count >= 1
				select thingDef).ToList();
			float maxWeight = ((IEnumerable<ThingDef>)source).Max((Func<ThingDef, float>)WeightSelector);
			ThingDef thingDef = source.RandomElementByWeight((ThingDef thingDef2) => maxWeight - WeightSelector(thingDef2));
			thing.Destroy();
			thing = ThingMaker.MakeThing(thingDef);
			thing.stackCount = Mathf.FloorToInt(value / thingDef.BaseMarketValue);
			GenSpawn.Spawn(thing, position, map);
			float WeightSelector(ThingDef thingDef2)
			{
				float num = value / thingDef2.BaseMarketValue;
				return Mathf.Abs(num - (float)Mathf.FloorToInt(num));
			}
		}
	}

	private bool IsValid(ThingDef thingDef)
	{
		if (thingDef.category != ThingCategory.Item)
		{
			return false;
		}
		if (thingDef.IsCorpse)
		{
			return false;
		}
		if (thingDef.MadeFromStuff)
		{
			return false;
		}
		if (thingDef.IsEgg)
		{
			return false;
		}
		if (thingDef.tradeTags != null && thingDef.tradeTags.Any((string tag) => tag.Contains("CE") && tag.Contains("Ammo")))
		{
			return false;
		}
		return true;
	}

	public override bool CanHitTarget(LocalTargetInfo target)
	{
		if (((Ability)this).targetParams.CanTarget(target.Thing, (ITargetingSource)this))
		{
			return GenSight.LineOfSight(base.pawn.Position, target.Cell, base.pawn.Map, skipFirstCell: true);
		}
		return false;
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (!((Ability)this).ValidateTarget(target, showMessages))
		{
			return false;
		}
		if (target.Thing.MarketValue < 1f)
		{
			if (showMessages)
			{
				Messages.Message("VPE.TooCheap".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		return true;
	}
}
