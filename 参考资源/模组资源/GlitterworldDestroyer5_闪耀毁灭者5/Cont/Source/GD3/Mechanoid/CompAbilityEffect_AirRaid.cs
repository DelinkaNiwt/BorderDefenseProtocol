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
	public class CompAbilityEffect_AirRaid : CompAbilityEffect
	{
		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			MechMosquito pawn = parent.pawn as MechMosquito;
			Map map = pawn.Map;
			IntVec3 targ = target.Cell;
			if (!targ.IsValid || pawn == null || map == null)
            {
				return;
            }
			Vector3 distance = targ.ToVector3Shifted() - pawn.PositionHeld.ToVector3Shifted();
			Vector3 direction = distance.normalized * 0.8f;

			Vector3 destination = targ.ToVector3Shifted();
			Vector3 tmp = destination;
			for (int i = 0; i < 10; i++)
            {
				tmp += direction;
				IntVec3 tmpInt = tmp.ToIntVec3();
				if (tmpInt == targ)
                {
					continue;
                }
				if (tmpInt.InBounds(map) && tmpInt.Walkable(map))
                {
					destination = tmp;
					if ((destination - targ.ToVector3Shifted()).magnitude >= distance.magnitude)
                    {
						break;
                    }
                }
                else
                {
					break;
                }
            }

			List<IntVec3> cells = new List<IntVec3>();
			Vector3 tmp2 = pawn.PositionHeld.ToVector3Shifted();
			for (int i = 0; i < 18; i++)
            {
				tmp2 += direction;
				IntVec3 tmpInt2 = tmp2.ToIntVec3();
				if (cells.Contains(tmpInt2))
                {
					continue;
                }
				cells.Add(tmpInt2);
				if (tmpInt2 == destination.ToIntVec3())
                {
					break;
                }
			}
			
			pawn.dest = destination.ToIntVec3();
			pawn.airRaidTicker = 0;
			pawn.cells = cells;

			Job job = JobMaker.MakeJob(JobDefOf.Wait_Combat, pawn.PositionHeld);
			job.overrideFacing = pawn.GetRot(destination);
			job.forceMaintainFacing = true;
			pawn.jobs.TryTakeOrderedJob(job);
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
			if (!pawn.Flying)
			{
				return false;
			}
			return true;
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