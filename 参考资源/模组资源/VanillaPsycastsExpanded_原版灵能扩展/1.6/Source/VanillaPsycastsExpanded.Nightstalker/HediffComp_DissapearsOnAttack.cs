using Verse;

namespace VanillaPsycastsExpanded.Nightstalker;

public class HediffComp_DissapearsOnAttack : HediffComp
{
	public override bool CompShouldRemove
	{
		get
		{
			Stance stance = base.Pawn?.stances?.curStance;
			if (stance is Stance_Warmup stance_Warmup)
			{
				if (stance_Warmup.ticksLeft <= 1)
				{
					Verb verb = stance_Warmup.verb;
					if (verb != null)
					{
						VerbProperties verbProps = verb.verbProps;
						if (verbProps != null && verbProps.violent)
						{
							goto IL_007f;
						}
					}
				}
			}
			else if (stance is Stance_Cooldown { verb: { verbProps: { violent: not false } } })
			{
				goto IL_007f;
			}
			return false;
			IL_007f:
			return true;
		}
	}
}
