using System.Collections.Generic;
using RimTalk.Service;
using RimWorld;
using Verse;

namespace RimTalk;

public class PlayLogEntry_RimTalkInteraction : PlayLogEntry_Interaction
{
	private string _cachedString;

	public Pawn Initiator => initiator;

	public Pawn Recipient => recipient;

	public List<RulePackDef> ExtraSentencePacks => extraSentencePacks;

	public string CachedString => _cachedString;

	public int TicksAbs => ticksAbs;

	public PlayLogEntry_RimTalkInteraction()
	{
	}

	public PlayLogEntry_RimTalkInteraction(InteractionDef interactionDef, Pawn initiator, Pawn recipient, List<RulePackDef> rules)
		: base(interactionDef, initiator, recipient, rules)
	{
		_cachedString = TalkService.GetTalk(initiator);
	}

	protected override string ToGameStringFromPOV_Worker(Thing pov, bool forceLog)
	{
		return _cachedString;
	}
}
