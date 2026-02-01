using System;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GD3
{
	[HarmonyPatch(typeof(StunHandler), "Notify_DamageApplied")]
	public static class EMP_Patch
	{
		public static bool Prefix(StunHandler __instance, DamageInfo dinfo)
		{
            Building_TurretGun attacker = dinfo.Instigator as Building_TurretGun;
            Pawn aim = __instance.parent as Pawn;
            Building_TurretGun aimBuilding = __instance.parent as Building_TurretGun;
			if (dinfo.Def == DamageDefOf.EMP && attacker != null && (aim != null || aimBuilding != null) && attacker.gun.def.defName == "Gun_EMPArtilleryTurret")
			{
				if (Find.ResearchManager.GetProgress(GDDefOf.GD3_GiantCluster_ArtilleryA) == GDDefOf.GD3_GiantCluster_ArtilleryA.baseCost)
                {
                    if (aim != null && aim.Faction != null)
                    {
                        if (attacker.Faction != null && !attacker.Faction.HostileTo(aim.Faction))
                        {
                            return false;
                        }
                    }
                    if (aimBuilding != null && aimBuilding.Faction != null)
                    {
                        if (attacker.Faction != null && !attacker.Faction.HostileTo(aimBuilding.Faction))
                        {
                            return false;
                        }
                    }
                }
                if (aim != null && Find.ResearchManager.GetProgress(GDDefOf.GD3_GiantCluster_ArtilleryB) == GDDefOf.GD3_GiantCluster_ArtilleryB.baseCost)
                {
                    if (aim.Faction != null && attacker.Faction.HostileTo(aim.Faction))
                    {
                        EMP_Patch.DamageVictim(aim, attacker);
                    }
                }
            }
            return true;
		}
        private static void DamageVictim(Pawn victim, Building_TurretGun attacker)
        {
            if (victim.Dead)
            {
                return;
            }
            HediffSet hediffSet = victim.health.hediffSet;
            IEnumerable<BodyPartRecord> source = from x in HittablePartsViolence(hediffSet)
                                                 where !victim.health.hediffSet.hediffs.Any((Hediff y) => y.Part == x && y.CurStage != null && y.CurStage.partEfficiencyOffset < 0f)
                                                 select x;
            BodyPartRecord bodyPartRecord = source.RandomElementByWeight((BodyPartRecord x) => x.coverageAbs);
            if (bodyPartRecord == null)
            {
                return;
            }
            int maxHitPoints = bodyPartRecord.def.hitPoints;
            float num = (float)Rand.RangeInclusive(20, 40);
            if (victim.def.defName == "Mech_Diabolus")
            {
                num *= 1.5f;
            }
            victim.TakeDamage(new DamageInfo(DamageDefOf.Flame, (float)num, 2f, 0f, attacker, bodyPartRecord, attacker.gun.def, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true));
        }
        private static IEnumerable<BodyPartRecord> HittablePartsViolence(HediffSet bodyModel)
        {
            return from x in bodyModel.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null)
                   where x.depth == BodyPartDepth.Outside || (x.depth == BodyPartDepth.Inside && x.def.IsSolid(x, bodyModel.hediffs))
                   select x;
        }
    }
}
