using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
	public class PlaceWorker_Station : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			foreach (IntVec3 c in GenAdj.OccupiedRect(loc, rot, checkingDef.Size))
			{
				if (!c.InBounds(map))
				{
					continue;
				}
				Thing t = c.GetFirstThing(map, GDDefOf.HiTechResearchBench);
				if (t == null)
				{
					return TranslatorFormattedStringExtensions.Translate("GD.MustReserchBench", checkingDef);
				}
				else if (rot != t.Rotation)
                {
					return TranslatorFormattedStringExtensions.Translate("GD.MustSameRot", checkingDef);
				}
				bool flag;
				if (t.Rotation == Rot4.South && t.Position - loc == new IntVec3(-2, 0, 0))
                {
					flag = true;
                }
				else if (t.Rotation == Rot4.North && t.Position - loc == new IntVec3(2, 0, 0))
				{
					flag = true;
				}
				else if (t.Rotation == Rot4.East && t.Position - loc == new IntVec3(0, 0, -2))
				{
					flag = true;
				}
				else if (t.Rotation == Rot4.West && t.Position - loc == new IntVec3(0, 0, -2))
				{
					flag = true;
				}
				else
                {
					flag = false;
				}
				if (!flag)
                {
					return TranslatorFormattedStringExtensions.Translate("GD.MustPlaceEdge", checkingDef);
				}
			}
			return true;
		}
	}
}
