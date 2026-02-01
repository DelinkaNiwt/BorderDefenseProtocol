using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
	public class CompUseEffect_Check : CompUseEffect
	{
		public Building Pillar
		{
			get
			{
				return this.parent as Building;
			}
		}

		public override AcceptanceReport CanBeUsedBy(Pawn p)
		{
			CompPuzzle comp = this.Pillar.TryGetComp<CompPuzzle>();
			AcceptanceReport result;
			if (comp != null && comp.checking)
			{
				result = "PuzzleChecking".Translate();
			}
			else if (comp != null && comp.ended)
			{
				result = "PuzzleEnded".Translate();
			}
			else
			{
				result = base.CanBeUsedBy(p);
			}
			return result;
		}

		public override void DoEffect(Pawn user)
		{
			base.DoEffect(user);
			CompPuzzle comp = this.Pillar.TryGetComp<CompPuzzle>();
			if (comp != null && !comp.checking)
			{
				comp.checking = !comp.checking;
				GDDefOf.PuzzleTrigger.PlayOneShot(new TargetInfo(Pillar.PositionHeld, Pillar.MapHeld, false));
			}
		}
	}
}
