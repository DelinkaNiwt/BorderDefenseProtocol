using System;
using RimWorld;
using Verse;

namespace NCL;

public class CompUnlockResearch : ThingComp
{
	private bool researchUnlocked;

	private int ticksUntilNextCheck;

	private CompProperties_UnlockResearch Props => (CompProperties_UnlockResearch)props;

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
		ticksUntilNextCheck = Props.checkIntervalTicks;
	}

	public override void CompTick()
	{
		base.CompTick();
		if (!researchUnlocked && parent.Spawned && Find.ResearchManager != null)
		{
			ResearchProjectDef researchToUnlock = Props.researchToUnlock;
			if (researchToUnlock != null && !researchToUnlock.IsFinished && --ticksUntilNextCheck <= 0)
			{
				ticksUntilNextCheck = Props.checkIntervalTicks;
				CheckColonistProximity();
			}
		}
	}

	private void CheckColonistProximity()
	{
		Map map = parent.Map;
		if (map == null)
		{
			return;
		}
		int cellsToCheck = GenRadial.NumCellsInRadius(Props.activateRange);
		for (int i = 0; i < cellsToCheck; i++)
		{
			IntVec3 cell = parent.Position + GenRadial.RadialPattern[i];
			if (!cell.InBounds(map))
			{
				continue;
			}
			foreach (Thing thing in cell.GetThingList(map))
			{
				Pawn pawn = thing as Pawn;
				if (IsValidColonist(pawn))
				{
					UnlockResearch(pawn);
					return;
				}
			}
		}
	}

	private bool IsValidColonist(Pawn pawn)
	{
		return pawn != null && pawn.IsColonistPlayerControlled && (!Props.onlyAwakeColonists || pawn.Awake()) && (!Props.requireLineOfSight || GenSight.LineOfSightToThing(pawn.Position, parent, parent.Map));
	}

	private void UnlockResearch(Pawn triggerer)
	{
		try
		{
			Find.ResearchManager.FinishProject(Props.researchToUnlock);
			researchUnlocked = true;
			if (Props.sendLetter && Props.letterDef != null)
			{
				string label = (Props.letterLabel.NullOrEmpty() ? ((string)"ResearchUnlocked".Translate()) : Props.letterLabel);
				string text = (Props.letterText.NullOrEmpty() ? ((string)"ResearchUnlockedBy".Translate(Props.researchToUnlock.LabelCap, triggerer.LabelShort)) : Props.letterText);
				Find.LetterStack.ReceiveLetter(label, text, Props.letterDef, new LookTargets(parent));
			}
			if (Props.showActivationEffect)
			{
				FleckMaker.ThrowLightningGlow(parent.DrawPos, parent.Map, 1.5f);
				Messages.Message("NCL_ResearchUnlocked".Translate(Props.researchToUnlock.LabelCap), parent, MessageTypeDefOf.PositiveEvent);
			}
		}
		catch (Exception arg)
		{
			Log.Error($"Failed to unlock research {Props.researchToUnlock?.defName}: {arg}");
		}
	}

	public override string CompInspectStringExtra()
	{
		return (!researchUnlocked && Props.researchToUnlock != null) ? "NCL_WillUnlockResearch".Translate(Props.researchToUnlock.LabelCap) : ((TaggedString)null);
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref researchUnlocked, "researchUnlocked", defaultValue: false);
		Scribe_Values.Look(ref ticksUntilNextCheck, "ticksUntilNextCheck", Props.checkIntervalTicks);
	}
}
