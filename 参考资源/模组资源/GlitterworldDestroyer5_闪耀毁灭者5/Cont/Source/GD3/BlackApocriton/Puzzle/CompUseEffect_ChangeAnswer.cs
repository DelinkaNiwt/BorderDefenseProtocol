using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
	public class CompUseEffect_ChangeAnswer : CompUseEffect
	{
		public Building Pillar
		{
			get
			{
				return this.parent as Building;
			}
		}

		public override void DoEffect(Pawn user)
		{
			base.DoEffect(user);
			CompPuzzle comp = this.Pillar.TryGetComp<CompPuzzle>();
			if (comp != null)
            {
				comp.state = !comp.state;
				GDDefOf.PuzzleTrigger.PlayOneShot(new TargetInfo(Pillar.PositionHeld, Pillar.MapHeld, false));
			}
		}
	}
}
