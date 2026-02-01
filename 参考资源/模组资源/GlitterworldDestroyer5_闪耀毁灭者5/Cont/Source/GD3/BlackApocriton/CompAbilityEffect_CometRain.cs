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
	public class CompAbilityEffect_CometRain : CompAbilityEffect
	{
		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			BlackApocriton blackApocriton = parent.pawn as BlackApocriton;
			if (blackApocriton != null)
            {
				blackApocriton.SetCometRain();
            }
		}

		public override void Apply(GlobalTargetInfo target)
		{
			this.Apply(null, null);
		}

		public override bool AICanTargetNow(LocalTargetInfo target)
		{
			BlackApocriton blackApocriton = parent.pawn as BlackApocriton;
			if (blackApocriton != null && !blackApocriton.CanUsePsychicAttack)
			{
				return false;
			}
			return true;
		}

		public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
		{
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