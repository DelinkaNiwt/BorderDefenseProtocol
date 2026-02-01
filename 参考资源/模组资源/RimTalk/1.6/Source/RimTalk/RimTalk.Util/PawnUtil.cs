using System;
using System.Collections.Generic;
using System.Linq;
using RimTalk.Data;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimTalk.Util;

public static class PawnUtil
{
	private static readonly HashSet<string> ResearchJobDefNames = new HashSet<string> { "Research", "RR_Analyse", "RR_AnalyseInPlace", "RR_AnalyseTerrain", "RR_Research", "RR_InterrogatePrisoner", "RR_LearnRemotely" };

	private static readonly string[] MovementJobPatterns = new string[4] { "Goto", "Flee", "Wait", "Wander" };

	public static bool IsTalkEligible(this Pawn pawn)
	{
		if (pawn.IsPlayer())
		{
			return true;
		}
		if (pawn.HasVocalLink())
		{
			return true;
		}
		if (pawn.DestroyedOrNull() || !pawn.Spawned || pawn.Dead)
		{
			return false;
		}
		if (!pawn.RaceProps.Humanlike)
		{
			return false;
		}
		if ((int)pawn.RaceProps.intelligence < 2)
		{
			return false;
		}
		if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
		{
			return false;
		}
		if (pawn.skills?.GetSkill(SkillDefOf.Social) == null)
		{
			return false;
		}
		RimTalkSettings settings = Settings.Get();
		if (!settings.AllowBabiesToTalk && pawn.IsBaby())
		{
			return false;
		}
		return pawn.IsFreeColonist || (settings.AllowSlavesToTalk && pawn.IsSlave) || (settings.AllowPrisonersToTalk && pawn.IsPrisoner) || (settings.AllowOtherFactionsToTalk && pawn.IsVisitor()) || (settings.AllowEnemiesToTalk && pawn.IsEnemy());
	}

	public static HashSet<Hediff> GetHediffs(this Pawn pawn)
	{
		return pawn?.health.hediffSet.hediffs.Where((Hediff hediff) => hediff.Visible).ToHashSet();
	}

