using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

public class StatPart_NearbyFoci : StatPart
{
	public static bool ShouldApply = true;

	public override void TransformValue(StatRequest req, ref float val)
	{
		if (req.Thing == null || req.Pawn == null || !ShouldApply || req.Thing.Map == null)
		{
			return;
		}
		try
		{
			ShouldApply = false;
			List<(Thing, float)> list = AllFociNearby(req.Thing, req.Pawn);
			for (int i = 0; i < list.Count; i++)
			{
				val += list[i].Item2;
			}
		}
		finally
		{
			ShouldApply = true;
		}
	}

	private static List<(Thing thing, float value)> AllFociNearby(Thing main, Pawn pawn)
	{
		CompMeditationFocus compMeditationFocus = main.TryGetComp<CompMeditationFocus>();
		if (compMeditationFocus == null)
		{
			return new List<(Thing, float)>();
		}
		Map map = pawn.Map;
		IntVec3 position = main.Position;
		HashSet<MeditationFocusDef> hashSet = new HashSet<MeditationFocusDef>(compMeditationFocus.Props.focusTypes);
		List<(Thing, List<MeditationFocusDef>, float)> list = new List<(Thing, List<MeditationFocusDef>, float)>();
		foreach (CompMeditationFocus item4 in GenRadialCached.MeditationFociAround(position, map, MeditationUtility.FocusObjectSearchRadius, useCenter: true))
		{
			if (item4.CanPawnUse(pawn))
			{
				float statValueForPawn = item4.parent.GetStatValueForPawn(StatDefOf.MeditationFocusStrength, pawn);
				list.Add((item4.parent, item4.Props.focusTypes, statValueForPawn));
			}
		}
		list.Sort(((Thing thing, List<MeditationFocusDef> types, float value) a, (Thing thing, List<MeditationFocusDef> types, float value) b) => b.value.CompareTo(a.value));
		List<(Thing, float)> list2 = new List<(Thing, float)>();
		foreach (var item5 in list)
		{
			Thing item = item5.Item1;
			List<MeditationFocusDef> item2 = item5.Item2;
			float item3 = item5.Item3;
			bool flag = false;
			foreach (MeditationFocusDef item6 in item2)
			{
				if (hashSet.Add(item6))
				{
					flag = true;
				}
			}
			if (flag)
			{
				list2.Add((item, item3));
			}
		}
		return list2;
	}

	public override string ExplanationPart(StatRequest req)
	{
		if (req.Thing == null || req.Pawn == null || !ShouldApply || req.Thing.Map == null)
		{
			return "";
		}
		try
		{
			ShouldApply = false;
			List<string> list = (from tuple in AllFociNearby(req.Thing, req.Pawn)
				select tuple.thing.LabelCap + ": " + StatDefOf.MeditationFocusStrength.Worker.ValueToString(tuple.value, finalized: true, ToStringNumberSense.Offset)).ToList();
			return (list.Count > 0) ? ((string)("VPE.Nearby".Translate() + ":\n" + list.ToLineList("  ", capitalizeItems: true))) : "";
		}
		finally
		{
			ShouldApply = true;
		}
	}
}
