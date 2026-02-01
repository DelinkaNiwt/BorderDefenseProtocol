using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LudeonTK;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[StaticConstructorOnStartup]
public static class PsycastUtility
{
	private static readonly HashSet<ThingDef> eltexThings;

	static PsycastUtility()
	{
		eltexThings = (from recipe in DefDatabase<RecipeDef>.AllDefs
			where recipe.ingredients.Any((IngredientCount x) => x.IsFixedIngredient && x.FixedIngredient == VPE_DefOf.VPE_Eltex)
			select recipe.ProducedThingDef).ToHashSet();
	}

	public static void RecheckPaths(this Pawn pawn)
	{
		Hediff_PsycastAbilities hediff_PsycastAbilities = pawn.Psycasts();
		if (hediff_PsycastAbilities == null)
		{
			return;
		}
		if (hediff_PsycastAbilities.unlockedPaths != null)
		{
			foreach (PsycasterPathDef item in hediff_PsycastAbilities.unlockedPaths.ToList())
			{
				if (item.ensureLockRequirement && !item.CanPawnUnlock(pawn))
				{
					hediff_PsycastAbilities.previousUnlockedPaths.Add(item);
					hediff_PsycastAbilities.unlockedPaths.Remove(item);
				}
			}
		}
		if (hediff_PsycastAbilities.previousUnlockedPaths == null)
		{
			return;
		}
		foreach (PsycasterPathDef item2 in hediff_PsycastAbilities.previousUnlockedPaths.ToList())
		{
			if (item2.ensureLockRequirement)
			{
				if (item2.CanPawnUnlock(pawn))
				{
					hediff_PsycastAbilities.previousUnlockedPaths.Remove(item2);
					hediff_PsycastAbilities.unlockedPaths.Add(item2);
				}
			}
			else
			{
				hediff_PsycastAbilities.previousUnlockedPaths.Remove(item2);
				hediff_PsycastAbilities.unlockedPaths.Add(item2);
			}
		}
	}

	public static bool IsEltexOrHasEltexMaterial(this ThingDef def)
	{
		if (def != null)
		{
			if (def != VPE_DefOf.VPE_Eltex && (def.costList == null || !def.costList.Any((ThingDefCountClass x) => x.thingDef == VPE_DefOf.VPE_Eltex)))
			{
				return eltexThings.Contains(def);
			}
			return true;
		}
		return false;
	}

	public static Hediff_PsycastAbilities Psycasts(this Pawn pawn)
	{
		return (Hediff_PsycastAbilities)(object)pawn?.health?.hediffSet?.GetFirstHediffOfDef(VPE_DefOf.VPE_PsycastAbilityImplant);
	}

	[DebugAction("Pawns", "Reset Psycasts", true, false, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void ResetPsycasts(Pawn p)
	{
		p.Psycasts()?.Reset();
	}

	public static bool CanReceiveHypothermia(this Pawn pawn, out HediffDef hypothermiaHediff)
	{
		if (pawn.RaceProps.FleshType == FleshTypeDefOf.Insectoid)
		{
			hypothermiaHediff = VPE_DefOf.HypothermicSlowdown;
			return true;
		}
		if (pawn.RaceProps.IsFlesh)
		{
			hypothermiaHediff = HediffDefOf.Hypothermia;
			return true;
		}
		hypothermiaHediff = null;
		return false;
	}

	public static T CreateDelegate<T>(this MethodInfo method) where T : Delegate
	{
		return (T)method.CreateDelegate(typeof(T));
	}
}
