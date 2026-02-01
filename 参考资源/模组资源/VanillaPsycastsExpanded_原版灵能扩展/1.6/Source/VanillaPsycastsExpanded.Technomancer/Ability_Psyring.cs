using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public class Ability_Psyring : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		Thing thing = targets[0].Thing;
		if (thing != null)
		{
			Find.WindowStack.Add(new Dialog_CreatePsyring(base.pawn, thing, ((Def)(object)base.def).GetModExtension<PsyringExclusionExtension>()?.excludedAbilities));
		}
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (!((Ability)this).ValidateTarget(target, showMessages))
		{
			return false;
		}
		if (!target.HasThing)
		{
			return false;
		}
		if (target.Thing.def != VPE_DefOf.VPE_Eltex)
		{
			if (showMessages)
			{
				Messages.Message("VPE.MustEltex".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		return true;
	}
}
