using System.Linq;
using RimWorld;
using Verse;

namespace NCLWorm;

public class NCLCallTool_GiveLong : NCLCallTool_Bool
{
	public IntRange delayTick;

	public int ReLongTick;

	public override void SecAction()
	{
		Pawn usedBy = windows.usedBy;
		GameConditionManager gameConditionManager = usedBy.Map.GameConditionManager;
		GameConditionDef named = DefDatabase<GameConditionDef>.GetNamed("NCL_WaitWorm");
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
		if (Current.Game.GetComponent<GameComp_NCLWorm>().ReLongTime > 0)
		{
			return "NCLYouNewWorm".Translate(Current.Game.GetComponent<GameComp_NCLWorm>().ReLongTime.TicksToDays());
		}
		return true;
	}

	public override bool NoCanSee()
	{
		if (Find.CurrentMap == null)
		{
			return true;
		}
		return Find.CurrentMap.mapPawns.AllPawnsSpawned.Any((Pawn x) => x.def.defName == "NCL_MechWorm");
	}
}
