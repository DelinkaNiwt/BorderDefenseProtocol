using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_GoodwillImpact : Ability
{
	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (target.Thing is Pawn pawn && (pawn.HostileTo(base.pawn) || pawn.Faction == base.pawn.Faction || pawn.Faction == null))
		{
			if (showMessages)
			{
				Messages.Message("VPE.MustBeAllyOrNeutral".Translate(), pawn, MessageTypeDefOf.CautionInput);
			}
			return false;
		}
		return ((Ability)this).ValidateTarget(target, showMessages);
	}

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			Pawn obj = globalTargetInfo.Thing as Pawn;
			int goodwillChange = (int)Mathf.Max(10f, base.pawn.GetStatValue(StatDefOf.PsychicSensitivity) * 100f - 100f);
			obj.Faction.TryAffectGoodwillWith(base.pawn.Faction, goodwillChange);
		}
	}
}
