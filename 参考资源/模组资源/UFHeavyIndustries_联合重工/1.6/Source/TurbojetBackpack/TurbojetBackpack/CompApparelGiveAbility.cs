using Verse;

namespace TurbojetBackpack;

public class CompApparelGiveAbility : ThingComp
{
	public CompProperties_ApparelGiveAbility Props => (CompProperties_ApparelGiveAbility)props;

	public override void Notify_Equipped(Pawn pawn)
	{
		base.Notify_Equipped(pawn);
		if (pawn.abilities != null && Props.abilityDef != null)
		{
			pawn.abilities.GainAbility(Props.abilityDef);
		}
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		base.Notify_Unequipped(pawn);
		if (pawn.abilities != null && Props.abilityDef != null)
		{
			pawn.abilities.RemoveAbility(Props.abilityDef);
		}
	}
}
