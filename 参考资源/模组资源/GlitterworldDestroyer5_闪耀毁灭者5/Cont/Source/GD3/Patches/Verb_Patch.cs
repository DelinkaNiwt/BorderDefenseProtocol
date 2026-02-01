using System;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GD3
{
	[HarmonyPatch(typeof(ThingDef), "SpecialDisplayStats")]
	public static class Verb_StatDrawEntry_Patch
	{
		public static void Postfix(ThingDef __instance, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
		{
			List<StatDrawEntry> list = __result.ToList();
			
			Ext_Arc ext = __instance.GetModExtension<Ext_Arc>();
			if (ext != null)
            {
                if (!__instance.Verbs.NullOrEmpty())
                {
                    VerbProperties verb = __instance.Verbs.First((VerbProperties x) => x.isPrimary);
                    if (!typeof(Verb_ThrowArc).IsAssignableFrom(verb.verbClass))
                    {
                        return;
                    }
                    StatCategoryDef verbStatCategory = (__instance.category == ThingCategory.Pawn) ? StatCategoryDefOf.PawnCombat : null;

                    if (ext.damage.harmsHealth)
                    {
                        StatCategoryDef statCat = verbStatCategory ?? StatCategoryDefOf.Weapon_Ranged;
                        StringBuilder stringBuilder2 = new StringBuilder();
                        stringBuilder2.AppendLine("Stat_Thing_Damage_Desc".Translate());
                        stringBuilder2.AppendLine();
                        list.Add(new StatDrawEntry(valueString: (ext.amount * GDUtility.QualityFactor(req.QualityCategory)).ToString(), category: statCat, label: "Damage".Translate(), reportText: stringBuilder2.ToString(), displayPriorityWithinCategory: 5500));
                        if (ext.damage.armorCategory != null)
                        {
                            StringBuilder stringBuilder3 = new StringBuilder();
                            float armorPenetration = ext.penetration * GDUtility.QualityFactor(req.QualityCategory);
                            TaggedString taggedString = "ArmorPenetrationExplanation".Translate();
                            if (stringBuilder3.Length != 0)
                            {
                                taggedString += "\n\n" + stringBuilder3;
                            }

                            list.Add(new StatDrawEntry(statCat, "ArmorPenetration".Translate(), armorPenetration.ToStringPercent(), taggedString, 5400));
                        }

                        float buildingDamageFactor = ext.damage.buildingDamageFactor;
                        float dmgBuildingsImpassable = ext.damage.buildingDamageFactorImpassable;
                        float dmgBuildingsPassable = ext.damage.buildingDamageFactorPassable;
                        if (buildingDamageFactor != 1f)
                        {
                            list.Add(new StatDrawEntry(statCat, "BuildingDamageFactor".Translate(), buildingDamageFactor.ToStringPercent(), "BuildingDamageFactorExplanation".Translate(), 5410));
                        }

                        if (dmgBuildingsImpassable != 1f)
                        {
                            list.Add(new StatDrawEntry(statCat, "BuildingDamageFactorImpassable".Translate(), dmgBuildingsImpassable.ToStringPercent(), "BuildingDamageFactorImpassableExplanation".Translate(), 5420));
                        }

                        if (dmgBuildingsPassable != 1f)
                        {
                            list.Add(new StatDrawEntry(statCat, "BuildingDamageFactorPassable".Translate(), dmgBuildingsPassable.ToStringPercent(), "BuildingDamageFactorPassableExplanation".Translate(), 5430));
                        }
                    }

                    __result = list;
                }
            }
		}
	}

    [HarmonyPatch(typeof(VerbProperties), "get_Ranged")]
    public static class Verb_Ranged_Patch
    {
        public static bool Prefix(VerbProperties __instance, ref bool __result)
        {
            if (typeof(Verb_ThrowArc).IsAssignableFrom(__instance.verbClass))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
