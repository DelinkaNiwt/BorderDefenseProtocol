using HarmonyLib;
using RimWorld;
using Verse;

namespace GD3
{
    [HarmonyPatch(typeof(Thing), "PreApplyDamage")]
    public static class Damage_Patch
    {
        private static float MechDamageFactor => GDSettings.mechDamageRate / 100f;

        private static float TurretDamageFactor => GDSettings.turretDamageRate / 100f;

        public static void Postfix(Thing __instance, ref DamageInfo dinfo)
        {
            Pawn pawn = dinfo.Instigator as Pawn;
            Building building = dinfo.Instigator as Building;
            if (pawn != null && pawn.RaceProps.IsMechanoid)
            {
                dinfo.SetAmount(dinfo.Amount * MechDamageFactor);
            }
            else if (building != null && Faction.OfMechanoids != null && building.Faction == Faction.OfMechanoids)
            {
                dinfo.SetAmount(dinfo.Amount * TurretDamageFactor);
            }
        }
    }
}
