using System;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
using UnityEngine;

namespace GD3
{
	public class CompAbilityEffect_PocketThunder : CompAbilityEffect
	{
		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			BlackApocriton pawn = parent.pawn as BlackApocriton;
			if (pawn != null)
            {
				pawn.SetCaneAttack(target);
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

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
			BlackApocriton blackApocriton = parent.pawn as BlackApocriton;
			if (blackApocriton != null && !blackApocriton.CanUsePsychicAttack)
			{
				return false;
			}
			if (blackApocriton.Drawer.renderer.HasAnimation)
            {
				return false;
            }
            return true;
        }

        public override bool CanApplyOn(GlobalTargetInfo target)
		{
			return this.CanApplyOn(null, null);
		}

		public override bool GizmoDisabled(out string reason)
		{
			reason = null;
			return false;
		}
	}
}