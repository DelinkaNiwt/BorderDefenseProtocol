using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class AbilityExtension_GameCondition : AbilityExtension_AbilityMod
{
	public GameConditionDef gameCondition;

	public FloatRange? durationDays;

	public bool sendLetter;

	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		GameCondition cond = GameConditionMaker.MakeCondition(gameCondition, durationDays.HasValue ? ((int)(durationDays.Value.RandomInRange * 60000f)) : ability.GetDurationForPawn());
		ability.pawn.Map.gameConditionManager.RegisterCondition(cond);
		if (sendLetter)
		{
			ChoiceLetter choiceLetter = LetterMaker.MakeLetter(gameCondition.LabelCap, gameCondition.letterText, LetterDefOf.NegativeEvent, LookTargets.Invalid, null, null, gameCondition.letterHyperlinks);
			Find.LetterStack.ReceiveLetter(choiceLetter);
		}
	}
}
