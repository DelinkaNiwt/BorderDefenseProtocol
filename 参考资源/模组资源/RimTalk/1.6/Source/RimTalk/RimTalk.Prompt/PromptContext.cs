using System.Collections;
using System.Collections.Generic;
using RimTalk.Data;
using RimTalk.Source.Data;
using Verse;

namespace RimTalk.Prompt;

public class PromptContext
{
	public Pawn CurrentPawn { get; set; }

	public List<Pawn> AllPawns { get; set; }

	public Map Map { get; set; }

	public VariableStore VariableStore { get; set; }

	public TalkRequest TalkRequest { get; set; }

	public string PawnContext { get; set; }

	public string DialogueType { get; set; }

	public string DialogueStatus { get; set; }

	public string DialoguePrompt { get; set; }

	public List<(Role role, string message)> ChatHistory { get; set; } = new List<(Role, string)>();

	public bool IsMonologue => TalkRequest?.IsMonologue ?? false;

	public TalkType TalkType => TalkRequest?.TalkType ?? TalkType.Other;

	public string UserPrompt => (TalkType != TalkType.User) ? null : TalkRequest?.Prompt;

	public List<Pawn> Pawns => AllPawns;

	public int ScopedPawnIndex { get; set; }

	public bool IsPreview { get; set; }

	public PromptContext()
	{
		AllPawns = new List<Pawn>();
	}

	public PromptContext(Pawn pawn, VariableStore variableStore = null)
	{
		CurrentPawn = pawn;
		AllPawns = ((pawn != null) ? new List<Pawn> { pawn } : new List<Pawn>());
		Map = pawn?.Map;
		VariableStore = variableStore ?? PromptManager.Instance?.VariableStore ?? new VariableStore();
	}

	public PromptContext(List<Pawn> pawns, VariableStore variableStore = null)
	{
		AllPawns = pawns ?? new List<Pawn>();
		CurrentPawn = ((AllPawns.Count > 0) ? AllPawns[0] : null);
		Map = CurrentPawn?.Map;
		VariableStore = variableStore ?? PromptManager.Instance?.VariableStore ?? new VariableStore();
	}

	public static PromptContext FromTalkRequest(TalkRequest request, List<Pawn> pawns = null)
	{
		List<Pawn> participants = request?.Participants ?? pawns ?? new List<Pawn>();
		return new PromptContext
		{
			TalkRequest = request,
			AllPawns = participants,
			CurrentPawn = request?.Initiator,
			Map = request?.Initiator?.Map,
			VariableStore = (PromptManager.Instance?.VariableStore ?? new VariableStore()),
			PawnContext = request?.Context,
			ChatHistory = (List<(Role role, string message)>)((request?.Initiator != null) ? ((IList)TalkHistory.GetMessageHistory(request.Initiator)) : ((IList)new List<(Role, string)>()))
		};
	}
}
