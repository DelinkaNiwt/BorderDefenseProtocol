using Verse;

public class HediffComp_AddAbility : HediffComp
{
	public HediffCompProperties_AddAbility Props => (HediffCompProperties_AddAbility)props;

	public override void CompPostMake()
	{
		base.CompPostMake();
		AddAbility();
	}

	public override void CompPostPostRemoved()
	{
		base.CompPostPostRemoved();
		RemoveAbility();
	}

	private void AddAbility()
	{
		if (parent.pawn.abilities != null && Props.abilityDef != null)
		{
			parent.pawn.abilities.GainAbility(Props.abilityDef);
		}
	}

	private void RemoveAbility()
	{
		if (parent.pawn.abilities != null && Props.abilityDef != null)
		{
			parent.pawn.abilities.RemoveAbility(Props.abilityDef);
		}
	}
}
