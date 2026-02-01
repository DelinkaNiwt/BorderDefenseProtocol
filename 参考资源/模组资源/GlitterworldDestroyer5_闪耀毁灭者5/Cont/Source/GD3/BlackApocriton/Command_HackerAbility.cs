using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using System;
using System.Collections.Generic;


namespace GD3
{
	public class Command_HackerAbility : Command_Ability
	{
		public Command_HackerAbility(Ability ability, Pawn pawn) : base(ability, pawn) { }
		protected override void DisabledCheck()
		{
			base.DisabledCheck();
			Pawn pawn = ability.pawn;
			List<Thing> list = pawn.Map.listerThings.ThingsOfDef(GDDefOf.Mech_BlackApocriton);
			if (list.Count > 0)
			{
				disabledReason = "BlackApocritonExist".Translate();
				disabled = true;
			}
		}
	}
}

