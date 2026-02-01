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
	public class CompAbilityEffect_AnnihilatorJumpAndSuspend : CompAbilityEffect
	{
		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			Annihilator pawn = parent.pawn as Annihilator;
			if (pawn != null)
			{
				pawn.JumpTo(target, true);
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
			Annihilator pawn = parent.pawn as Annihilator;
			if (pawn.animation != GDDefOf.Annihilator_Ambient || pawn.Flying)
			{
				return false;
			}
			if (pawn.PositionHeld.DistanceTo(target.Cell) <= Annihilator.IfEnemyClose)
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
			Annihilator pawn = parent.pawn as Annihilator;
			if (pawn.animation != GDDefOf.Annihilator_Ambient || pawn.Flying)
			{
				reason = "GD.AnnihilatorBusy".Translate(pawn.LabelShort);
				return true;
			}
			if (pawn.Dying)
			{
				return true;
			}
			return false;
		}

		public override void DrawEffectPreview(LocalTargetInfo target)
		{
			Map map = Find.CurrentMap;
			if (map != null && target.Cell.InBounds(map))
			{
				List<IntVec3> cells = map.AllCells.Where(c => GenAdj.IsInside(c, target.Cell, Rot4.East, new IntVec2(9, 9))).ToList();
				if (cells.Any())
				{
					GenDraw.DrawFieldEdges(cells);
				}
			}
		}
	}
}