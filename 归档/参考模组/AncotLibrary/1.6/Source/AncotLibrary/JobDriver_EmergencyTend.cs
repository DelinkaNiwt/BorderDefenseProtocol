using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobDriver_EmergencyTend : JobDriver
{
	private int baseHealTick = 300;

	private float baseTendQuality = 0.4f;

	private Pawn targetPawn => (Pawn)job.GetTarget(TargetIndex.B).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(base.TargetThingB, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		int healSpeed = (int)((float)baseHealTick / pawn.GetStatValue(StatDefOf.MedicalTendSpeed));
		if (targetPawn != pawn)
		{
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.B);
			Toil toil = Toils_General.WaitWith(TargetIndex.B, healSpeed, useProgressBar: true, maintainPosture: true);
			toil.WithProgressBarToilDelay(TargetIndex.B);
			toil.FailOnDespawnedOrNull(TargetIndex.B);
			toil.FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch);
			toil.handlingFacing = true;
			yield return toil;
		}
		else
		{
			Toil toil2 = Toils_General.Wait(healSpeed);
			toil2.WithProgressBarToilDelay(TargetIndex.A);
			toil2.FailOnDespawnedOrNull(TargetIndex.A);
			yield return toil2;
		}
		yield return Toils_General.Do(Tend);
	}

	private void Tend()
	{
		List<Hediff> hediffs = targetPawn.health.hediffSet.hediffs;
		for (int i = 0; i < 3; i++)
		{
			for (int num = hediffs.Count - 1; num >= 0; num--)
			{
				if ((hediffs[num] is Hediff_Injury || hediffs[num] is Hediff_MissingPart) && hediffs[num].TendableNow())
				{
					float num2 = baseTendQuality * pawn.GetStatValue(StatDefOf.MedicalTendQuality);
					float num3 = Mathf.Max(Rand.Range(num2 - 0.1f, num2 + 0.1f), 0.01f);
					hediffs[num].Tended(num3, num3, 1);
					Vector3 loc = targetPawn.DrawPos + new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
					MoteMaker.ThrowText(loc, targetPawn.Map, "Ancot.TextMote_TendQuality".Translate(num3.ToStringPercent()), Color.white, 1.9f);
					break;
				}
			}
		}
	}
}
