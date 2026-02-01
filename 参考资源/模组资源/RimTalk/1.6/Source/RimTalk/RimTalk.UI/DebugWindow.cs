using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimTalk.Data;
using RimTalk.Service;
using RimTalk.Source.Data;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimTalk.UI;

public class DebugWindow : Window
{
	private enum DebugViewMode
	{
		MainTable,
		GroupedByPawn,
		ActiveRequests
	}

	private const float RowHeight = 22f;

	private const float FilterBarHeight = 30f;

	private const float HeaderHeight = 22f;

	private const float ColumnPadding = 10f;

	private const float TimestampColumnWidth = 65f;

	private const float PawnColumnWidth = 80f;

	private const float TimeColumnWidth = 55f;

	private const float TokensColumnWidth = 50f;

	private const float StateColumnWidth = 65f;

	private const float InteractionTypeColumnWidth = 50f;

	private const float ARTimeWidth = 65f;

	private const float ARInitiatorWidth = 80f;

	private const float ARRecipientWidth = 80f;

	private const float ARTypeWidth = 60f;

	private const float ARElapsedWidth = 60f;

	private const float ARStatusWidth = 60f;

	private const float GroupedPawnNameWidth = 80f;

	private const float GroupedRequestsWidth = 60f;

	private const float GroupedLastTalkWidth = 60f;

	private const float GroupedChattinessWidth = 65f;

	private const float GroupedExpandIconWidth = 25f;

	private const float GroupedStatusWidth = 60f;

	private readonly string _generating = "RimTalk.DebugWindow.Generating".Translate();

	private Vector2 _tableScrollPosition;

	private Vector2 _activeRequestsScrollPosition;

	private Vector2 _detailsScrollPosition;

	private bool _stickToBottom = true;

	private string _aiStatus;

	private long _totalCalls;

	private long _totalTokens;

	private double _avgCallsPerMin;

	private double _avgTokensPerMin;

	private double _avgTokensPerCall;

	private List<PawnState> _pawnStates;

	private List<ApiLog> _requests;

	private List<TalkRequest> _cachedActiveViewList = new List<TalkRequest>();

	private readonly Dictionary<string, List<ApiLog>> _talkLogsByPawn = new Dictionary<string, List<ApiLog>>();

	private int _maxRows;

	private string _pawnFilter;

	private string _textSearch;

	private ApiLog.State _stateFilter;

	private RequestStatus? _activeRequestStatusFilter;

	private ApiLog _selectedLog;

	private Guid _selectedRequestIdForTemp = Guid.Empty;

	private string _tempResponse;

	private string _tempPromptSegmentsText;

	private List<PromptMessageSegment> _tempPromptSegments = new List<PromptMessageSegment>();

	private readonly HashSet<int> _expandedPromptSegmentIndices = new HashSet<int>();

	private DebugViewMode _viewMode;

	private string _sortColumn;

	private bool _sortAscending;

	private readonly List<string> _expandedPawns;

	private const string ControlNamePawnFilter = "PawnFilterField";

	private const string ControlNameTextSearch = "TextSearchField";

	private const string ControlNameDetailResponse = "DetailResponseField";

	private const string ControlNameDetailMessages = "DetailMessagesField";

	private GUIStyle _contextStyle;

	private GUIStyle _monoTinyStyle;

	public override Vector2 InitialSize => new Vector2(1100f, 600f);

	public DebugWindow()
	{
		doCloseX = true;
		draggable = true;
		resizeable = true;
		absorbInputAroundWindow = false;
		closeOnClickedOutside = false;
		closeOnAccept = false;
		closeOnCancel = true;
		preventCameraMotion = false;
		RimTalkSettings settings = Settings.Get();
		_viewMode = DebugViewMode.MainTable;
		_sortColumn = settings.DebugSortColumn;
		_sortAscending = settings.DebugSortAscending;
		_expandedPawns = new List<string>();
		_maxRows = 500;
		_pawnFilter = string.Empty;
		_textSearch = string.Empty;
		_stateFilter = ApiLog.State.None;
		_activeRequestStatusFilter = null;
	}

	public override void PreClose()
	{
		base.PreClose();
		RimTalkSettings settings = Settings.Get();
		settings.DebugSortColumn = _sortColumn;
		settings.DebugSortAscending = _sortAscending;
		settings.Write();
	}

