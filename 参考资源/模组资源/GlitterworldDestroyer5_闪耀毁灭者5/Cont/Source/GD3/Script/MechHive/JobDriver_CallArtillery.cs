using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace GD3
{
	public class JobDriver_CallArtillery : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell).FailOn((Toil to) => !((Building_CommsConsole)to.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing).CanUseCommsNow);
			Toil openComms = ToilMaker.MakeToil("MakeNewToils");
			openComms.initAction = delegate
			{
				Pawn actor = openComms.actor;
				if (((Building_CommsConsole)actor.jobs.curJob.GetTarget(TargetIndex.A).Thing).CanUseCommsNow)
				{
					Dialog_Artillery dialog = new Dialog_Artillery((Building_CommsConsole)actor.jobs.curJob.GetTarget(TargetIndex.A).Thing);
					dialog.soundAmbient = SoundDefOf.RadioComms_Ambience;
					Find.WindowStack.Add(dialog);
				}
			};
			yield return openComms;
		}
	}
}