	public static bool IsInDanger(this Pawn pawn, bool includeMentalState = false)
	{
		if (pawn == null || pawn.IsPlayer())
		{
			return false;
		}
		if (pawn.Dead)
		{
			return true;
		}
		if (pawn.Downed)
		{
			return true;
		}
		if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
		{
			return true;
		}
		if (pawn.InMentalState && includeMentalState)
		{
			return true;
		}
		if (pawn.IsBurning())
		{
			return true;
		}
		if (pawn.health.hediffSet.PainTotal >= pawn.GetStatValue(StatDefOf.PainShockThreshold))
		{
			return true;
		}
		if (pawn.health.hediffSet.BleedRateTotal > 0.3f)
		{
			return true;
		}
		if (pawn.IsInCombat())
		{
			return true;
		}
		if (pawn.CurJobDef == JobDefOf.Flee || pawn.CurJobDef == JobDefOf.FleeAndCower)
		{
			return true;
		}
		foreach (Hediff h in pawn.health.hediffSet.hediffs)
		{
			if (h.Visible)
			{
				HediffStage curStage = h.CurStage;
				if ((curStage != null && curStage.lifeThreatening) || (h.def.lethalSeverity > 0f && h.Severity > h.def.lethalSeverity * 0.8f))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsInCombat(this Pawn pawn)
	{
		if (pawn == null)
		{
			return false;
		}
		if (pawn.mindState.enemyTarget != null)
		{
			return true;
		}
		if (pawn.stances?.curStance is Stance_Busy { verb: not null })
		{
			return true;
		}
		Pawn hostilePawn = pawn.GetHostilePawnNearBy();
		return hostilePawn != null && pawn.Position.DistanceTo(hostilePawn.Position) <= 20f;
	}

	public static string GetRole(this Pawn pawn, bool includeFaction = false)
	{
		if (pawn == null)
		{
			return null;
		}
		if (pawn.IsPrisoner)
		{
			return "Prisoner";
		}
		if (pawn.IsSlave)
		{
			return "Slave";
		}
		if (pawn.IsEnemy())
		{
			if (pawn.GetMapRole() == MapRole.Invading)
			{
				return (includeFaction && pawn.Faction != null) ? ("Enemy Group(" + pawn.Faction.Name + ")") : "Enemy";
			}
			return "Enemy Defender";
		}
		if (pawn.IsVisitor())
		{
			return (includeFaction && pawn.Faction != null) ? ("Visitor Group(" + pawn.Faction.Name + ")") : "Visitor";
		}
		if (pawn.IsQuestLodger())
		{
			return "Lodger";
		}
		if (pawn.IsFreeColonist)
		{
			return (pawn.GetMapRole() == MapRole.Invading) ? "Invader" : "Colonist";
		}
		return null;
	}

	public static bool IsVisitor(this Pawn pawn)
	{
		return pawn?.Faction != null && pawn.Faction != Faction.OfPlayer && !pawn.HostileTo(Faction.OfPlayer) && !pawn.IsPrisoner;
	}

	public static bool IsEnemy(this Pawn pawn)
	{
		return pawn != null && pawn.HostileTo(Faction.OfPlayer) && !pawn.IsPrisoner;
	}

	public static bool IsBaby(this Pawn pawn)
	{
		Pawn_AgeTracker ageTracker = pawn.ageTracker;
		return ageTracker != null && ageTracker.CurLifeStage?.developmentalStage < DevelopmentalStage.Child;
	}

	public static (string, bool) GetPawnStatusFull(this Pawn pawn, List<Pawn> nearbyPawns)
	{
		RimTalkSettings settings = Settings.Get();
		if (pawn == null)
		{
			return (null, false);
		}
		if (pawn.IsPlayer())
		{
			return (settings.PlayerName, false);
		}
		bool isInDanger = false;
		List<string> lines = new List<string>();
		HashSet<Pawn> relevantPawns = CollectRelevantPawns(pawn, nearbyPawns);
		bool useOptimization = settings.Context.EnableContextOptimization;
		string pawnLabel = GetPawnLabel(pawn, relevantPawns, useOptimization);
		string pawnActivity = GetPawnActivity(pawn, relevantPawns, useOptimization);
		if (pawn.IsInDanger())
		{
			lines.Add(pawnLabel + " " + pawnActivity + " [IN DANGER]");
			isInDanger = true;
		}
		else
		{
			lines.Add(pawnLabel + " " + pawnActivity);
		}
		if (nearbyPawns != null && nearbyPawns.Any())
		{
			string nearbyList = GetCombinedNearbyList(pawn, nearbyPawns, relevantPawns, useOptimization, settings.Context.MaxPawnContextCount, ref isInDanger);
			lines.Add("Nearby: " + nearbyList);
		}
		else
		{
			lines.Add("Nearby people: none");
		}
		AddContextualInfo(pawn, lines, ref isInDanger);
		return (string.Join("\n", lines), isInDanger);
	}

	private static string GetCombinedNearbyList(Pawn mainPawn, List<Pawn> nearbyPawns, HashSet<Pawn> relevantPawns, bool useOptimization, int maxCount, ref bool situationIsCritical)
	{
		if (nearbyPawns == null || !nearbyPawns.Any())
		{
			return "none";
		}
		List<string> descriptions = new List<string>();
		bool localDangerFound = false;
		IEnumerable<Pawn> pawnsToScan = nearbyPawns.Take(maxCount);
		foreach (Pawn p in pawnsToScan)
		{
			string label = GetPawnLabel(p, relevantPawns, useOptimization);
			string extraStatus = "";
			if (p.IsInDanger(includeMentalState: true))
			{
				if (p.Faction == mainPawn.Faction)
				{
					localDangerFound = true;
				}
				extraStatus = " [!]";
			}
			PawnState pawnState = Cache.Get(p);
			string entry;
			if (pawnState != null)
			{
				string activity = GetPawnActivity(p, relevantPawns, useOptimization);
				string talkRequestStr = "";
				TalkRequest talkRequest = pawnState.GetNextTalkRequest();
				if (talkRequest != null)
				{
					pawnState.MarkRequestSpoken(talkRequest);
					talkRequestStr = " - " + talkRequest.Prompt;
				}
				entry = label + " " + activity.StripTags() + extraStatus + talkRequestStr;
			}
			else
			{
				entry = label + extraStatus;
			}
			descriptions.Add(entry);
		}
		if (localDangerFound)
		{
			situationIsCritical = true;
		}
		return "\n- " + string.Join("\n- ", descriptions);
	}

	private static HashSet<Pawn> CollectRelevantPawns(Pawn mainPawn, List<Pawn> nearbyPawns)
	{
		HashSet<Pawn> relevantPawns = new HashSet<Pawn> { mainPawn };
		if (mainPawn.CurJob != null)
		{
			AddJobTargetsToRelevantPawns(mainPawn.CurJob, relevantPawns);
		}
		if (nearbyPawns != null)
		{
			relevantPawns.UnionWith(nearbyPawns);
			foreach (Pawn nearby in nearbyPawns.Where((Pawn p) => p.CurJob != null))
			{
				AddJobTargetsToRelevantPawns(nearby.CurJob, relevantPawns);
			}
		}
		return relevantPawns;
	}

	private static string GetPawnLabel(Pawn pawn, HashSet<Pawn> relevantPawns, bool useOptimization)
	{
		if (useOptimization)
		{
			return pawn.LabelShort;
		}
		return relevantPawns.Contains(pawn) ? ContextHelper.GetDecoratedName(pawn) : pawn.LabelShort;
	}

	private static string GetPawnActivity(Pawn pawn, HashSet<Pawn> relevantPawns, bool useOptimization)
	{
		string activity = pawn.GetActivity();
		if (useOptimization || string.IsNullOrEmpty(activity))
		{
			return activity;
		}
		return DecorateText(activity, relevantPawns);
	}

	private static void AddContextualInfo(Pawn pawn, List<string> lines, ref bool isInDanger)
	{
		if (pawn.IsVisitor())
		{
			lines.Add("Visiting user colony");
			return;
		}
		if (pawn.IsFreeColonist && pawn.GetMapRole() == MapRole.Invading)
		{
			lines.Add("You are away from colony, attacking to capture enemy settlement");
			return;
		}
		if (pawn.IsEnemy())
		{
			if (pawn.GetMapRole() == MapRole.Invading)
			{
				LordJob lord = pawn.GetLord()?.LordJob;
				if (lord is LordJob_StageThenAttack || lord is LordJob_Siege)
				{
					lines.Add("waiting to invade user colony");
				}
				else
				{
					lines.Add("invading user colony");
				}
			}
			else
			{
				lines.Add("Fighting to protect your home from being captured");
			}
			return;
		}
		Pawn nearestHostile = pawn.GetHostilePawnNearBy();
		if (nearestHostile != null)
		{
			float distance = pawn.Position.DistanceTo(nearestHostile.Position);
			if (distance <= 10f)
			{
				lines.Add("Threat: Engaging in battle!");
			}
			else if (distance <= 20f)
			{
				lines.Add("Threat: Hostiles are dangerously close!");
			}
			else
			{
				lines.Add("Alert: hostiles in the area");
			}
			isInDanger = true;
		}
	}

	private static string DecorateText(string text, HashSet<Pawn> relevantPawns)
	{
		if (string.IsNullOrEmpty(text) || relevantPawns == null || !relevantPawns.Any())
		{
			return text;
		}
		var replacements = (from p in relevantPawns
			select new
			{
				Key = p.LabelShort,
				Value = ContextHelper.GetDecoratedName(p)
			} into x
			where !string.IsNullOrEmpty(x.Key)
			orderby x.Key.Length descending
			select x).ToList();
		return replacements.Aggregate(text, (string current, replacement) => current.Replace(replacement.Key, replacement.Value));
	}

	public static Pawn GetHostilePawnNearBy(this Pawn pawn)
	{
		if (pawn?.Map == null)
		{
			return null;
		}
		Faction referenceFaction = GetReferenceFaction(pawn);
		if (referenceFaction == null)
		{
			return null;
		}
		HashSet<IAttackTarget> hostileTargets = pawn.Map.attackTargetsCache?.TargetsHostileToFaction(referenceFaction);
		if (hostileTargets == null)
		{
			return null;
		}
		return FindClosestValidThreat(pawn, referenceFaction, hostileTargets);
	}

	private static Faction GetReferenceFaction(Pawn pawn)
	{
		if (pawn.IsPrisoner || pawn.IsSlave || pawn.IsFreeColonist || pawn.IsVisitor() || pawn.IsQuestLodger())
		{
			return Faction.OfPlayer;
		}
		return pawn.Faction;
	}

	private static Pawn FindClosestValidThreat(Pawn pawn, Faction referenceFaction, IEnumerable<IAttackTarget> hostileTargets)
	{
		Pawn closestPawn = null;
		float closestDistSq = float.MaxValue;
		foreach (IAttackTarget target in hostileTargets)
		{
			if (GenHostility.IsActiveThreatTo(target, referenceFaction) && target.Thing is Pawn { Downed: false } threatPawn && IsValidThreat(pawn, threatPawn))
			{
				float distSq = pawn.Position.DistanceToSquared(threatPawn.Position);
				if (distSq < closestDistSq)
				{
					closestDistSq = distSq;
					closestPawn = threatPawn;
				}
			}
		}
		return closestPawn;
	}

	private static bool IsValidThreat(Pawn observer, Pawn threat)
	{
		if (threat.IsPrisoner && threat.HostFaction == Faction.OfPlayer)
		{
			return false;
		}
		if (threat.IsSlave && threat.HostFaction == Faction.OfPlayer)
		{
			return false;
		}
		if (observer.IsPrisoner && threat.IsPrisoner)
		{
			return false;
		}
		Lord lord = threat.GetLord();
		bool flag;
		if (lord != null)
		{
			LordToil curLordToil = lord.CurLordToil;
			if (curLordToil is LordToil_ExitMapFighting || curLordToil is LordToil_ExitMap)
			{
				flag = true;
				goto IL_0093;
			}
		}
		flag = false;
		goto IL_0093;
		IL_0093:
		if (flag)
		{
			return false;
		}
		Job curJob = threat.CurJob;
		if (curJob != null && curJob.exitMapOnArrival)
		{
			return false;
		}
		if (threat.RaceProps.IsMechanoid && lord != null && lord.CurLordToil is LordToil_DefendPoint)
		{
			return false;
		}
		return true;
	}

	internal static string GetActivity(this Pawn pawn)
	{
		if (pawn == null)
		{
			return null;
		}
		if (pawn.InMentalState)
		{
			return pawn.MentalState?.InspectLine;
		}
		if (pawn.CurJobDef == null)
		{
			return null;
		}
		string target = ((!pawn.IsAttacking()) ? null : pawn.TargetCurrentlyAimingAt.Thing?.LabelShortCap);
		if (target != null)
		{
			return "Attacking " + target;
		}
		string lord = pawn.GetLord()?.LordJob?.GetReport(pawn);
		string job = pawn.jobs?.curDriver?.GetReport();
		string activity = ((lord == null) ? job : ((job == null) ? lord : (lord + " (" + job + ")")));
		if (ResearchJobDefNames.Contains(pawn.CurJob?.def.defName))
		{
			activity = AppendResearchProgress(activity);
		}
		Pawn_PathFollower pather = pawn.pather;
		if (pather != null && pather.Moving && pawn.CurJob != null && !Near(pawn.CurJob.targetA) && !Near(pawn.CurJob.targetB) && !Near(pawn.CurJob.targetC) && !MovementJobPatterns.Any((string p) => pawn.CurJob.def.defName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0))
		{
			activity = "(traveling to) " + activity;
		}
		return activity;
		bool Near(LocalTargetInfo t)
		{
			return t.IsValid && pawn.Position.InHorDistOf(t.Cell, 5f);
		}
	}

	private static string AppendResearchProgress(string activity)
	{
		ResearchProjectDef project = Find.ResearchManager.GetProject();
		if (project == null)
		{
			return activity;
		}
		float progress = Find.ResearchManager.GetProgress(project);
		float percentage = progress / project.baseCost * 100f;
		return $"{activity} (Project: {project.label} - {percentage:F0}%)";
	}

	private static void AddJobTargetsToRelevantPawns(Job job, HashSet<Pawn> relevantPawns)
	{
		if (job == null)
		{
			return;
		}
		foreach (TargetIndex index in Enum.GetValues(typeof(TargetIndex)))
		{
			try
			{
				LocalTargetInfo target = job.GetTarget(index);
				if (!(target == null) && target.HasThing && target.Thing is Pawn pawn && relevantPawns.Add(pawn) && pawn.CurJob != null)
				{
					AddJobTargetsToRelevantPawns(pawn.CurJob, relevantPawns);
				}
			}
			catch
			{
			}
		}
	}

	public static MapRole GetMapRole(this Pawn pawn)
	{
		if (pawn?.Map == null || pawn.IsPrisonerOfColony)
		{
			return MapRole.None;
		}
		Map map = pawn.Map;
		Faction mapFaction = map.ParentFaction;
		if (mapFaction == pawn.Faction || (map.IsPlayerHome && pawn.Faction == Faction.OfPlayer))
		{
			return MapRole.Defending;
		}
		if (pawn.Faction.HostileTo(mapFaction))
		{
			return MapRole.Invading;
		}
		return MapRole.Visiting;
	}

	public static string GetPrisonerSlaveStatus(this Pawn pawn)
	{
		if (pawn == null)
		{
			return null;
		}
		List<string> lines = new List<string>();
		if (pawn.IsPrisoner)
		{
			float resistance = pawn.guest.resistance;
			lines.Add($"Resistance: {resistance:0.0} ({Describer.Resistance(resistance)})");
			float will = pawn.guest.will;
			lines.Add($"Will: {will:0.0} ({Describer.Will(will)})");
		}
		else if (pawn.IsSlave)
		{
			Need_Suppression suppressionNeed = pawn.needs?.TryGetNeed<Need_Suppression>();
			if (suppressionNeed != null)
			{
				float suppression = suppressionNeed.CurLevelPercentage * 100f;
				lines.Add($"Suppression: {suppression:0.0}% ({Describer.Suppression(suppression)})");
			}
		}
		return lines.Any() ? string.Join("\n", lines) : null;
	}

	public static bool IsPlayer(this Pawn pawn)
	{
		return pawn == Cache.GetPlayer();
	}

	public static bool HasVocalLink(this Pawn pawn)
	{
		return Settings.Get().AllowNonHumanToTalk && pawn.health.hediffSet.HasHediff(Constant.VocalLinkDef);
	}
}
