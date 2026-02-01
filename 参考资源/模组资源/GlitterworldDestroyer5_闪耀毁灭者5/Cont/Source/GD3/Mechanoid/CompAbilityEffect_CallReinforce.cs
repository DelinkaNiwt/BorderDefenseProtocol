using System;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Collections.Generic;

namespace GD3
{
	public class CompAbilityEffect_CallReinforce : CompAbilityEffect
	{
		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			Pawn pawn = parent.pawn;
			Map map = pawn.Map;
			IntVec3 targ = target.Cell;
			if (!targ.IsValid || pawn == null || map == null)
			{
				return;
			}
			Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(GDDefOf.GD_CallReinforcement);
			if (hediff != null)
            {
				pawn.health.RemoveHediff(hediff);
				GDUtility.CallForReinforcement(targ, map, null, delegate(TargetInfo tar)
                {
					GDDefOf.GDReinforceFlare.PlayOneShot(tar);
				});
			}
		}

		public override void Apply(GlobalTargetInfo target)
		{
			this.Apply(null, null);
		}

		public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
		{
			return true;
		}

		public override bool CanApplyOn(GlobalTargetInfo target)
		{
			return this.CanApplyOn(null, null);
		}

		public override bool AICanTargetNow(LocalTargetInfo target)
		{
			Pawn pawn = parent.pawn;
			if (pawn.Downed)
			{
				return false;
			}
			return true;
		}

		public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
		{
			return true;
		}
	}
}