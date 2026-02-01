using System.Linq;
using RimTalk.Client;
using RimTalk.Data;
using RimTalk.Error;
using RimTalk.Patch;
using RimTalk.Service;
using Verse;

namespace RimTalk;

public class RimTalk : GameComponent
{
	public RimTalk(Game game)
	{
	}

	public override void StartedNewGame()
	{
		base.StartedNewGame();
		Reset();
	}

	public override void LoadedGame()
	{
		base.LoadedGame();
		Reset();
	}

	public static void Reset(bool soft = false)
	{
		RimTalkSettings settings = Settings.Get();
		if (settings != null)
		{
			settings.CurrentCloudConfigIndex = 0;
		}
		AIErrorHandler.ResetQuotaWarning();
		TickManagerPatch.Reset();
		AIClientFactory.Clear();
		AIService.Clear();
		TalkHistory.Clear();
		PatchThoughtHandlerGetDistinctMoodThoughtGroups.Clear();
		Cache.GetAll().ToList().ForEach(delegate(PawnState pawnState)
		{
			pawnState.IgnoreAllTalkResponses();
		});
		Cache.InitializePlayerPawn();
		UserRequestPool.Clear();
		if (!soft)
		{
			Counter.Tick = 0;
			Cache.Clear();
			Stats.Reset();
			TalkRequestPool.Clear();
			ApiHistory.Clear();
		}
	}
}
