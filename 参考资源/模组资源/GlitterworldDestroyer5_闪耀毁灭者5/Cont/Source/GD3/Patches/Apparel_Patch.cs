using System;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace GD3
{
	[HarmonyPatch(typeof(Apparel), "PawnCanWear")]
	public static class Apparel_Patch
	{
		public static void Postfix(Apparel __instance, Pawn pawn, ref bool __result)
		{
			if (__instance.def.defName == "Apparel_GD_BlackWindbreaker")
            {
				if (pawn != null && pawn.RaceProps.Humanlike && pawn.story.bodyType.defName == "Fat")
                {
					Apparel apparel = __instance;
					GenPlace.TryPlaceThing(apparel, pawn.Position, pawn.Map, ThingPlaceMode.Near, null, null, default(Rot4));
					MoteMaker.ThrowText(pawn.PositionHeld.ToVector3(), pawn.MapHeld, "GD_FailedToWear".Translate(), 5f);
					__result = false;
                }
            }
		}
	}
}