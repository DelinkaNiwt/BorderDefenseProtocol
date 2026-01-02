using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAvoidAllyDamage : ThingComp
{
	public CompProperties_AvoidAllyDamage Props => (CompProperties_AvoidAllyDamage)props;

	protected Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				if (parent is Pawn result)
				{
					return result;
				}
				return null;
			}
			return wearer;
		}
	}

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		if (dinfo.Instigator != null && dinfo.Instigator.Faction != null && dinfo.Instigator.Faction == PawnOwner.Faction)
		{
			absorbed = true;
		}
		else
		{
			absorbed = false;
		}
	}
}
