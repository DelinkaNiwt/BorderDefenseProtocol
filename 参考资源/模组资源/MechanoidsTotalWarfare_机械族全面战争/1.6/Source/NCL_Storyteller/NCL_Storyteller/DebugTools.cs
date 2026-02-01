using LudeonTK;

namespace NCL_Storyteller;

public static class DebugTools
{
	[DebugAction("DeadLoop", "DeadLoop", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void DeadLoop()
	{
		DeadLoop();
	}
}
