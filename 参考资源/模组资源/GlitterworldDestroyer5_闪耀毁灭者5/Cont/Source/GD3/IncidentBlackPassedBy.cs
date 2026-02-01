using System;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace GD3
{
	[UsedImplicitly]
	public class IncidentBlacKPassedBy : IncidentWorker_TravelerGroup
	{
		protected override PawnGroupKindDef PawnGroupKindDef
		{
			get
			{
				return PawnGroupKindDefOf.Combat;
			}
		}
		protected override bool CanFireNowSub(IncidentParms parms)
		{
			if (GDUtility.MissionComponent.scriptEnded)
            {
				return false;
            }
			Map map = (Map)parms.target;
			List<Thing> list = map.listerThings.ThingsOfDef(GDDefOf.Mech_BlackApocriton);
			return Find.FactionManager.FirstFactionOfDef(GDDefOf.BlackMechanoid).HostileTo(Faction.OfPlayer) && list.Count == 0 && Find.World.GetComponent<MissionComponent>().blackMechDiscoverd && !Find.World.GetComponent<MissionComponent>().scriptEnded;
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
        {
			Map map = (Map)parms.target;
			if (!TryResolveParms(parms))
			{
				return false;
			}

			if (!RCellFinder.TryFindTravelDestFrom(parms.spawnCenter, map, out IntVec3 travelDest))
			{
				Log.Warning(string.Concat("Failed to do traveler incident from ", parms.spawnCenter, ": Couldn't find anywhere for the traveler to go."));
				return false;
			}

			List<Pawn> list = SpawnPawns(parms);
			if (list.Count == 0)
			{
				return false;
			}

			string text;
			if (list.Count == 1)
			{
				text = "SingleTravelerPassing".Translate(list[0].story.Title, parms.faction.Name, list[0].Name.ToStringFull, list[0].Named("PAWN"));
				text = text.AdjustedFor(list[0]);
			}
			else
			{
				text = "GroupTravelersPassing".Translate(parms.faction.Name);
			}

			Messages.Message(text, list[0], MessageTypeDefOf.NeutralEvent);
			LordJob_TravelAndExit lordJob = new LordJob_TravelAndExit(travelDest);
			LordMaker.MakeNewLord(parms.faction, lordJob, map, list);
			PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(list, "LetterRelatedPawnsNeutralGroup".Translate(Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent, informEvenIfSeenBefore: true);
			Find.LetterStack.ReceiveLetter("BlackPassed".Translate(), "BlackPassedDesc".Translate(), LetterDefOf.ThreatBig, new LookTargets(parms.spawnCenter, map));
			return true;
		}

		public override bool FactionCanBeGroupSource(Faction f, IncidentParms parms, bool desperate = false)
		{
			return f.def == FactionDef.Named("BlackMechanoid");
		}
		
		protected override void ResolveParmsPoints(IncidentParms parms)
		{
			parms.points = 100000f;
		}

	}

}
