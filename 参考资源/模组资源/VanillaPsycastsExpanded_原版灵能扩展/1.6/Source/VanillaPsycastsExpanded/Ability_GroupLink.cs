using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_GroupLink : Ability
{
	public override Hediff ApplyHediff(Pawn targetPawn, HediffDef hediffDef, BodyPartRecord bodyPart, int duration, float severity)
	{
		Hediff_GroupLink obj = ((Ability)this).ApplyHediff(targetPawn, hediffDef, bodyPart, duration, severity) as Hediff_GroupLink;
		obj.LinkAllPawnsAround();
		return (Hediff)(object)obj;
	}
}
