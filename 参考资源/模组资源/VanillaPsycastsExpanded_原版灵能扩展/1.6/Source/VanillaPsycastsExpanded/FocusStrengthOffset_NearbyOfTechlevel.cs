using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded;

public class FocusStrengthOffset_NearbyOfTechlevel : FocusStrengthOffset
{
	public float radius = 10f;

	public TechLevel techLevel;

	public override float GetOffset(Thing parent, Pawn user = null)
	{
		Map mapHeld = parent.MapHeld;
		int num;
		float num2;
		if (mapHeld != null)
		{
			List<Thing> things = GetThings(parent.Position, mapHeld);
			num = Mathf.Clamp(things.Count, 1, 10);
			num2 = Mathf.Clamp(things.Sum((Thing t) => t.MarketValue * (float)t.stackCount), 1f, 5000f);
		}
		else
		{
			num = 1;
			num2 = parent.MarketValue * (float)parent.stackCount;
		}
		return (float)num / 5.55f * num2 / 10000f;
	}

	public override string GetExplanation(Thing parent)
	{
		Map mapHeld = parent.MapHeld;
		int num = ((mapHeld == null) ? 1 : GetThings(parent.Position, mapHeld).Count);
		return "VPE.ThingsOfLevel".Translate(num, techLevel.ToString()) + ": " + GetOffset(parent).ToStringWithSign("0%");
	}

	public override void PostDrawExtraSelectionOverlays(Thing parent, Pawn user = null)
	{
		base.PostDrawExtraSelectionOverlays(parent, user);
		GenDraw.DrawRadiusRing(parent.Position, radius, PlaceWorker_MeditationOffsetBuildingsNear.RingColor);
		Map mapHeld = parent.MapHeld;
		if (mapHeld == null)
		{
			return;
		}
		foreach (Thing thing in GetThings(parent.Position, mapHeld))
		{
			GenDraw.DrawLineBetween(parent.TrueCenter(), thing.TrueCenter(), SimpleColor.Green);
		}
	}

	protected virtual List<Thing> GetThings(IntVec3 cell, Map map)
	{
		return (from t in GenRadialCached.RadialDistinctThingsAround(cell, map, radius, useCenter: true)
			where t.def.techLevel == techLevel
			select t).Take(10).ToList();
	}
}
