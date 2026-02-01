using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_DrainPsyessence : Ability
{
	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (target.Thing is Pawn pawn)
		{
			if (!pawn.Downed)
			{
				if (showMessages)
				{
					Messages.Message("VPE.MustBeDowned".Translate(), pawn, MessageTypeDefOf.CautionInput);
				}
				return false;
			}
			if ((pawn.Psycasts() == null || ((Hediff_Level)(object)pawn.Psycasts()).level < 1) && showMessages)
			{
				Messages.Message("VPE.MustHavePsychicLevel".Translate(), pawn, MessageTypeDefOf.CautionInput);
			}
		}
		return ((Ability)this).ValidateTarget(target, showMessages);
	}

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			Pawn pawn = globalTargetInfo.Thing as Pawn;
			Hediff_PsycastAbilities hediff_PsycastAbilities = pawn.Psycasts();
			Hediff_PsycastAbilities hediff_PsycastAbilities2 = base.pawn.Psycasts();
			int level = ((Hediff_Level)(object)hediff_PsycastAbilities).level;
			hediff_PsycastAbilities.experience = 0f;
			hediff_PsycastAbilities2.GainExperience(hediff_PsycastAbilities.experience);
			float num = 0f;
			for (int j = 0; j < level; j++)
			{
				num += (float)Hediff_PsycastAbilities.ExperienceRequiredForLevel(j);
			}
			hediff_PsycastAbilities2.GainExperience(num);
			pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicAmplifier));
			pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_PsycastAbilityImplant));
			pawn.Kill(null, null);
			pawn.Corpse.GetComp<CompRottable>().RotProgress += 1200000f;
			FilthMaker.TryMakeFilth(pawn.Corpse.Position, pawn.Corpse.Map, ThingDefOf.Filth_CorpseBile, 3);
			MoteBetween obj = (MoteBetween)ThingMaker.MakeThing(VPE_DefOf.VPE_PsycastPsychicEffectTransfer);
			obj.Attach(pawn.Corpse, base.pawn);
			obj.Scale = 1f;
			obj.exactPosition = pawn.Corpse.DrawPos;
			GenSpawn.Spawn(obj, pawn.Corpse.Position, pawn.MapHeld);
		}
		foreach (Faction allFaction in Find.FactionManager.AllFactions)
		{
			if (!allFaction.IsPlayer && !allFaction.defeated)
			{
				Faction.OfPlayer.TryAffectGoodwillWith(allFaction, -15, canSendMessage: true, canSendHostilityLetter: true, HistoryEventDefOf.UsedHarmfulAbility);
			}
		}
	}
}
