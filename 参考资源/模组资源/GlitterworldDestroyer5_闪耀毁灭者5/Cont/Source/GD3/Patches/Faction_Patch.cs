using System;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Text;
using System.Collections.Generic;

namespace GD3
{
    [HarmonyPatch(typeof(Faction), "TryAffectGoodwillWith")]
    public static class FactionPatch
    {
        public static bool Prefix(Faction __instance)
        {
            if (__instance != null && __instance.def == GDDefOf.BlackMechanoid && Find.World.GetComponent<MissionComponent>().factionRelationLock)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Building_Sarcophagus), "TryAcceptThing")]
    public static class SarcophagusPatch
    {
        public static bool Prefix(Building_Sarcophagus __instance, Thing thing, ref bool __result, ThingOwner ___innerContainer)
        {
            if (thing.def == GDDefOf.GD_MechCorpse)
            {
                ___innerContainer.TryAdd(thing);
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Building_CorpseCasket), "get_HasCorpse")]
    public static class SarcophagusPatch_Corpse
    {
        public static bool Prefix(Building_CorpseCasket __instance, ref bool __result, ThingOwner ___innerContainer)
        {
            patched = false;
            for (int i = 0; i < ___innerContainer.Count; i++)
            {
                Thing result;
                if ((result = ___innerContainer[i]) != null && result.def == GDDefOf.GD_MechCorpse)
                {
                    __result = true;
                    patched = true;
                    return false;
                }
            }
            return true;
        }

        public static bool patched = false;
    }

    [HarmonyPatch(typeof(Building_Grave), "GetInspectString")]
    public static class SarcophagusPatch_String
    {
        public static bool Prefix(Building_Grave __instance, ref string __result, ThingOwner ___innerContainer)
        {
            if (___innerContainer != null && ___innerContainer.Count > 0 && ___innerContainer[0].def == GDDefOf.GD_MechCorpse)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("GD.ContainCorpse".Translate());

                __result = stringBuilder.ToString();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FocusStrengthOffset_GraveFull), "CanApply")]
    public static class SarcophagusPatch_Focus
    {
        public static bool Prefix(Thing parent, ref bool __result)
        {
            if (parent.Spawned && parent is Building_Grave building_Grave && building_Grave.HasCorpse && SarcophagusPatch_Corpse.patched)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FocusStrengthOffset_GraveCorpseRelationship), "CanApply")]
    public static class SarcophagusPatch_FocusB
    {
        public static bool Prefix(Thing parent, ref bool __result)
        {
            Building_Grave building_Grave = parent as Building_Grave;
            if (parent.Spawned && building_Grave != null && building_Grave.HasCorpse && SarcophagusPatch_Corpse.patched)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
