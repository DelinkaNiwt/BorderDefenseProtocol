using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public class Ability_IncreaseQuality : Ability
{
	private QualityCategory MaxQuality => (QualityCategory)(int)((Ability)this).GetPowerForPawn();

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo globalTargetInfo = targets[i];
			CompQuality compQuality = globalTargetInfo.Thing.GetInnerIfMinified().TryGetComp<CompQuality>();
			if (compQuality == null || (int)compQuality.Quality >= (int)MaxQuality)
			{
				break;
			}
			compQuality.SetQuality(compQuality.Quality + 1, ArtGenerationContext.Colony);
			for (int j = 0; j < 16; j++)
			{
				FleckMaker.ThrowMicroSparks(globalTargetInfo.Thing.TrueCenter(), base.pawn.Map);
			}
		}
	}

	public override float GetPowerForPawn()
	{
		float statValue = base.pawn.GetStatValue(StatDefOf.PsychicSensitivity);
		int num = ((statValue <= 1.2f) ? 3 : ((statValue <= 2.5f) ? 4 : ((!(statValue > 2.5f)) ? 2 : 5)));
		return num;
	}

	public override string GetPowerForPawnDescription()
	{
		return "VPE.MaxQuality".Translate(MaxQuality.GetLabel()).Colorize(Color.cyan);
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (!((Ability)this).ValidateTarget(target, showMessages))
		{
			return false;
		}
		CompQuality compQuality;
		if ((compQuality = target.Thing.GetInnerIfMinified().TryGetComp<CompQuality>()) == null)
		{
			if (showMessages)
			{
				Messages.Message("VPE.MustHaveQuality".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		if ((int)compQuality.Quality >= (int)MaxQuality)
		{
			if (showMessages)
			{
				Messages.Message("VPE.QualityTooHigh".Translate(MaxQuality.GetLabel()), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		return true;
	}
}