	private void InitializeContextStyle()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected O, but got Unknown
		if (_contextStyle == null)
		{
			GUIStyle val = new GUIStyle(Text.fontStyles[0])
			{
				fontSize = 12,
				alignment = TextAnchor.UpperLeft
			};
			val.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
			_contextStyle = val;
		}
		if (_monoTinyStyle == null)
		{
			GUIStyle val2 = new GUIStyle(Text.fontStyles[0])
			{
				fontSize = 12,
				wordWrap = true,
				alignment = TextAnchor.UpperLeft
			};
			val2.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
			_monoTinyStyle = val2;
		}
	}

	public override void DoWindowContents(Rect inRect)
	{
		HandleGlobalClicks(inRect);
		UpdateData();
		float contentHeight = inRect.height - 150f - 10f;
		float leftWidth = inRect.width * 0.6f - 5f;
		float rightWidth = inRect.width * 0.4f - 5f;
		Rect leftPaneRect = new Rect(inRect.x, inRect.y, leftWidth, contentHeight);
		Rect detailsRect = new Rect(leftPaneRect.xMax + 10f, inRect.y, rightWidth, contentHeight);
		DrawLeftPane(leftPaneRect);
		DrawDetailsPanel(detailsRect);
		Rect bottomRect = new Rect(inRect.x, leftPaneRect.yMax + 10f, inRect.width, 150f);
		float graphWidth = bottomRect.width * 0.5f;
		float statsWidth = bottomRect.width * 0.3f;
		float actionsWidth = bottomRect.width * 0.2f - 20f;
		Rect graphRect = new Rect(bottomRect.x, bottomRect.y, graphWidth, bottomRect.height);
		Rect statsRect = new Rect(graphRect.xMax + 10f, bottomRect.y, statsWidth, bottomRect.height);
		Rect actionsRect = new Rect(statsRect.xMax + 10f, bottomRect.y, actionsWidth, bottomRect.height);
		DrawGraph(graphRect);
		DrawStatsSection(statsRect);
		DrawBottomActions(actionsRect);
	}

	private void UpdateData()
	{
		RimTalkSettings settings = Settings.Get();
		if (!settings.IsEnabled)
		{
			_aiStatus = "RimTalk.DebugWindow.StatusDisabled".Translate();
		}
		else
		{
			_aiStatus = (AIService.IsBusy() ? "RimTalk.DebugWindow.StatusProcessing".Translate() : "RimTalk.DebugWindow.StatusIdle".Translate());
		}
		_totalCalls = Stats.TotalCalls;
		_totalTokens = Stats.TotalTokens;
		_avgCallsPerMin = Stats.AvgCallsPerMinute;
		_avgTokensPerMin = Stats.AvgTokensPerMinute;
		_avgTokensPerCall = Stats.AvgTokensPerCall;
		_pawnStates = global::RimTalk.Data.Cache.GetAll().ToList();
		IEnumerable<ApiLog> allHistory = ApiHistory.GetAll();
		IEnumerable<ApiLog> filtered = ApplyFilters(allHistory);
		ApiLog[] apiLogs = (filtered as ApiLog[]) ?? filtered.ToArray();
		int count = apiLogs.Count();
		_requests = ((count > _maxRows) ? apiLogs.Skip(count - _maxRows).ToList() : apiLogs.ToList());
		_talkLogsByPawn.Clear();
		foreach (ApiLog request in _requests.Where((ApiLog r) => r.Name != null))
		{
			if (!_talkLogsByPawn.ContainsKey(request.Name))
			{
				_talkLogsByPawn[request.Name] = new List<ApiLog>();
			}
			_talkLogsByPawn[request.Name].Add(request);
		}
		if (_selectedLog != null && _requests.All((ApiLog r) => r.Id != _selectedLog.Id))
		{
			_selectedLog = null;
			_selectedRequestIdForTemp = Guid.Empty;
		}
		List<TalkRequest> tempActiveList = new List<TalkRequest>();
		tempActiveList.AddRange(TalkRequestPool.GetAllActive());
		tempActiveList.AddRange(TalkRequestPool.GetHistory());
		if (_pawnStates != null)
		{
			tempActiveList.AddRange(_pawnStates.SelectMany((PawnState state) => state.TalkRequests));
		}
		IEnumerable<TalkRequest> q = tempActiveList;
		if (!string.IsNullOrWhiteSpace(_pawnFilter))
		{
			string needle = _pawnFilter.Trim();
			q = q.Where((TalkRequest r) => (r.Initiator?.LabelShort ?? "").IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 || (r.Recipient?.LabelShort ?? "").IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
		}
		if (!string.IsNullOrWhiteSpace(_textSearch))
		{
			string needle2 = _textSearch.Trim();
			q = q.Where((TalkRequest r) => (r.Prompt ?? "").IndexOf(needle2, StringComparison.OrdinalIgnoreCase) >= 0);
		}
		if (_activeRequestStatusFilter.HasValue)
		{
			q = q.Where((TalkRequest r) => r.Status == _activeRequestStatusFilter.Value);
		}
		_cachedActiveViewList = q.ToList();
		_cachedActiveViewList.Sort((TalkRequest a, TalkRequest b) => a.CreatedTime.CompareTo(b.CreatedTime));
	}

	private void DrawLeftPane(Rect rect)
	{
		Rect filterRect = new Rect(rect.x, rect.y, rect.width, 30f);
		DrawInternalFilterBar(filterRect);
		Rect tableRect = new Rect(rect.x, rect.y + 30f, rect.width, rect.height - 30f);
		switch (_viewMode)
		{
		case DebugViewMode.ActiveRequests:
			DrawActiveRequestsTable(tableRect);
			break;
		case DebugViewMode.GroupedByPawn:
			DrawGroupedPawnTable(tableRect);
			break;
		default:
			DrawConsoleTable(tableRect);
			break;
		}
	}

	private void DrawInternalFilterBar(Rect rect)
	{
		float y = rect.y + 3f;
		float height = 24f;
		float gap = 5f;
		float startX = rect.x;
		float viewDropdownWidth = 90f;
		float statusWidth = 100f;
		float limitWidth = 90f;
		float totalFixedSpace = viewDropdownWidth + statusWidth + limitWidth + 4f * gap;
		float flexSpace = rect.width - totalFixedSpace;
		if (flexSpace < 50f)
		{
			flexSpace = 50f;
		}
		float pawnFilterWidth = flexSpace * 0.35f;
		float textSearchWidth = flexSpace * 0.65f;
		float currentX = startX;
		Rect viewBtnRect = new Rect(currentX, y, viewDropdownWidth, height);
		DebugViewMode viewMode = _viewMode;
		if (1 == 0)
		{
		}
		string text = viewMode switch
		{
			DebugViewMode.MainTable => "RimTalk.DebugWindow.ViewByTime".Translate(), 
			DebugViewMode.GroupedByPawn => "RimTalk.DebugWindow.ViewByPawn".Translate(), 
			DebugViewMode.ActiveRequests => "RimTalk.DebugWindow.ViewTalkRequests".Translate(), 
			_ => _viewMode.ToString(), 
		};
		if (1 == 0)
		{
		}
		string viewLabel = text;
		if (Widgets.ButtonText(viewBtnRect, viewLabel))
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>
			{
				new FloatMenuOption("RimTalk.DebugWindow.ViewByTime".Translate(), delegate
				{
					_viewMode = DebugViewMode.MainTable;
				}),
				new FloatMenuOption("RimTalk.DebugWindow.ViewByPawn".Translate(), delegate
				{
					_viewMode = DebugViewMode.GroupedByPawn;
				}),
				new FloatMenuOption("RimTalk.DebugWindow.ViewTalkRequests".Translate(), delegate
				{
					_viewMode = DebugViewMode.ActiveRequests;
				})
			};
			Find.WindowStack.Add(new FloatMenu(options));
		}
		currentX += viewDropdownWidth + gap;
		_pawnFilter = DrawSearchField(new Rect(currentX, y, pawnFilterWidth, height), _pawnFilter, "RimTalk.DebugWindow.FilterPawn".Translate(), "PawnFilterField");
		currentX += pawnFilterWidth + gap;
		_textSearch = DrawSearchField(new Rect(currentX, y, textSearchWidth, height), _textSearch, "RimTalk.DebugWindow.Search".Translate(), "TextSearchField");
		currentX += textSearchWidth + gap;
		Rect stateBtnRect = new Rect(currentX, y, statusWidth, height);
		if (_viewMode == DebugViewMode.ActiveRequests)
		{
			string label = (_activeRequestStatusFilter.HasValue ? $"RimTalk.DebugWindow.State{_activeRequestStatusFilter.Value}".Translate() : "RimTalk.DebugWindow.StateAll".Translate());
			if (Widgets.ButtonText(stateBtnRect, label))
			{
				List<FloatMenuOption> options2 = new List<FloatMenuOption>
				{
					new FloatMenuOption("RimTalk.DebugWindow.StateAll".Translate(), delegate
					{
						_activeRequestStatusFilter = null;
					})
				};
				options2.AddRange(from RequestStatus s in Enum.GetValues(typeof(RequestStatus))
					select new FloatMenuOption($"RimTalk.DebugWindow.State{s}".Translate(), delegate
					{
						_activeRequestStatusFilter = s;
					}));
				Find.WindowStack.Add(new FloatMenu(options2));
			}
		}
		else if (Widgets.ButtonText(stateBtnRect, _stateFilter.GetLabel()))
		{
			List<FloatMenuOption> options3 = (from ApiLog.State filter in Enum.GetValues(typeof(ApiLog.State))
				select new FloatMenuOption(filter.GetLabel(), delegate
				{
					_stateFilter = filter;
				})).ToList();
			Find.WindowStack.Add(new FloatMenu(options3));
		}
		currentX += statusWidth + gap;
		Rect limitBtnRect = new Rect(currentX, y, limitWidth, height);
		string lastPrefix = "RimTalk.DebugWindow.Last".Translate();
		if (Widgets.ButtonText(limitBtnRect, $"{lastPrefix} {_maxRows}"))
		{
			List<FloatMenuOption> options4 = new List<FloatMenuOption>
			{
				new FloatMenuOption(lastPrefix + " 200", delegate
				{
					_maxRows = 200;
				}),
				new FloatMenuOption(lastPrefix + " 500", delegate
				{
					_maxRows = 500;
				}),
				new FloatMenuOption(lastPrefix + " 1000", delegate
				{
					_maxRows = 1000;
				}),
				new FloatMenuOption(lastPrefix + " 2000", delegate
				{
					_maxRows = 2000;
				})
			};
			Find.WindowStack.Add(new FloatMenu(options4));
		}
	}

	private void DrawActiveRequestsTable(Rect rect)
	{
		float fixedWidth = 465f;
		float promptWidth = Mathf.Max(50f, rect.width - fixedWidth - 16f);
		DrawActiveRequestHeader(new Rect(rect.x, rect.y, rect.width, 22f), promptWidth);
		Rect scrollRect = new Rect(rect.x, rect.y + 22f, rect.width, rect.height - 22f);
		float viewWidth = scrollRect.width - 16f;
		float viewHeight = (float)_cachedActiveViewList.Count * 22f;
		Rect viewRect = new Rect(0f, 0f, viewWidth, viewHeight);
		float maxScroll = Mathf.Max(0f, viewHeight - scrollRect.height);
		if (_stickToBottom)
		{
			_activeRequestsScrollPosition.y = maxScroll;
		}
		Widgets.BeginScrollView(scrollRect, ref _activeRequestsScrollPosition, viewRect);
		if (_stickToBottom && _activeRequestsScrollPosition.y < maxScroll - 1f)
		{
			_stickToBottom = false;
		}
		for (int i = 0; i < _cachedActiveViewList.Count; i++)
		{
			DrawActiveRequestRow(_cachedActiveViewList[i], i, (float)i * 22f, viewWidth, promptWidth);
		}
		Widgets.EndScrollView();
		DrawStickToBottomOverlay(scrollRect, maxScroll, ref _activeRequestsScrollPosition);
	}

	private void DrawActiveRequestHeader(Rect rect, float promptWidth)
	{
		Widgets.DrawBoxSolid(rect, new Color(0.2f, 0.2f, 0.25f, 0.9f));
		Text.Font = GameFont.Tiny;
		float currentX = rect.x + 5f;
		Widgets.Label(new Rect(currentX, rect.y, 65f, rect.height), "RimTalk.DebugWindow.HeaderTimestamp".Translate());
		currentX += 75f;
		Widgets.Label(new Rect(currentX, rect.y, 80f, rect.height), "RimTalk.DebugWindow.HeaderInitiator".Translate());
		currentX += 90f;
		Widgets.Label(new Rect(currentX, rect.y, 80f, rect.height), "RimTalk.DebugWindow.HeaderRecipient".Translate());
		currentX += 90f;
		Widgets.Label(new Rect(currentX, rect.y, promptWidth, rect.height), "RimTalk.DebugWindow.HeaderPrompt".Translate());
		currentX += promptWidth + 10f;
		Widgets.Label(new Rect(currentX, rect.y, 60f, rect.height), "RimTalk.DebugWindow.HeaderType".Translate());
		currentX += 70f;
		Widgets.Label(new Rect(currentX, rect.y, 60f, rect.height), "RimTalk.DebugWindow.HeaderElapsed".Translate());
		currentX += 70f;
		Widgets.Label(new Rect(currentX, rect.y, 60f, rect.height), "RimTalk.DebugWindow.HeaderStatus".Translate());
	}

	private void DrawActiveRequestRow(TalkRequest req, int index, float rowY, float width, float promptWidth)
	{
		Rect rowRect = new Rect(0f, rowY, width, 22f);
		if (index % 2 == 0)
		{
			Widgets.DrawBoxSolid(rowRect, new Color(0.15f, 0.15f, 0.15f, 0.4f));
		}
		float currentX = 5f;
		int currentTick = GenTicks.TicksGame;
		Widgets.Label(new Rect(currentX, rowY, 65f, 22f), req.CreatedTime.ToString("HH:mm:ss"));
		currentX += 75f;
		Rect initRect = new Rect(currentX, rowY, 80f, 22f);
		string initName = req.Initiator?.LabelShort ?? "-";
		UIUtil.DrawClickablePawnName(initRect, initName, req.Initiator);
		currentX += 90f;
		Rect recRect = new Rect(currentX, rowY, 80f, 22f);
		if (req.Recipient != null && req.Recipient != req.Initiator)
		{
			UIUtil.DrawClickablePawnName(recRect, req.Recipient.LabelShort, req.Recipient);
		}
		else
		{
			Widgets.Label(recRect, "-");
		}
		currentX += 90f;
		string prompt = req.Prompt ?? "";
		Widgets.Label(new Rect(currentX, rowY, promptWidth, 22f), prompt);
		currentX += promptWidth + 10f;
		Widgets.Label(new Rect(currentX, rowY, 60f, 22f), req.TalkType.ToString());
		currentX += 70f;
		int ticksElapsed = Math.Max(0, ((req.Status == RequestStatus.Pending || req.FinishedTick == -1) ? GenTicks.TicksGame : req.FinishedTick) - req.CreatedTick);
		string elapsedStr = $"{ticksElapsed / 60}s";
		Widgets.Label(new Rect(currentX, rowY, 60f, 22f), elapsedStr);
		currentX += 70f;
		Color c = GUI.color;
		if (req.Status == RequestStatus.Expired)
		{
			GUI.color = Color.gray;
		}
		else if (req.Status == RequestStatus.Processed)
		{
			GUI.color = Color.green;
		}
		else
		{
			GUI.color = Color.yellow;
		}
		string translationKey = $"RimTalk.DebugWindow.State{req.Status}";
		Widgets.Label(new Rect(currentX, rowY, 60f, 22f), translationKey.Translate());
		GUI.color = c;
	}

	private void DrawConsoleTable(Rect rect)
	{
		float responseWidth = CalculateResponseColumnWidth(rect.width, includePawnColumn: true);
		DrawRequestTableHeader(new Rect(rect.x, rect.y, rect.width, 22f), responseWidth, showPawnColumn: true);
		Rect scrollRect = new Rect(rect.x, rect.y + 22f, rect.width, rect.height - 22f);
		float viewWidth = scrollRect.width - 16f;
		float viewHeight = (float)_requests.Count * 22f;
		Rect viewRect = new Rect(0f, 0f, viewWidth, viewHeight);
		float maxScroll = Mathf.Max(0f, viewHeight - scrollRect.height);
		if (_stickToBottom)
		{
			_tableScrollPosition.y = maxScroll;
		}
		Widgets.BeginScrollView(scrollRect, ref _tableScrollPosition, viewRect);
		if (_stickToBottom && _tableScrollPosition.y < maxScroll - 1f)
		{
			_stickToBottom = false;
		}
		Rect? overlayContentRect = null;
		if (!_stickToBottom)
		{
			float overlayWinX = scrollRect.xMax - 30f - 20f;
			float overlayWinY = scrollRect.yMax - 30f - 5f;
			float overlayContentX = overlayWinX - scrollRect.x + _tableScrollPosition.x;
			float overlayContentY = overlayWinY - scrollRect.y + _tableScrollPosition.y;
			overlayContentRect = new Rect(overlayContentX, overlayContentY, 30f, 30f);
		}
		float visibleTop = _tableScrollPosition.y;
		float visibleBottom = _tableScrollPosition.y + scrollRect.height;
		int firstIndex = Mathf.Clamp((int)(visibleTop / 22f), 0, _requests.Count);
		int lastIndex = Mathf.Clamp((int)(visibleBottom / 22f) + 1, 0, _requests.Count);
		for (int i = firstIndex; i < lastIndex; i++)
		{
			float rowY = (float)i * 22f;
			bool inputBlocked = overlayContentRect.HasValue && Mouse.IsOver(overlayContentRect.Value);
			DrawRequestRow(_requests[i], i, rowY, viewWidth, 0f, responseWidth, showPawnColumn: true, inputBlocked);
		}
		Widgets.EndScrollView();
		DrawStickToBottomOverlay(scrollRect, maxScroll, ref _tableScrollPosition);
	}

	private void DrawRequestRow(ApiLog request, int rowIndex, float rowY, float totalWidth, float xOffset, float responseColumnWidth, bool showPawnColumn, bool inputBlocked = false)
	{
		Rect rowRect = new Rect(xOffset, rowY, totalWidth, 22f);
		if (rowIndex % 2 == 0)
		{
			Widgets.DrawBoxSolid(rowRect, new Color(0.15f, 0.15f, 0.15f, 0.4f));
		}
		if (_selectedLog != null && _selectedLog.Id == request.Id)
		{
			Widgets.DrawBoxSolid(rowRect, new Color(0.2f, 0.25f, 0.35f, 0.45f));
		}
		float currentX = xOffset + 5f;
		Widgets.Label(new Rect(currentX, rowRect.y, 65f, 22f), request.Timestamp.ToString("HH:mm:ss"));
		currentX += 75f;
		if (showPawnColumn)
		{
			string pawnName = request.Name ?? "-";
			Rect pawnNameRect = new Rect(currentX, rowRect.y, 80f, 22f);
			Pawn pawn = _pawnStates.FirstOrDefault((PawnState p) => p.Pawn.LabelShort == pawnName)?.Pawn;
			UIUtil.DrawClickablePawnName(pawnNameRect, pawnName, pawn);
			currentX += 90f;
		}
		string resp = request.Response ?? _generating;
		Widgets.Label(new Rect(currentX, rowRect.y, responseColumnWidth, 22f), resp);
		currentX += responseColumnWidth + 10f;
		string interactionType = request.InteractionType ?? "-";
		Widgets.Label(new Rect(currentX, rowRect.y, 50f, 22f), interactionType);
		currentX += 60f;
		string elapsedMsText = ((request.Response == null) ? "" : ((request.ElapsedMs == 0) ? "-" : request.ElapsedMs.ToString()));
		Widgets.Label(new Rect(currentX, rowRect.y, 55f, 22f), elapsedMsText);
		currentX += 65f;
		int count = request.Payload?.TokenCount ?? 0;
		string tokenCountText = ((count != 0) ? count.ToString() : (request.IsFirstDialogue ? "-" : ""));
		Widgets.Label(new Rect(currentX, rowRect.y, 50f, 22f), tokenCountText);
		currentX += 60f;
		ApiLog.State stateFilter = request.GetState();
		GUI.color = stateFilter.GetColor();
		Widgets.Label(new Rect(currentX, rowRect.y, 65f, 22f), stateFilter.GetLabel());
		GUI.color = Color.white;
		if (!inputBlocked)
		{
			TooltipHandler.TipRegion(rowRect, "RimTalk.DebugWindow.TooltipSelectForDetails".Translate());
		}
		if (!inputBlocked && Widgets.ButtonInvisible(rowRect))
		{
			_selectedLog = request;
			_stickToBottom = false;
		}
	}

	private void DrawDetailsPanel(Rect rect)
	{
		//IL_04ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f8: Expected O, but got Unknown
		Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.08f, 0.1f, 0.8f));
		InitializeContextStyle();
		Rect inner = rect.ContractedBy(8f);
		GUI.BeginGroup(inner);
		float y = 0f;
		Text.Font = GameFont.Small;
		Widgets.Label(new Rect(0f, y, inner.width, 24f), "RimTalk.DebugWindow.Details".Translate());
		y += 26f;
		if (_selectedLog == null)
		{
			Text.Font = GameFont.Tiny;
			GUI.color = Color.gray;
			Widgets.Label(new Rect(0f, y, inner.width, 50f), "RimTalk.DebugWindow.SelectRowHint".Translate());
			GUI.color = Color.white;
			GUI.EndGroup();
			return;
		}
		bool isGenerating = _selectedLog.Response == null || AIService.IsBusy();
		if (_selectedLog.Id != _selectedRequestIdForTemp || isGenerating)
		{
			if (_selectedLog.Id != _selectedRequestIdForTemp)
			{
				_selectedRequestIdForTemp = _selectedLog.Id;
				_expandedPromptSegmentIndices.Clear();
			}
			_tempResponse = _selectedLog.Response ?? string.Empty;
			_tempPromptSegments = ResolvePromptSegments(_selectedLog);
			_tempPromptSegmentsText = FormatPromptSegments(_tempPromptSegments);
		}
		StringBuilder header = new StringBuilder();
		header.Append(_selectedLog.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
		header.Append("  |  ");
		header.Append((_selectedLog.Name ?? "-").Trim());
		if (_selectedLog.InteractionType != null)
		{
			header.Append("  |  ");
			header.Append(_selectedLog.InteractionType);
		}
		Text.Font = GameFont.Tiny;
		GUI.color = Color.gray;
		Widgets.Label(new Rect(0f, y, inner.width, 18f), header.ToString());
		GUI.color = Color.white;
		y += 22f;
		float buttonsRowH = 24f;
		float btnW = 88f;
		float btnX = 0f;
		if (Widgets.ButtonText(new Rect(btnX, y, btnW, buttonsRowH), "RimTalk.DebugWindow.CopyAll".Translate()))
		{
			GUIUtility.systemCopyBuffer = _selectedLog.ToString();
			Messages.Message("RimTalk.DebugWindow.Copied".Translate(), MessageTypeDefOf.TaskCompletion, historical: false);
		}
		if (_selectedLog.GetState() != ApiLog.State.None)
		{
			btnX += btnW + 6f;
			Rect reportRect = new Rect(btnX, y, btnW, buttonsRowH);
			if (Widgets.ButtonText(reportRect, "RimTalk.DebugWindow.ApiLog".Translate()))
			{
				GUIUtility.systemCopyBuffer = _selectedLog.Payload?.ToString();
				Messages.Message("RimTalk.DebugWindow.Copied".Translate(), MessageTypeDefOf.TaskCompletion, historical: false);
			}
		}
		GUI.enabled = _selectedLog.Channel != Channel.User;
		btnX += btnW + 6f;
		Color prevResendColor = GUI.color;
		GUI.color = new Color(0.6f, 0.9f, 0.6f);
		Rect resendRect = new Rect(btnX, y, btnW, buttonsRowH);
		if (Widgets.ButtonText(resendRect, "RimTalk.DebugWindow.Resend".Translate()))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			Resend();
		}
		TooltipHandler.TipRegion(resendRect, "RimTalk.DebugWindow.ResendTooltip".Translate());
		GUI.color = prevResendColor;
		GUI.enabled = true;
		y += buttonsRowH + 8f;
		if (_selectedLog.GetState() == ApiLog.State.Failed)
		{
			Color prevColor = GUI.color;
			GUI.color = new Color(1f, 0.5f, 0.5f);
			string failedMsg = "RimTalk.DebugWindow.FailMsg".Translate();
			Widgets.Label(new Rect(0f, y, inner.width, 20f), failedMsg);
			GUI.color = prevColor;
			y += 30f;
		}
		Rect scrollOuter = new Rect(0f, y, inner.width, inner.height - y);
		float blockSpacing = 10f;
		float headerH = 18f;
		float viewWidth = scrollOuter.width - 16f;
		float textAreaWidth = viewWidth - 8f;
		float respH = Mathf.Max(40f, _monoTinyStyle.CalcHeight(new GUIContent(_tempResponse), textAreaWidth) + 10f);
		float msgH = CalculatePromptSegmentsHeight(_tempPromptSegments, viewWidth);
		float viewH = headerH + respH + blockSpacing + msgH + 10f;
		Rect view = new Rect(0f, 0f, scrollOuter.width - 16f, viewH);
		Widgets.BeginScrollView(scrollOuter, ref _detailsScrollPosition, view);
		float yy = 0f;
		DrawSelectableBlock(ref yy, view.width, "RimTalk.DebugWindow.Response".Translate(), ref _tempResponse, respH, "DetailResponseField", delegate
		{
			GUIUtility.systemCopyBuffer = _tempResponse;
			Messages.Message("RimTalk.DebugWindow.Copied".Translate(), MessageTypeDefOf.TaskCompletion, historical: false);
		}, null, readOnly: true);
		yy += blockSpacing;
		DrawPromptMessagesBlock(ref yy, view.width, "RimTalk.DebugWindow.PromptMessages".Translate(), _tempPromptSegments, _tempPromptSegmentsText);
		Widgets.EndScrollView();
		GUI.EndGroup();
	}

	private void DrawSelectableBlock(ref float y, float width, string title, ref string content, float contentHeight, string controlName, Action onCopy, Action onReset = null, bool readOnly = false)
	{
		float headerHeight = 18f;
		Text.Font = GameFont.Tiny;
		GUI.color = Color.gray;
		Vector2 labelSize = Text.CalcSize(title);
		Rect labelRect = new Rect(0f, y, labelSize.x, headerHeight);
		Widgets.Label(labelRect, title);
		Rect copyRect = new Rect(labelRect.xMax + 8f, y, 16f, 16f);
		if (Widgets.ButtonImage(copyRect, TexButton.Copy))
		{
			onCopy?.Invoke();
		}
		TooltipHandler.TipRegion(copyRect, "RimTalk.DebugWindow.Copy".Translate());
		if (onReset != null)
		{
			Rect resetRect = new Rect(copyRect.xMax + 4f, y, 16f, 16f);
			if (Widgets.ButtonImage(resetRect, TexButton.HotReloadDefs))
			{
				onReset();
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			TooltipHandler.TipRegion(resetRect, "RimTalk.DebugWindow.Undo".Translate());
		}
		GUI.color = Color.white;
		y += headerHeight;
		Rect box = new Rect(0f, y, width, contentHeight);
		bool isFocused = GUI.GetNameOfFocusedControl() == controlName;
		Color colorUnfocused = new Color(0.05f, 0.05f, 0.05f, 0.55f);
		Color colorFocused = new Color(0.15f, 0.15f, 0.15f, 0.4f);
		Widgets.DrawBoxSolid(box, (isFocused && !readOnly) ? colorFocused : colorUnfocused);
		Rect textRect = box.ContractedBy(4f);
		GUI.SetNextControlName(controlName);
		if (readOnly)
		{
			GUI.TextArea(textRect, content, _monoTinyStyle);
		}
		else
		{
			content = GUI.TextArea(textRect, content, _monoTinyStyle);
		}
		y += contentHeight;
	}

	private void DrawPromptMessagesBlock(ref float y, float width, string title, List<PromptMessageSegment> segments, string combinedText)
	{
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Expected O, but got Unknown
		float headerHeight = 18f;
		Text.Font = GameFont.Tiny;
		GUI.color = Color.gray;
		Vector2 labelSize = Text.CalcSize(title);
		Rect labelRect = new Rect(0f, y, labelSize.x, headerHeight);
		Widgets.Label(labelRect, title);
		Rect copyRect = new Rect(labelRect.xMax + 8f, y, 16f, 16f);
		if (Widgets.ButtonImage(copyRect, TexButton.Copy))
		{
			GUIUtility.systemCopyBuffer = combinedText ?? "";
			Messages.Message("RimTalk.DebugWindow.Copied".Translate(), MessageTypeDefOf.TaskCompletion, historical: false);
		}
		TooltipHandler.TipRegion(copyRect, "RimTalk.DebugWindow.Copy".Translate());
		GUI.color = Color.white;
		y += headerHeight;
		if (segments == null || segments.Count == 0)
		{
			GUI.color = Color.gray;
			Widgets.Label(new Rect(0f, y, width, 20f), "-");
			GUI.color = Color.white;
			y += 20f;
			return;
		}
		for (int i = 0; i < segments.Count; i++)
		{
			PromptMessageSegment segment = segments[i];
			string preview = GetPromptMessagePreview(segment.Content);
			string roleLabel = GetRoleLabel(segment.Role);
			string entryName = (string.IsNullOrWhiteSpace(segment.EntryName) ? "Entry" : segment.EntryName);
			string state = (_expandedPromptSegmentIndices.Contains(i) ? "[-]" : "[+]");
			string label = $"{state} {i + 1}. {entryName} ({roleLabel}): {preview}";
			Rect headerRect = new Rect(0f, y, width, 22f);
			Widgets.DrawBoxSolid(headerRect, new Color(0.12f, 0.12f, 0.12f, 0.6f));
			Widgets.Label(new Rect(headerRect.x + 6f, headerRect.y + 2f, headerRect.width - 12f, headerRect.height), label);
			if (Widgets.ButtonInvisible(headerRect))
			{
				if (_expandedPromptSegmentIndices.Contains(i))
				{
					_expandedPromptSegmentIndices.Remove(i);
				}
				else
				{
					_expandedPromptSegmentIndices.Add(i);
				}
			}
			y += 22f;
			if (_expandedPromptSegmentIndices.Contains(i))
			{
				string safeContent = segment.Content ?? "";
				float bodyHeight = Mathf.Max(40f, _monoTinyStyle.CalcHeight(new GUIContent(safeContent), width - 8f) + 10f);
				Rect bodyRect = new Rect(0f, y, width, bodyHeight);
				Widgets.DrawBoxSolid(bodyRect, new Color(0.05f, 0.05f, 0.05f, 0.55f));
				Rect textRect = bodyRect.ContractedBy(4f);
				string newContent = GUI.TextArea(textRect, safeContent, _monoTinyStyle);
				if (newContent != safeContent)
				{
					segment.Content = newContent;
					_tempPromptSegmentsText = FormatPromptSegments(segments);
				}
				y += bodyHeight;
			}
			y += 6f;
		}
	}

	private void DrawGroupedPawnTable(Rect rect)
	{
		if (_pawnStates == null || !_pawnStates.Any())
		{
			return;
		}
		float viewWidth = rect.width - 16f;
		float totalHeight = CalculateGroupedTableHeight(viewWidth);
		Rect viewRect = new Rect(0f, 0f, viewWidth, totalHeight);
		float maxScroll = Mathf.Max(0f, totalHeight - rect.height);
		if (_stickToBottom)
		{
			_tableScrollPosition.y = maxScroll;
		}
		Widgets.BeginScrollView(rect, ref _tableScrollPosition, viewRect);
		if (_stickToBottom && _tableScrollPosition.y < maxScroll - 1f)
		{
			_stickToBottom = false;
		}
		float responseColumnWidth = CalculateGroupedResponseColumnWidth(viewRect.width);
		DrawGroupedHeader(new Rect(0f, 0f, viewRect.width, 22f), responseColumnWidth);
		float currentY = 22f;
		List<PawnState> sortedPawns = GetSortedPawnStates().ToList();
		if (!string.IsNullOrWhiteSpace(_pawnFilter))
		{
			string needle = _pawnFilter.Trim();
			sortedPawns = sortedPawns.Where((PawnState p) => (p.Pawn.LabelShort ?? "").IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		}
		if (!string.IsNullOrWhiteSpace(_textSearch))
		{
			string needle2 = _textSearch.Trim();
			sortedPawns = sortedPawns.Where((PawnState p) => GetLastResponseForPawn(p.Pawn.LabelShort).IndexOf(needle2, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		}
		for (int i = 0; i < sortedPawns.Count; i++)
		{
			PawnState pawnState = sortedPawns[i];
			string pawnKey = pawnState.Pawn.LabelShort;
			bool isExpanded = _expandedPawns.Contains(pawnKey);
			Rect rowRect = new Rect(0f, currentY, viewRect.width, 22f);
			if (i % 2 == 0)
			{
				Widgets.DrawBoxSolid(rowRect, new Color(0.15f, 0.15f, 0.15f, 0.4f));
			}
			float currentX = 0f;
			Widgets.Label(new Rect(rowRect.x + 5f, rowRect.y + 3f, 15f, 15f), isExpanded ? "-" : "+");
			currentX += 25f;
			Rect pawnNameRect = new Rect(currentX, rowRect.y, 80f, 22f);
			UIUtil.DrawClickablePawnName(pawnNameRect, pawnKey, pawnState.Pawn);
			currentX += 90f;
			string lastResponse = GetLastResponseForPawn(pawnKey);
			Widgets.Label(new Rect(currentX, rowRect.y, responseColumnWidth, 22f), lastResponse);
			currentX += responseColumnWidth + 10f;
			bool canTalk = pawnState.CanGenerateTalk();
			string statusText = (canTalk ? "RimTalk.DebugWindow.StatusReady".Translate() : "RimTalk.DebugWindow.StatusBusy".Translate());
			GUI.color = (canTalk ? Color.green : Color.yellow);
			Widgets.Label(new Rect(currentX, rowRect.y, 60f, 22f), statusText);
			GUI.color = Color.white;
			currentX += 70f;
			Widgets.Label(new Rect(currentX, rowRect.y, 60f, 22f), pawnState.LastTalkTick.ToString());
			currentX += 70f;
			_talkLogsByPawn.TryGetValue(pawnKey, out var pawnRequests);
			Widgets.Label(label: ((pawnRequests?.Where((ApiLog apiLog) => apiLog.IsFirstDialogue).ToList())?.Count ?? 0).ToString(), rect: new Rect(currentX, rowRect.y, 60f, 22f));
			currentX += 70f;
			Widgets.Label(new Rect(currentX, rowRect.y, 65f, 22f), pawnState.TalkInitiationWeight.ToString("F2"));
			if (Widgets.ButtonInvisible(rowRect))
			{
				if (isExpanded)
				{
					_expandedPawns.Remove(pawnKey);
				}
				else
				{
					_expandedPawns.Add(pawnKey);
				}
			}
			currentY += 22f;
			if (!isExpanded || !_talkLogsByPawn.TryGetValue(pawnKey, out var requests) || !requests.Any())
			{
				continue;
			}
			float innerWidth = viewRect.width - 20f;
			float innerResponseWidth = CalculateResponseColumnWidth(innerWidth, includePawnColumn: false);
			DrawRequestTableHeader(new Rect(20f, currentY, innerWidth, 22f), innerResponseWidth, showPawnColumn: false);
			currentY += 22f;
			foreach (ApiLog r in requests)
			{
				DrawRequestRow(r, 0, currentY, innerWidth, 20f, innerResponseWidth, showPawnColumn: false);
				currentY += 22f;
			}
		}
		Widgets.EndScrollView();
		DrawStickToBottomOverlay(rect, maxScroll, ref _tableScrollPosition);
	}

	private void DrawGroupedHeader(Rect rect, float responseColumnWidth)
	{
		Widgets.DrawBoxSolid(rect, new Color(0.3f, 0.3f, 0.3f, 0.8f));
		Text.Font = GameFont.Tiny;
		GUI.color = Color.white;
		float currentX = 25f;
		DrawSortableHeader(new Rect(currentX, rect.y, 80f, rect.height), "RimTalk.DebugWindow.HeaderPawn", "Pawn");
		currentX += 90f;
		DrawSortableHeader(new Rect(currentX, rect.y, responseColumnWidth, rect.height), "RimTalk.DebugWindow.HeaderResponse", "Response");
		currentX += responseColumnWidth + 10f;
		DrawSortableHeader(new Rect(currentX, rect.y, 60f, rect.height), "RimTalk.DebugWindow.HeaderStatus", "Status");
		currentX += 70f;
		DrawSortableHeader(new Rect(currentX, rect.y, 60f, rect.height), "RimTalk.DebugWindow.HeaderLastTalk", "Last Talk");
		currentX += 70f;
		DrawSortableHeader(new Rect(currentX, rect.y, 60f, rect.height), "RimTalk.DebugWindow.HeaderRequests", "Requests");
		currentX += 70f;
		DrawSortableHeader(new Rect(currentX, rect.y, 65f, rect.height), "RimTalk.DebugWindow.HeaderChattiness", "Chattiness");
	}

	private void DrawRequestTableHeader(Rect rect, float responseColumnWidth, bool showPawnColumn)
	{
		Widgets.DrawBoxSolid(rect, new Color(0.2f, 0.2f, 0.25f, 0.9f));
		Text.Font = GameFont.Tiny;
		float currentX = rect.x + 5f;
		Widgets.Label(new Rect(currentX, rect.y, 65f, rect.height), "RimTalk.DebugWindow.HeaderTimestamp".Translate());
		currentX += 75f;
		if (showPawnColumn)
		{
			Widgets.Label(new Rect(currentX, rect.y, 80f, rect.height), "RimTalk.DebugWindow.HeaderPawn".Translate());
			currentX += 90f;
		}
		Widgets.Label(new Rect(currentX, rect.y, responseColumnWidth, rect.height), "RimTalk.DebugWindow.HeaderResponse".Translate());
		currentX += responseColumnWidth + 10f;
		Widgets.Label(new Rect(currentX, rect.y, 50f, rect.height), "RimTalk.DebugWindow.HeaderType".Translate());
		currentX += 60f;
		Widgets.Label(new Rect(currentX, rect.y, 55f, rect.height), "RimTalk.DebugWindow.HeaderTimeMs".Translate());
		currentX += 65f;
		Widgets.Label(new Rect(currentX, rect.y, 50f, rect.height), "RimTalk.DebugWindow.HeaderTokens".Translate());
		currentX += 60f;
		Widgets.Label(new Rect(currentX, rect.y, 65f, rect.height), "RimTalk.DebugWindow.HeaderState".Translate());
	}

	private void DrawStickToBottomOverlay(Rect scrollRect, float maxScroll, ref Vector2 scrollPosition)
	{
		if (!_stickToBottom)
		{
			Rect overlayRect = new Rect(scrollRect.xMax - 30f - 20f, scrollRect.yMax - 30f - 5f, 30f, 30f);
			Color bgColor = (Mouse.IsOver(overlayRect) ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0f, 0f, 0f, 0.6f));
			Widgets.DrawBoxSolid(overlayRect, bgColor);
			if (Widgets.ButtonInvisible(overlayRect))
			{
				_stickToBottom = true;
				scrollPosition.y = maxScroll;
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleLeft;
			Rect labelRect = overlayRect;
			labelRect.xMin += 5f;
			Widgets.Label(labelRect, "▼");
			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Tiny;
			TooltipHandler.TipRegion(overlayRect, "RimTalk.DebugWindow.AutoScroll".Translate());
		}
	}

	private void DrawSortableHeader(Rect rect, string key, string defaultLabel)
	{
		string translatedColumn = key.Translate();
		if (translatedColumn == key)
		{
			translatedColumn = defaultLabel;
		}
		string arrow = ((!(_sortColumn == key)) ? "" : (_sortAscending ? " ▲" : " ▼"));
		if (Widgets.ButtonInvisible(rect))
		{
			if (_sortColumn == key)
			{
				_sortAscending = !_sortAscending;
			}
			else
			{
				_sortColumn = key;
				_sortAscending = true;
			}
			RimTalkSettings settings = Settings.Get();
			settings.DebugSortColumn = _sortColumn;
			settings.DebugSortAscending = _sortAscending;
		}
		Widgets.Label(rect, translatedColumn + arrow);
	}

	private void DrawGraph(Rect rect)
	{
		Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.15f, 0.8f));
		(List<long>, Color, TaggedString)[] series = new(List<long>, Color, TaggedString)[1] { (Stats.TokensPerSecondHistory, new Color(1f, 1f, 1f, 0.7f), "RimTalk.DebugWindow.TokensPerSecond".Translate()) };
		if (!series.Any(((List<long> data, Color color, TaggedString label) s) => s.data != null && s.data.Any()))
		{
			return;
		}
		long maxVal = Math.Max(1L, series.Where(((List<long> data, Color color, TaggedString label) s) => s.data != null && s.data.Any()).SelectMany(((List<long> data, Color color, TaggedString label) s) => s.data).Max());
		Text.Font = GameFont.Tiny;
		GUI.color = Color.gray;
		Widgets.Label(new Rect(rect.x + 5f, rect.y, 40f, 20f), maxVal.ToString());
		Widgets.Label(new Rect(rect.x + 5f, rect.y + rect.height - 15f, 60f, 20f), "RimTalk.DebugWindow.SixtySecondsAgo".Translate());
		Widgets.Label(new Rect(rect.xMax - 35f, rect.y + rect.height - 15f, 40f, 20f), "RimTalk.DebugWindow.Now".Translate());
		GUI.color = Color.white;
		Rect graphArea = rect.ContractedBy(2f);
		(List<long>, Color, TaggedString)[] array = series;
		for (int num = 0; num < array.Length; num++)
		{
			var (data, color, _) = array[num];
			if (data == null || data.Count < 2)
			{
				continue;
			}
			float graphHeight = graphArea.height - 30f;
			if (graphHeight <= 0f)
			{
				continue;
			}
			List<Vector2> points = new List<Vector2>();
			for (int i = 0; i < data.Count; i++)
			{
				float x = graphArea.x + (float)i / (float)(data.Count - 1) * graphArea.width;
				float y = graphArea.y + graphArea.height - 15f - (float)data[i] / (float)maxVal * graphHeight;
				points.Add(new Vector2(x, y));
				if (data[i] > 0 && i > 0 && i % 6 == 0)
				{
					GUI.color = color;
					Widgets.Label(new Rect(x - 10f, y - 15f, 40f, 20f), data[i].ToString());
					GUI.color = Color.white;
				}
			}
			for (int i2 = 0; i2 < points.Count - 1; i2++)
			{
				Widgets.DrawLine(points[i2], points[i2 + 1], color, 2f);
			}
		}
		Rect legendRect = new Rect(rect.xMax - 100f, rect.y + 10f, 90f, 30f);
		Listing_Standard legendListing = new Listing_Standard();
		Widgets.DrawBoxSolid(legendRect, new Color(0f, 0f, 0f, 0.4f));
		legendListing.Begin(legendRect.ContractedBy(5f));
		(List<long>, Color, TaggedString)[] array2 = series;
		for (int num2 = 0; num2 < array2.Length; num2++)
		{
			(List<long>, Color, TaggedString) tuple2 = array2[num2];
			List<long> data2 = tuple2.Item1;
			Color color2 = tuple2.Item2;
			TaggedString label = tuple2.Item3;
			Rect labelRect = legendListing.GetRect(18f);
			Widgets.DrawBoxSolid(new Rect(labelRect.x, labelRect.y + 4f, 10f, 10f), color2);
			Widgets.Label(new Rect(labelRect.x + 15f, labelRect.y, 70f, 20f), label);
		}
		legendListing.End();
	}

	private void DrawStatsSection(Rect rect)
	{
		Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.15f, 0.4f));
		Text.Font = GameFont.Small;
		GUI.BeginGroup(rect);
		float currentY = 10f;
		Rect contentRect = rect.AtZero().ContractedBy(10f);
		TaggedString aiStatus = _aiStatus.Translate();
		Color statusColor = (((string)aiStatus == (string)"RimTalk.DebugWindow.StatusProcessing".Translate()) ? Color.yellow : ((!((string)aiStatus == (string)"RimTalk.DebugWindow.StatusIdle".Translate())) ? Color.gray : Color.green));
		GUI.color = Color.gray;
		Widgets.Label(new Rect(contentRect.x, currentY, 120f, 22f), "RimTalk.DebugWindow.AIStatus".Translate());
		GUI.color = statusColor;
		Widgets.Label(new Rect(contentRect.x + 120f, currentY, 150f, 22f), _aiStatus);
		GUI.color = Color.white;
		currentY += 22f;
		DrawStatRow("RimTalk.DebugWindow.TotalCalls".Translate(), _totalCalls.ToString("N0"));
		DrawStatRow("RimTalk.DebugWindow.TotalTokens".Translate(), _totalTokens.ToString("N0"));
		DrawStatRow("RimTalk.DebugWindow.AvgCallsPerMin".Translate(), _avgCallsPerMin.ToString("F2"));
		DrawStatRow("RimTalk.DebugWindow.AvgTokensPerMin".Translate(), _avgTokensPerMin.ToString("F2"));
		DrawStatRow("RimTalk.DebugWindow.AvgTokensPerCall".Translate(), _avgTokensPerCall.ToString("F2"));
		GUI.EndGroup();
		void DrawStatRow(string label, string value)
		{
			GUI.color = Color.gray;
			Widgets.Label(new Rect(contentRect.x, currentY, 120f, 22f), label);
			GUI.color = Color.white;
			Widgets.Label(new Rect(contentRect.x + 120f, currentY, 150f, 22f), value);
			currentY += 22f;
		}
	}

	private void DrawBottomActions(Rect rect)
	{
		Listing_Standard listing = new Listing_Standard();
		listing.Begin(rect);
		listing.Gap(6f);
		RimTalkSettings settings = Settings.Get();
		bool modEnabled = settings.IsEnabled;
		listing.CheckboxLabeled("RimTalk.DebugWindow.EnableRimTalk".Translate(), ref modEnabled);
		settings.IsEnabled = modEnabled;
		listing.Gap();
		if (listing.ButtonText("RimTalk.DebugWindow.ModSettings".Translate()))
		{
			Find.WindowStack.Add(new Dialog_ModSettings(LoadedModManager.GetMod<Settings>()));
		}
		listing.Gap(6f);
		if (listing.ButtonText("RimTalk.DebugWindow.Export".Translate()))
		{
			UIUtil.ExportLogs(_requests);
		}
		listing.Gap(6f);
		Color prevColor = GUI.color;
		GUI.color = new Color(1f, 0.4f, 0.4f);
		if (listing.ButtonText("RimTalk.DebugWindow.ResetLogs".Translate()))
		{
			Reset();
		}
		GUI.color = prevColor;
		listing.End();
	}

	private IEnumerable<ApiLog> ApplyFilters(IEnumerable<ApiLog> source)
	{
		IEnumerable<ApiLog> q = source;
		if (!string.IsNullOrWhiteSpace(_pawnFilter))
		{
			string needle = _pawnFilter.Trim();
			q = q.Where((ApiLog r) => (r.Name ?? string.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
		}
		if (!string.IsNullOrWhiteSpace(_textSearch))
		{
			string needle2 = _textSearch.Trim();
			q = q.Where((ApiLog r) => (r.TalkRequest.Prompt ?? string.Empty).IndexOf(needle2, StringComparison.OrdinalIgnoreCase) >= 0 || (r.Response ?? string.Empty).IndexOf(needle2, StringComparison.OrdinalIgnoreCase) >= 0 || (r.InteractionType ?? string.Empty).IndexOf(needle2, StringComparison.OrdinalIgnoreCase) >= 0);
		}
		if (_stateFilter != ApiLog.State.None)
		{
			q = q.Where((ApiLog r) => r.GetState() == _stateFilter);
		}
		return q;
	}

	private void HandleGlobalClicks(Rect inRect)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Event.current.type != 0 || !inRect.Contains(Event.current.mousePosition))
		{
			return;
		}
		string focused = GUI.GetNameOfFocusedControl();
		switch (focused)
		{
		default:
			if (!(focused == "DetailMessagesField"))
			{
				break;
			}
			goto case "PawnFilterField";
		case "PawnFilterField":
		case "TextSearchField":
		case "DetailResponseField":
			GUI.FocusControl((string)null);
			break;
		}
	}

	private string DrawSearchField(Rect rect, string text, string placeholder, string controlName)
	{
		GUI.SetNextControlName(controlName);
		string result = Widgets.TextField(rect, text);
		if (string.IsNullOrEmpty(result))
		{
			Color prevColor = GUI.color;
			GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
			Text.Anchor = TextAnchor.MiddleLeft;
			Rect labelRect = new Rect(rect.x + 5f, rect.y, rect.width - 5f, rect.height);
			Widgets.Label(labelRect, placeholder);
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = prevColor;
		}
		return result;
	}

	private IEnumerable<PawnState> GetSortedPawnStates()
	{
		List<ApiLog> value;
		return _sortColumn switch
		{
			"RimTalk.DebugWindow.HeaderPawn" => _sortAscending ? _pawnStates.OrderBy((PawnState p) => p.Pawn.LabelShort) : _pawnStates.OrderByDescending((PawnState p) => p.Pawn.LabelShort), 
			"RimTalk.DebugWindow.HeaderRequests" => _sortAscending ? _pawnStates.OrderBy((PawnState p) => _talkLogsByPawn.TryGetValue(p.Pawn.LabelShort, out value) ? value.Count((ApiLog r) => r.IsFirstDialogue) : 0) : _pawnStates.OrderByDescending((PawnState p) => _talkLogsByPawn.TryGetValue(p.Pawn.LabelShort, out value) ? value.Count((ApiLog r) => r.IsFirstDialogue) : 0), 
			"RimTalk.DebugWindow.HeaderResponse" => _sortAscending ? _pawnStates.OrderBy((PawnState p) => GetLastResponseForPawn(p.Pawn.LabelShort)) : _pawnStates.OrderByDescending((PawnState p) => GetLastResponseForPawn(p.Pawn.LabelShort)), 
			"RimTalk.DebugWindow.HeaderStatus" => _sortAscending ? _pawnStates.OrderBy((PawnState p) => p.CanDisplayTalk()) : _pawnStates.OrderByDescending((PawnState p) => p.CanDisplayTalk()), 
			"RimTalk.DebugWindow.HeaderLastTalk" => _sortAscending ? _pawnStates.OrderBy((PawnState p) => p.LastTalkTick) : _pawnStates.OrderByDescending((PawnState p) => p.LastTalkTick), 
			"RimTalk.DebugWindow.HeaderChattiness" => _sortAscending ? _pawnStates.OrderBy((PawnState p) => p.TalkInitiationWeight) : _pawnStates.OrderByDescending((PawnState p) => p.TalkInitiationWeight), 
			_ => _pawnStates, 
		};
	}

	private float CalculateResponseColumnWidth(float totalWidth, bool includePawnColumn)
	{
		float fixedWidth = 285f;
		int columnGaps = 6;
		if (includePawnColumn)
		{
			fixedWidth += 80f;
			columnGaps++;
		}
		float availableWidth = totalWidth - fixedWidth - 10f * (float)columnGaps;
		return Mathf.Max(40f, availableWidth);
	}

	private float CalculateGroupedResponseColumnWidth(float totalWidth)
	{
		float fixedWidth = 350f;
		int columnGaps = 6;
		float availableWidth = totalWidth - fixedWidth - 10f * (float)columnGaps;
		return Math.Max(150f, availableWidth);
	}

	private float CalculateGroupedTableHeight(float viewWidth)
	{
		float height = 22f + (float)_pawnStates.Count * 22f;
		foreach (PawnState pawnState in _pawnStates)
		{
			string pawnKey = pawnState.Pawn.LabelShort;
			if (_expandedPawns.Contains(pawnKey) && _talkLogsByPawn.TryGetValue(pawnKey, out var requests))
			{
				height += 22f;
				height += (float)requests.Count * 22f;
			}
		}
		return height + 50f;
	}

	private string GetLastResponseForPawn(string pawnKey)
	{
		if (_talkLogsByPawn.TryGetValue(pawnKey, out var logs) && logs.Any())
		{
			return logs.Last().Response ?? _generating;
		}
		return "";
	}

	private static List<PromptMessageSegment> ResolvePromptSegments(ApiLog log)
	{
		TalkRequest request = log?.TalkRequest;
		if (request?.PromptMessageSegments != null && request.PromptMessageSegments.Count > 0)
		{
			return request.PromptMessageSegments;
		}
		List<PromptMessageSegment> segments = new List<PromptMessageSegment>();
		if (request == null)
		{
			return segments;
		}
		if (request.PromptMessages != null && request.PromptMessages.Count > 0)
		{
			for (int i = 0; i < request.PromptMessages.Count; i++)
			{
				var (role, content) = request.PromptMessages[i];
				segments.Add(new PromptMessageSegment($"message-{i}", string.Format("{0} {1}", "RimTalk.DebugWindow.FormatEntry".Translate(), i + 1), role, content));
			}
			return segments;
		}
		string instruction = Constant.Instruction + "\n" + request.Context;
		segments.Add(new PromptMessageSegment("system-instruction", "RimTalk.DebugWindow.SystemInstruction".Translate(), Role.System, instruction));
		if (request.Initiator != null)
		{
			foreach (var (role2, message) in TalkHistory.GetMessageHistory(request.Initiator))
			{
				segments.Add(new PromptMessageSegment("chat-history", "RimTalk.DebugWindow.ChatHistory".Translate(), role2, message));
			}
		}
		if (!string.IsNullOrWhiteSpace(request.Prompt))
		{
			segments.Add(new PromptMessageSegment("input-prompt", "RimTalk.DebugWindow.InputPrompt".Translate(), Role.User, request.Prompt));
		}
		return segments;
	}

	private float CalculatePromptSegmentsHeight(List<PromptMessageSegment> segments, float width)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		float height = 18f;
		if (segments == null || segments.Count == 0)
		{
			return height + 20f;
		}
		for (int i = 0; i < segments.Count; i++)
		{
			height += 22f;
			if (_expandedPromptSegmentIndices.Contains(i))
			{
				string content = segments[i].Content ?? "";
				height += Mathf.Max(40f, _monoTinyStyle.CalcHeight(new GUIContent(content), width - 8f) + 10f);
			}
			height += 6f;
		}
		return height;
	}

	private static string GetPromptMessagePreview(string content)
	{
		if (string.IsNullOrWhiteSpace(content))
		{
			return "(empty)";
		}
		string firstLine = content.Replace("\r", "").Split('\n')[0].Trim();
		if (firstLine.Length > 80)
		{
			firstLine = firstLine.Substring(0, 77) + "...";
		}
		return firstLine;
	}

	private static string GetRoleLabel(Role role)
	{
		return (role == Role.AI) ? "Assistant" : role.ToString();
	}

	private static string FormatPromptSegments(List<PromptMessageSegment> segments)
	{
		if (segments == null || segments.Count == 0)
		{
			return string.Format("({0})", "RimTalk.DebugWindow.SelectRowHint".Translate());
		}
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < segments.Count; i++)
		{
			PromptMessageSegment segment = segments[i];
			string roleLabel = ((segment.Role == Role.AI) ? "assistant" : segment.Role.ToString().ToLowerInvariant());
			string entryName = (string.IsNullOrWhiteSpace(segment.EntryName) ? "RimTalk.DebugWindow.FormatEntry".Translate().Resolve() : segment.EntryName);
			sb.Append("RimTalk.DebugWindow.FormatEntry".Translate());
			sb.Append(": ");
			sb.AppendLine(entryName);
			sb.Append("RimTalk.DebugWindow.FormatRole".Translate());
			sb.Append(": ");
			sb.AppendLine(roleLabel);
			sb.AppendLine(segment.Content ?? "");
			if (i < segments.Count - 1)
			{
				sb.AppendLine();
			}
		}
		return sb.ToString();
	}

	private void Resend()
	{
		if (AIService.IsBusy())
		{
			Messages.Message("RimTalk.DebugWindow.ResendError".Translate(), MessageTypeDefOf.RejectInput);
			return;
		}
		TalkRequest debugRequest = _selectedLog.TalkRequest.Clone();
		if (_tempPromptSegments != null && _tempPromptSegments.Count > 0)
		{
			debugRequest.PromptMessageSegments = _tempPromptSegments.Select((PromptMessageSegment s) => new PromptMessageSegment(s.EntryId, s.EntryName, s.Role, s.Content)).ToList();
			debugRequest.PromptMessages = debugRequest.PromptMessageSegments.Select((PromptMessageSegment s) => (Role: s.Role, Content: s.Content)).ToList();
		}
		if (_selectedLog.Channel == Channel.Stream)
		{
			TalkService.GenerateTalkDebug(debugRequest);
		}
		else if (_selectedLog.Channel == Channel.Query)
		{
			Task.Run(() => AIService.Query<PersonalityData>(debugRequest));
		}
		Messages.Message("RimTalk.DebugWindow.ResendSuccess".Translate(), MessageTypeDefOf.TaskCompletion);
	}

	private void Reset()
	{
		TalkHistory.Clear();
		TalkRequestPool.ClearHistory();
		Stats.Reset();
		ApiHistory.Clear();
		UpdateData();
		Messages.Message("RimTalk.DebugWindow.HistoryCleared".Translate(), MessageTypeDefOf.TaskCompletion, historical: false);
	}
}
