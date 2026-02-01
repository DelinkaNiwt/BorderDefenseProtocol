using RimWorld;
using Verse;

namespace NCLWorm;

public class NCLCallTool_ShiLian : NCLCallTool_Bool
{
	public IntRange delayTick;

	public ResearchProjectDef ResearchProj;

	public override void SecAction()
	{
		Pawn usedBy = windows.usedBy;
		GameConditionManager gameConditionManager = usedBy.Map.GameConditionManager;
		GameConditionDef named = DefDatabase<GameConditionDef>.GetNamed("NCL_WaitWormFight");
		int randomInRange = delayTick.RandomInRange;
		GameCondition cond = GameConditionMaker.MakeCondition(named, randomInRange);
		gameConditionManager.RegisterCondition(cond);
		ChoiceLetter choiceLetter = LetterMaker.MakeLetter(letter, letterText, LetterDefOf.NeutralEvent);
		Find.LetterStack.ReceiveLetter(choiceLetter);
		windows.Close();
	}

	public override AcceptanceReport Canuse()
	{
		if (Current.Game.GetComponent<GameComp_NCLWorm>().inWormWar)
		{
			return "NCLYouInWar".Translate();
		}
		if (windows.usedBy.Map.gameConditionManager.GetActiveCondition(NCLWormDefOf.NCL_WaitWormFight) != null || windows.usedBy.Map.gameConditionManager.GetActiveCondition(NCLWormDefOf.NCL_WaitWorm) != null)
		{
			return "NCLYouWaitWorm".Translate();
		}
		if (Find.ResearchManager.GetProgress(ResearchProj) < ResearchProj.baseCost)
		{
			return "NCLNeedReaearch".Translate(ResearchProj.LabelCap);
		}
		return true;
	}
}
