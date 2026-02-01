using System.Collections.Generic;
using System.Linq;
using RimTalk.Data;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimTalk.Properties;

public class JobDriver_ApplyVocalLink : JobDriver
{
	private Pawn Target => (Pawn)job.targetA.Thing;

	private Thing Item => job.targetB.Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Item, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
		yield return Toils_Haul.StartCarryThing(TargetIndex.B);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
		Toil wait = Toils_General.WaitWith(TargetIndex.A, 300, useProgressBar: false, maintainPosture: true, maintainSleep: false, TargetIndex.A);
		wait.WithProgressBarToilDelay(TargetIndex.A);
		wait.FailOnDespawnedOrNull(TargetIndex.A);
		wait.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
		yield return wait;
		yield return Toils_General.Do(Install);
	}

	private void Install()
	{
		CompTargetEffect_InstallVocalLink comp = Item.TryGetComp<CompTargetEffect_InstallVocalLink>();
		if (comp != null)
		{
			BodyPartRecord bodyPart = (Target.RaceProps.IsMechanoid ? (Target.health.hediffSet.GetNotMissingParts().FirstOrDefault(delegate(BodyPartRecord x)
			{
				string defName = x.def.defName;
				return (defName == "ArtificialBrain" || defName == "MechanicalHead") ? true : false;
			}) ?? Target.RaceProps.body.corePart) : Target.health.hediffSet.GetBrain());
			Target.health.AddHediff(comp.Props.hediffDef, bodyPart);
			comp.Props.soundOnUsed?.PlayOneShot(new TargetInfo(Target.Position, Target.Map));
			Item.SplitOff(1).Destroy();
			string prompt = "You have just gained the ability to speak due to a mysterious catalyst.";
			Cache.Get(Target)?.AddTalkRequest(prompt);
		}
	}
}
