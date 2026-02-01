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
	public class CompAbilityEffect_RocketAttack : CompAbilityEffect
	{
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
			Pawn pawn = target.Pawn;
			if (!target.HasThing)
			{
				return false;
			}
			if (!parent.pawn.Flying)
			{
				return false;
			}
			if (target.Thing is Building)
            {
				return true;
            }
			if (pawn != null)
            {
				if (pawn.RaceProps.baseHealthScale > 3)
                {
					return true;
                }
				float armor = pawn.GetStatValue(StatDefOf.ArmorRating_Sharp, true, 100);
				List<Apparel> apparels = pawn.apparel?.WornApparel;
				if (!apparels.NullOrEmpty())
                {
					armor = (float)Math.Max(armor, apparels.Max(a => a.GetStatValue(StatDefOf.ArmorRating_Sharp, true, 100)));
                }
				if (armor > 0.75f)
                {
					return true;
                }
            }
			return false;
		}

		public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
		{
			return true;
		}

		public override bool GizmoDisabled(out string reason)
		{
			reason = null;
			Pawn pawn = this.parent.pawn;
			if (pawn.Faction == Faction.OfPlayer && MechanitorUtility.GetOverseer(pawn) == null)
			{
				reason = "GD.MechanitorNotFound".Translate();
				return true;
			}
			if (pawn.Downed)
			{
				reason = "GD.MechanoidDowned".Translate();
				return true;
			}
			if (!pawn.Flying)
			{
				reason = "GD.NotFlying".Translate();
				return true;
			}
			return false;
		}
	}
}