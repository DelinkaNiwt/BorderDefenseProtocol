using System.Linq;
using RimWorld;
using Verse;

namespace NCLWorm;

public class NCLCallTool_GiveUpLong : NCLCallTool_Bool
{
	public IntRange delayTick;

	public override void SecAction()
	{
		Pawn pawn = windows.usedBy.Map.mapPawns.AllPawnsSpawned.Where((Pawn x) => x.def.defName == "NCL_MechWorm").RandomElement();
		FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastSkipFlashEntry, 10f);
		pawn.DeSpawn(DestroyMode.Refund);
		ChoiceLetter choiceLetter = LetterMaker.MakeLetter(letter, letterText, LetterDefOf.NeutralEvent);
		Find.LetterStack.ReceiveLetter(choiceLetter);
		windows.Close();
	}

	public override AcceptanceReport Canuse()
	{
		return true;
	}

	public override bool NoCanSee()
	{
		if (Find.CurrentMap == null)
		{
			return true;
		}
		return !Find.CurrentMap.mapPawns.AllPawnsSpawned.Any((Pawn x) => x.def.defName == "NCL_MechWorm");
	}
}
