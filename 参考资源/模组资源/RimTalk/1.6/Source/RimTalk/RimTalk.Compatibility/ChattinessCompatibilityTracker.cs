using RimTalk.Data;
using Verse;

namespace RimTalk.Compatibility;

public class ChattinessCompatibilityTracker : GameComponent
{
	private bool _wasRun;

	public ChattinessCompatibilityTracker(Game game)
	{
	}

	public override void ExposeData()
	{
		Scribe_Values.Look(ref _wasRun, "rimTalk_ChattinessHalved", defaultValue: false);
	}

	public override void FinalizeInit()
	{
		if (!_wasRun)
		{
			HalveAllExistingWeights();
			_wasRun = true;
		}
	}

	private void HalveAllExistingWeights()
	{
		HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("RimTalk_PersonaData");
		if (hediffDef == null)
		{
			return;
		}
		int count = 0;
		if (Find.Maps != null)
		{
			foreach (Map map in Find.Maps)
			{
				foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
				{
					if (TryPatchPawn(pawn, hediffDef))
					{
						count++;
					}
				}
			}
		}
		if (Find.WorldPawns != null)
		{
			foreach (Pawn pawn2 in Find.WorldPawns.AllPawnsAlive)
			{
				if (TryPatchPawn(pawn2, hediffDef))
				{
					count++;
				}
			}
		}
		Log.Message($"Compatibility patch run. Updated {count} personas.");
	}

	private bool TryPatchPawn(Pawn pawn, HediffDef def)
	{
		if (pawn?.health?.hediffSet == null)
		{
			return false;
		}
		if (pawn.health.hediffSet.GetFirstHediffOfDef(def) is Hediff_Persona hediff)
		{
			hediff.TalkInitiationWeight /= 2f;
			return true;
		}
		return false;
	}
}
