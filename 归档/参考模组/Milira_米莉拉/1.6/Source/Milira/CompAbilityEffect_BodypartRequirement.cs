using System.Linq;
using RimWorld;
using Verse;

namespace Milira;

public class CompAbilityEffect_BodypartRequirement : CompAbilityEffect
{
	public new CompProperties_AbilityBodypartRequirement Props => (CompProperties_AbilityBodypartRequirement)props;

	public override bool GizmoDisabled(out string reason)
	{
		Pawn pawn = parent.pawn;
		BodyPartRecord bodyPartRecord = pawn.RaceProps.body.AllParts.FirstOrDefault((BodyPartRecord bpr) => bpr.def.defName == Props.requiredBodypartdefName);
		if (bodyPartRecord != null && !pawn.health.hediffSet.GetNotMissingParts().Contains(bodyPartRecord))
		{
			reason = "Milira.AbilityDisabled_BodypartMissing".Translate(pawn.Name.ToStringShort, bodyPartRecord.LabelCap);
			return true;
		}
		reason = "";
		return false;
	}
}
