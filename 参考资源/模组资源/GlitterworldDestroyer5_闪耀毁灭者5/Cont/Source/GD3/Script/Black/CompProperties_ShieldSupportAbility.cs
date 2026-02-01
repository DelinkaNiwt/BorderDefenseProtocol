using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace GD3
{
	public class CompProperties_ShieldSupportAbility : CompProperties_AbilityEffect
	{
		public CompProperties_ShieldSupportAbility()
		{
			this.compClass = typeof(CompAbilityEffect_ShieldSupport);
		}
	}
}
