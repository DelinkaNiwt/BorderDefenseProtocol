using System.Linq;
using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_SnapFreeze : Ability
{
	public IntVec3 targetCell;

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (((Def)(object)base.def).GetModExtension<AbilityExtension_Hediff>().targetOnlyEnemies && target.Thing != null && !target.Thing.HostileTo(base.pawn))
		{
			if (showMessages)
			{
				Messages.Message("VFEA.TargetMustBeHostile".Translate(), target.Thing, MessageTypeDefOf.CautionInput, null);
			}
			return false;
		}
		return ((Ability)this).ValidateTarget(target, showMessages);
	}

	public override void ModifyTargets(ref GlobalTargetInfo[] targets)
	{
		targetCell = targets[0].Cell;
		((Ability)this).ModifyTargets(ref targets);
	}

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		Effecter effecter = EffecterDefOf.Skip_Exit.Spawn(targetCell, base.pawn.Map, 3f);
		((Ability)this).AddEffecterToMaintain(effecter, targetCell, 60, (Map)null);
	}

	public override void ApplyHediffs(params GlobalTargetInfo[] targetInfo)
	{
		foreach (GlobalTargetInfo globalTargetInfo in targetInfo)
		{
			ApplyHediff((Ability)(object)this, (LocalTargetInfo)globalTargetInfo);
		}
	}

	public static void ApplyHediff(Ability ability, LocalTargetInfo targetInfo)
	{
		AbilityExtension_Hediff hediffExtension = ((Def)(object)ability.def).GetModExtension<AbilityExtension_Hediff>();
		if (targetInfo.Pawn == null)
		{
			return;
		}
		AbilityExtension_Hediff obj = hediffExtension;
		if (obj == null || !obj.applyAuto)
		{
			return;
		}
		BodyPartRecord partRecord = ((hediffExtension.bodyPartToApply != null) ? ability.pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def == hediffExtension.bodyPartToApply) : null);
		Hediff hediff = HediffMaker.MakeHediff(hediffExtension.hediff, targetInfo.Pawn, partRecord);
		if (hediffExtension.severity > float.Epsilon)
		{
			hediff.Severity = hediffExtension.severity;
		}
		Hediff_Ability val = (Hediff_Ability)(object)((hediff is Hediff_Ability) ? hediff : null);
		if (val != null)
		{
			val.ability = ability;
		}
		int num = ability.GetDurationForPawn();
		float ambientTemperature = targetInfo.Pawn.AmbientTemperature;
		if (ambientTemperature >= 0f)
		{
			num = (int)((float)num * (1f - ambientTemperature / 100f));
		}
		if (hediffExtension.durationMultiplier != null)
		{
			num = (int)((float)num * targetInfo.Pawn.GetStatValue(hediffExtension.durationMultiplier));
		}
		if (hediff is HediffWithComps hediffWithComps)
		{
			foreach (HediffComp comp in hediffWithComps.comps)
			{
				HediffComp_Ability val2 = (HediffComp_Ability)(object)((comp is HediffComp_Ability) ? comp : null);
				if (val2 == null)
				{
					if (comp is HediffComp_Disappears hediffComp_Disappears)
					{
						hediffComp_Disappears.ticksToDisappear = num;
					}
				}
				else
				{
					val2.ability = ability;
				}
			}
		}
		targetInfo.Pawn.health.AddHediff(hediff);
	}
}
