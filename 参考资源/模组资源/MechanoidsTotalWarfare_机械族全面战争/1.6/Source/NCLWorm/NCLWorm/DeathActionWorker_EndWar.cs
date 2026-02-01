using RimWorld;
using Verse;
using Verse.AI.Group;

namespace NCLWorm;

public class DeathActionWorker_EndWar : DeathActionWorker
{
	public override void PawnDied(Corpse corpse, Lord prevLord)
	{
		if (corpse.InnerPawn.Faction.HostileTo(Faction.OfPlayer))
		{
			Current.Game.GetComponent<GameComp_NCLWorm>().inWormWar = false;
			corpse.Map.weatherManager.curWeather = WeatherDefOf.Clear;
			ChoiceLetter choiceLetter = LetterMaker.MakeLetter("NCLWormWarEnd", "NCLWormWarEndDesc", LetterDefOf.NeutralEvent);
			Find.LetterStack.ReceiveLetter(choiceLetter);
		}
		if (corpse.InnerPawn.Faction.IsPlayer)
		{
			Current.Game.GetComponent<GameComp_NCLWorm>().ReLongTime = ((NCLCallTool_GiveLong)DefDatabase<NCLCallDef>.GetNamed("NCLCommsConsole").NCLCallTools.Find((NCLCallTool x) => x is NCLCallTool_GiveLong)).ReLongTick;
		}
	}
}
