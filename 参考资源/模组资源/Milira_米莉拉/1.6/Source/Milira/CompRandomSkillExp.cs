using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Milira;

public class CompRandomSkillExp : CompUsable
{
	public override void PostExposeData()
	{
		base.PostExposeData();
	}

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
	}

	public override void UsedBy(Pawn pawn)
	{
		base.UsedBy(pawn);
		CompProperties_RandomSkillExp compProperties_RandomSkillExp = (CompProperties_RandomSkillExp)props;
		if (ModLister.RoyaltyInstalled && pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("PsychicAmplifier")) is Hediff_Psylink { level: <5 } hediff_Psylink)
		{
			hediff_Psylink.ChangeLevel(1);
			Messages.Message("PsychicAmplifierLevelIncreased".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent);
			parent.Destroy();
			return;
		}
		float value = Rand.Value;
		if (value < 0.9f)
		{
			List<SkillRecord> list = pawn.skills.skills.Where((SkillRecord skill) => skill.Level < 16).ToList();
			List<SkillRecord> source = ((list.Count > 0) ? list : pawn.skills.skills);
			SkillRecord skillRecord = source.RandomElement();
			skillRecord.Learn(compProperties_RandomSkillExp.expPoints, direct: true);
			Messages.Message("SkillExpGained".Translate(pawn.LabelShort, skillRecord.def.LabelCap, compProperties_RandomSkillExp.expPoints), pawn, MessageTypeDefOf.PositiveEvent);
		}
		else if (Rand.Value < compProperties_RandomSkillExp.interestChance)
		{
			List<SkillRecord> list2 = pawn.skills.skills.Where((SkillRecord skill) => skill.passion != Passion.Major).ToList();
			if (list2.Count > 0)
			{
				SkillRecord skillRecord2 = list2.RandomElement();
				skillRecord2.passion = ((skillRecord2.passion == Passion.None) ? Passion.Minor : Passion.Major);
				Messages.Message("SkillInterestGained".Translate(pawn.LabelShort, skillRecord2.def.LabelCap), pawn, MessageTypeDefOf.PositiveEvent);
			}
		}
		MiliraGameComponent_OverallControl component = Current.Game.GetComponent<MiliraGameComponent_OverallControl>();
		component.miliraThreatPoint += 10;
		parent.Destroy();
	}
}
