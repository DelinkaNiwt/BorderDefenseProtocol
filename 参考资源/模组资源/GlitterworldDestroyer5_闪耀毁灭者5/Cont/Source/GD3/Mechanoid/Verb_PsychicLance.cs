using System;
using RimWorld;
using Verse.AI;
using Verse;

namespace GD3
{
	public class Verb_PsychicLance : Verb_CastBase
	{
		protected override bool TryCastShot()
		{
			Pawn casterPawn = this.CasterPawn;
			Thing thing = this.currentTarget.Thing;
			if (casterPawn == null || thing == null)
			{
				return false;
			}
			Pawn p = thing as Pawn;
			if (p == null || p.GetStatValue(StatDefOf.PsychicSensitivity) == 0)
            {
				return false;
            }
			foreach (CompTargetEffect compTargetEffect in base.EquipmentSource.GetComps<CompTargetEffect>())
			{
				compTargetEffect.DoEffectOn(casterPawn, thing);
			}
			CompApparelReloadable reloadableCompSource = base.ReloadableCompSource;
			if (reloadableCompSource != null)
			{
				reloadableCompSource.UsedOnce();
			}
			return true;
		}
	}
}
