using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class AbilityExtension_GiveTrait : AbilityExtension_AbilityMod
{
	public TraitDef trait;

	public int degree;

	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			Pawn pawn = globalTargetInfo.Thing as Pawn;
			Pawn_StoryTracker story = pawn.story;
			bool? obj;
			if (story == null)
			{
				obj = null;
			}
			else
			{
				TraitSet traits = story.traits;
				obj = ((traits != null) ? new bool?(!traits.HasTrait(trait, degree)) : ((bool?)null));
			}
			bool? flag = obj;
			if (flag == true)
			{
				pawn.story.traits.GainTrait(new Trait(trait, degree));
				pawn.needs.AddOrRemoveNeedsAsAppropriate();
			}
		}
	}
}
