using RimWorld;
using Verse;

namespace AncotLibrary;

public class SignalAction_FactionChange : SignalAction
{
	public FactionDef factionOriginalDef;

	public FactionDef factionTargetedDef;

	public bool draftPawn;

	public bool becomePlayer;

	public ThoughtDef thoughtAddToPawn;

	public string dialogText;

	public bool pauseForced = false;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref factionOriginalDef, "factionOriginalDef");
		Scribe_Defs.Look(ref factionTargetedDef, "factionTargetedDef");
		Scribe_Values.Look(ref draftPawn, "draftPawn", defaultValue: false);
		Scribe_Values.Look(ref becomePlayer, "becomePlayer", defaultValue: false);
	}

	protected override void DoAction(SignalArgs args)
	{
		if (factionOriginalDef == null)
		{
			return;
		}
		Faction faction = Find.FactionManager.FirstFactionOfDef(factionOriginalDef);
		Faction faction2 = (becomePlayer ? Faction.OfPlayer : Find.FactionManager.FirstFactionOfDef(factionOriginalDef));
		if (faction2 == null)
		{
			return;
		}
		Map map = base.Map;
		if (map == null)
		{
			return;
		}
		foreach (Thing allThing in map.listerThings.AllThings)
		{
			if (allThing.Faction == faction && allThing.def.CanHaveFaction)
			{
				allThing.SetFaction(faction2);
				if (allThing is Pawn pawn)
				{
					ActionOnPawn(pawn);
				}
			}
		}
		if (dialogText != null)
		{
			DiaNode diaNode = new DiaNode(dialogText);
			DiaOption diaOption = new DiaOption();
			diaOption.resolveTree = true;
			diaOption.clickSound = null;
			diaNode.options.Add(diaOption);
			Dialog_NodeTree window = new Dialog_NodeTree(diaNode);
			Find.WindowStack.Add(window);
		}
		if (pauseForced)
		{
			Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
		}
	}

	public virtual void ActionOnPawn(Pawn pawn)
	{
		if (becomePlayer && draftPawn && !pawn.Downed)
		{
			pawn.drafter.Drafted = true;
		}
		if (thoughtAddToPawn != null)
		{
			Thought_Memory newThought = (Thought_Memory)ThoughtMaker.MakeThought(thoughtAddToPawn);
			pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(newThought);
		}
	}
}
