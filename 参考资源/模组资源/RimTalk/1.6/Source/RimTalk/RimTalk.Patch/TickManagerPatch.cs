using HarmonyLib;
using RimTalk.Data;
using RimTalk.Service;
using RimTalk.Source.Data;
using RimTalk.Util;
using RimWorld;
using Verse;

namespace RimTalk.Patch;

[HarmonyPatch(typeof(TickManager), "DoSingleTick")]
internal static class TickManagerPatch
{
	private const double DisplayInterval = 0.5;

	private const double DebugStatUpdateInterval = 1.0;

	private const int UpdateCacheInterval = 5;

	private static bool _noApiKeyMessageShown;

	private static bool _initialCacheRefresh;

	private static bool _chatHistoryCleared;

	private static int _lastTalkEndTick;

	private static double TalkInterval => Settings.Get().TalkInterval;

	public static void Postfix()
	{
		Counter.Tick++;
		if (IsNow(1.0))
		{
			Stats.Update();
		}
		if (!Settings.Get().IsEnabled || Find.CurrentMap == null)
		{
			return;
		}
		if (!_initialCacheRefresh || IsNow(5.0))
		{
			Cache.Refresh();
			_initialCacheRefresh = true;
		}
		if (IsNow(1.0))
		{
			int currentHour = CommonUtil.GetInGameHour(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile));
			if (currentHour == 0 && !_chatHistoryCleared)
			{
				TalkHistory.Clear();
				_chatHistoryCleared = true;
			}
			else if (currentHour != 0)
			{
				_chatHistoryCleared = false;
			}
		}
		if (!_noApiKeyMessageShown && Settings.Get().GetActiveConfig() == null)
		{
			Messages.Message("RimTalk.TickManager.ApiKeyMissing".Translate(), MessageTypeDefOf.NegativeEvent, historical: false);
			_noApiKeyMessageShown = true;
		}
		if (IsNow(0.5))
		{
			CustomDialogueService.Tick();
			TalkService.DisplayTalk();
		}
		if (IsNow(1.0))
		{
			while (true)
			{
				Pawn pawn = UserRequestPool.GetNextUserRequest();
				if (pawn == null)
				{
					break;
				}
				PawnState pawnState = Cache.Get(pawn);
				if (pawnState == null)
				{
					UserRequestPool.Remove(pawn);
					continue;
				}
				TalkRequest request = pawnState.GetNextTalkRequest();
				if (request == null)
				{
					UserRequestPool.Remove(pawn);
					continue;
				}
				if (!request.TalkType.IsFromUser())
				{
					break;
				}
				if (TalkService.GenerateTalk(request))
				{
					UserRequestPool.Remove(pawn);
				}
				return;
			}
		}
		if (AIService.IsBusy())
		{
			_lastTalkEndTick = GenTicks.TicksGame;
			return;
		}
		int intervalTicks = CommonUtil.GetTicksForDuration(TalkInterval);
		if (intervalTicks <= 0 || GenTicks.TicksGame - _lastTalkEndTick < intervalTicks)
		{
			return;
		}
		Pawn selectedPawn = PawnSelector.SelectNextAvailablePawn();
		if (selectedPawn != null)
		{
			bool talkGenerated = TryGenerateTalkFromPool(selectedPawn);
			if (!talkGenerated)
			{
				PawnState pawnState2 = Cache.Get(selectedPawn);
				if (pawnState2.GetNextTalkRequest() != null)
				{
					talkGenerated = TalkService.GenerateTalk(pawnState2.GetNextTalkRequest());
				}
			}
			if (!talkGenerated)
			{
				TalkRequest talkRequest = new TalkRequest(null, selectedPawn);
				TalkService.GenerateTalk(talkRequest);
			}
		}
		_lastTalkEndTick = GenTicks.TicksGame;
	}

	private static bool TryGenerateTalkFromPool(Pawn pawn)
	{
		if (!pawn.IsFreeNonSlaveColonist || pawn.IsQuestLodger() || TalkRequestPool.IsEmpty || pawn.IsInDanger(includeMentalState: true))
		{
			return false;
		}
		TalkRequest request = TalkRequestPool.GetRequestFromPool(pawn);
		return request != null && TalkService.GenerateTalk(request);
	}

	private static bool IsNow(double interval)
	{
		int ticksForDuration = CommonUtil.GetTicksForDuration(interval);
		if (ticksForDuration == 0)
		{
			return false;
		}
		return Counter.Tick % ticksForDuration == 0;
	}

	public static void Reset()
	{
		_noApiKeyMessageShown = false;
		_initialCacheRefresh = false;
		_lastTalkEndTick = GenTicks.TicksGame;
	}
}
