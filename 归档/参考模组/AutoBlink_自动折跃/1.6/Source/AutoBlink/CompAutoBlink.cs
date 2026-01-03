using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AutoBlink;

[StaticConstructorOnStartup]
public class CompAutoBlink : ThingComp, IExposable
{
	public bool autoBlinkMaster = true;

	public bool autoBlinkDrafted = true;

	public bool autoBlinkIdle = true;

	public bool jumpAsFarAsPossible = true;

	private static readonly Texture2D AutoBlinkIcon = ContentFinder<Texture2D>.Get("UI/Commands/AutoBlink");

	private static readonly List<Pawn> tmpSelPawns = new List<Pawn>();

	private static readonly List<CompAutoBlink> tmpAllComps = new List<CompAutoBlink>();

	public bool canBlink;

	public int lastBlinkTick;

	private int scheduledBlinkTick = -1;

	private bool scheduledFarBlink = false;

	private Hediff linkedHediff;

	public int localMinDistanceToBlink = 0;

	private Pawn pawn;

	private Pawn_PathFollower pather;

	public CompProperties_AutoBlink Props => (CompProperties_AutoBlink)props;

	public int CurrentMinBlinkDist
	{
		get
		{
			if (localMinDistanceToBlink > 0)
			{
				return localMinDistanceToBlink;
			}
			int minDistanceToBlink = Props.minDistanceToBlink;
			if (minDistanceToBlink > 0)
			{
				return minDistanceToBlink;
			}
			return (Props.maxDistanceToBlink > 0) ? Props.maxDistanceToBlink : 400;
		}
		set
		{
			int max = ((Props.maxDistanceToBlink > 0) ? Props.maxDistanceToBlink : 400);
			int num = Mathf.Clamp(value, 2, max);
			localMinDistanceToBlink = num;
		}
	}

	public void InitFromHediff(HediffComp_AutoBlink src)
	{
		linkedHediff = src?.parent;
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		pawn = parent as Pawn;
		pather = pawn?.pather;
		if (!(props is CompProperties_AutoBlink compProperties_AutoBlink))
		{
			pawn.AllComps.Remove(this);
			return;
		}
		compProperties_AutoBlink.InitCaches();
		if (!respawningAfterLoad)
		{
			autoBlinkDrafted = compProperties_AutoBlink.defaultAutoBlinkDrafted;
			autoBlinkIdle = compProperties_AutoBlink.defaultAutoBlinkIdle;
			jumpAsFarAsPossible = compProperties_AutoBlink.defaultJumpAsFarAsPossible;
		}
		canBlink = true;
		scheduledBlinkTick = -1;
		lastBlinkTick = 0;
	}

	public override void CompTick()
	{
		if (pawn?.Map == null)
		{
			return;
		}
		if (linkedHediff != null && parent.IsHashIntervalTick(500))
		{
			List<Hediff> list = linkedHediff.pawn?.health?.hediffSet?.hediffs;
			if (list == null || !list.Contains(linkedHediff))
			{
				(Current.Game?.GetComponent<AutoBlinkRemovalComponent>())?.EnqueueForRemoval(this);
				return;
			}
		}
		if (!parent.IsHashIntervalTick(12) || !autoBlinkMaster || pawn.Dead || pawn.Downed || (Props.playerFactionOnly && pawn.Faction != Faction.OfPlayer))
		{
			return;
		}
		if (pather == null)
		{
			pather = pawn.pather;
			if (pather == null)
			{
				return;
			}
		}
		if (!(pawn.Drafted ? autoBlinkDrafted : autoBlinkIdle))
		{
			return;
		}
		HashSet<JobDef> allowedJobDefsCached = Props.allowedJobDefsCached;
		if (allowedJobDefsCached != null && allowedJobDefsCached.Count > 0 && (pawn.CurJob == null || !allowedJobDefsCached.Contains(pawn.CurJob.def)))
		{
			return;
		}
		HashSet<JobDef> excludedJobDefsCached = Props.excludedJobDefsCached;
		if (excludedJobDefsCached == null || (pawn.CurJob != null && excludedJobDefsCached.Contains(pawn.CurJob.def)))
		{
			return;
		}
		int ticksGame = Find.TickManager.TicksGame;
		if (scheduledBlinkTick >= 0)
		{
			if (pather.Moving)
			{
				Pawn_StanceTracker stances = pawn.stances;
				if (stances == null || !stances.FullBodyBusy)
				{
					LocalTargetInfo destination = pather.Destination;
					if (!destination.IsValid)
					{
						scheduledBlinkTick = -1;
						return;
					}
					PawnPath curPath = pather.curPath;
					if (curPath == null)
					{
						scheduledBlinkTick = -1;
						scheduledFarBlink = false;
						return;
					}
					int nodesLeftCount = curPath.NodesLeftCount;
					if (nodesLeftCount < CurrentMinBlinkDist)
					{
						scheduledBlinkTick = -1;
						scheduledFarBlink = false;
					}
					else if (ticksGame >= scheduledBlinkTick)
					{
						ExecuteBlink(destination, ticksGame);
					}
					return;
				}
			}
			scheduledBlinkTick = -1;
		}
		else
		{
			if (!pather.Moving)
			{
				return;
			}
			Pawn_RopeTracker roping = pawn.roping;
			if (roping != null && roping.Ropees?.Count > 0)
			{
				return;
			}
			LocalTargetInfo destination2 = pather.Destination;
			if (!destination2.IsValid || destination2.Cell == pawn.Position)
			{
				return;
			}
			PawnPath curPath2 = pather.curPath;
			int num = 1;
			if (curPath2 == null || curPath2.NodesLeftCount <= num || ticksGame - lastBlinkTick < Props.blinkIntervalTicks)
			{
				return;
			}
			int currentMinBlinkDist = CurrentMinBlinkDist;
			int num2 = ((Props.maxDistanceToBlink > 0) ? Props.maxDistanceToBlink : 400);
			IntVec3 position = pawn.Position;
			IntVec3 cell = destination2.Cell;
			int num3 = position.x - cell.x;
			int num4 = position.z - cell.z;
			int num5 = num3 * num3 + num4 * num4;
			if (curPath2 == null || curPath2.NodesLeftCount < currentMinBlinkDist)
			{
				return;
			}
			bool flag = Props.maxDistanceToBlink > 0 && num5 > num2 * num2;
			if (!flag || jumpAsFarAsPossible)
			{
				Pawn_StanceTracker stances2 = pawn.stances;
				if ((stances2 == null || !stances2.FullBodyBusy) && !WillCollideWithPawnAt(pawn, pawn.Position, onlyStanding: true))
				{
					scheduledBlinkTick = ticksGame + Props.delayAfterEligibleTicks;
					scheduledFarBlink = flag;
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ExecuteBlink(LocalTargetInfo currentDest, int now)
	{
		int maxDist = ((Props.maxDistanceToBlink > 0) ? Props.maxDistanceToBlink : 400);
		IntVec3 position = (scheduledFarBlink ? FindBlinkCellAlongPath(maxDist) : FindBlinkCell(currentDest.Cell, Props.cellsBeforeTarget));
		PlayFX(Props.preSoundsCached, Props.preMotesCached, Props.preEffectsCached);
		pawn.Position = position;
		PawnPath curPath = pather.curPath;
		IntVec3 intVec = ((curPath == null || curPath.NodesLeftCount <= 0) ? pather.Destination.Cell : curPath.Peek(curPath.NodesLeftCount - 1));
		pawn.Notify_Teleported(endCurrentJob: false);
		pather.StartPath(intVec, PathEndMode.OnCell);
		PlayFX(Props.postSoundsCached, Props.postMotesCached, Props.postEffectsCached);
		lastBlinkTick = now;
		int ticks = ((Props.postBlinkStanceTicks >= 0) ? Props.postBlinkStanceTicks : Mathf.Max(Props.blinkIntervalTicks / 3, 1));
		pawn.stances.SetStance(new Stance_Cooldown(ticks, pawn, null));
		OnBlinkSucceeded();
		scheduledBlinkTick = -1;
		scheduledFarBlink = false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public void OnBlinkSucceeded()
	{
	}

	private IntVec3 FindBlinkCellAlongPath(int maxDist)
	{
		PawnPath pawnPath = pawn.pather?.curPath;
		if (pawnPath != null && pawnPath.NodesLeftCount > 1)
		{
			int nodesAhead = Mathf.Clamp(maxDist, 1, pawnPath.NodesLeftCount - 1);
			return pawnPath.Peek(nodesAhead);
		}
		return pawn.Position;
	}

	private IntVec3 FindBlinkCell(IntVec3 dest, int cellsBefore)
	{
		PawnPath pawnPath = pawn.pather?.curPath;
		if (pawnPath != null && pawnPath.NodesLeftCount > cellsBefore)
		{
			return pawnPath.Peek(pawnPath.NodesLeftCount - cellsBefore - 1);
		}
		return dest;
	}

	public bool CanBlinkToCell(IntVec3 cell)
	{
		if (!pawn.Spawned)
		{
			return false;
		}
		if (!cell.InBounds(pawn.Map) || !cell.Standable(pawn.Map))
		{
			return false;
		}
		int num = ((Props.maxDistanceToBlink > 0) ? Props.maxDistanceToBlink : 400);
		return pawn.Position.DistanceTo(cell) <= (float)num;
	}

	public void BlinkToCellDirect(IntVec3 cell)
	{
		if (CanBlinkToCell(cell))
		{
			int ticksGame = Find.TickManager.TicksGame;
			if (ticksGame - lastBlinkTick >= Props.blinkIntervalTicks)
			{
				PlayFX(Props.preSoundsCached, Props.preMotesCached, Props.preEffectsCached);
				pawn.pather.StopDead();
				pawn.jobs.StopAll();
				pawn.Position = cell;
				pawn.Notify_Teleported(endCurrentJob: false);
				PlayFX(Props.postSoundsCached, Props.postMotesCached, Props.postEffectsCached);
				lastBlinkTick = ticksGame;
				int ticks = ((Props.postBlinkStanceTicks >= 0) ? Props.postBlinkStanceTicks : Mathf.Max(Props.blinkIntervalTicks / 3, 1));
				pawn.stances.SetStance(new Stance_Cooldown(ticks, pawn, null));
				OnBlinkSucceeded();
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool WillCollideWithPawnAt(Pawn pawn, IntVec3 c, bool onlyStanding)
	{
		if (!PawnUtility.ShouldCollideWithPawns(pawn))
		{
			return false;
		}
		return PawnUtility.AnyPawnBlockingPathAt(c, pawn, actAsIfHadCollideWithPawnsJob: false, onlyStanding);
	}

	private void PlayFX(List<SoundDef> sounds, List<ThingDef> motes, List<EffecterDef> effs)
	{
		if (!pawn.Spawned || !pawn.PositionHeld.ShouldSpawnMotesAt(pawn.MapHeld, drawOffscreen: false))
		{
			return;
		}
		if (sounds != null)
		{
			foreach (SoundDef sound in sounds)
			{
				sound.PlayOneShot(pawn);
			}
		}
		if (motes != null)
		{
			foreach (ThingDef mote in motes)
			{
				MoteMaker.MakeStaticMote(pawn.PositionHeld, pawn.MapHeld, mote, 0.5f);
			}
		}
		if (effs == null)
		{
			return;
		}
		foreach (EffecterDef eff in effs)
		{
			eff.Spawn(pawn.PositionHeld, pawn.MapHeld).Cleanup();
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (!Props.drawGizmo)
		{
			yield break;
		}
		Pawn pawn = parent as Pawn;
		if (pawn != null && pawn.Faction == Faction.OfPlayer)
		{
			if (pawn.RaceProps.Humanlike)
			{
				if (!pawn.IsColonistPlayerControlled)
				{
					yield break;
				}
			}
			else if (pawn.RaceProps.IsMechanoid && !pawn.IsColonyMechPlayerControlled)
			{
				yield break;
			}
		}
		foreach (Gizmo item in base.CompGetGizmosExtra() ?? Enumerable.Empty<Gizmo>())
		{
			yield return item;
		}
		tmpSelPawns.Clear();
		foreach (object obj in Find.Selector.SelectedObjects)
		{
			if (obj is Pawn p)
			{
				tmpSelPawns.Add(p);
			}
		}
		if (!tmpSelPawns.Contains(pawn))
		{
			yield break;
		}
		if (tmpSelPawns.Count == 1)
		{
			if (pawn.Faction == Faction.OfPlayer && canBlink)
			{
				yield return CreateGizmo();
			}
			yield break;
		}
		tmpAllComps.Clear();
		foreach (Pawn selPawn in tmpSelPawns)
		{
			foreach (ThingComp comp in selPawn.AllComps)
			{
				if (comp is CompAutoBlink blinkComp)
				{
					tmpAllComps.Add(blinkComp);
				}
			}
		}
		CompAutoBlink master = ((tmpAllComps.Count > 0) ? tmpAllComps[0] : null);
		if (this == master && pawn.Faction == Faction.OfPlayer && canBlink)
		{
			yield return CreateGizmo();
		}
	}

	private Gizmo CreateGizmo()
	{
		string itemPath = ((!string.IsNullOrEmpty(Props?.gizmoIconPath)) ? Props.gizmoIconPath : "UI/Commands/AutoBlink");
		Texture2D icon;
		try
		{
			icon = ContentFinder<Texture2D>.Get(itemPath);
		}
		catch
		{
			icon = ContentFinder<Texture2D>.Get("UI/Commands/AutoBlink");
		}
		TaggedString taggedString = ((!string.IsNullOrEmpty(Props.gizmoLabel)) ? new TaggedString(Props.gizmoLabel) : "AutoBlink.AutoBlink_Label".Translate());
		return new Command_AutoBlinkRoot
		{
			Comp = this,
			icon = icon,
			defaultLabel = taggedString,
			defaultDesc = "AutoBlink.AutoBlink_Desc".Translate(),
			isActive = () => autoBlinkMaster,
			toggleAction = delegate
			{
				autoBlinkMaster = !autoBlinkMaster;
			}
		};
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref scheduledBlinkTick, "scheduledBlinkTick", -1);
		Scribe_Values.Look(ref lastBlinkTick, "lastBlinkTick", 0);
		Scribe_Values.Look(ref autoBlinkMaster, "autoBlinkMaster", defaultValue: true);
		Scribe_Values.Look(ref autoBlinkDrafted, "autoBlinkDrafted", defaultValue: true);
		Scribe_Values.Look(ref autoBlinkIdle, "autoBlinkIdle", defaultValue: true);
		Scribe_Values.Look(ref jumpAsFarAsPossible, "jumpAsFarAsPossible", defaultValue: true);
		Scribe_Values.Look(ref localMinDistanceToBlink, "localMinDistanceToBlink", 0);
		Scribe_Values.Look(ref scheduledFarBlink, "scheduledFarBlink", defaultValue: false);
		Scribe_References.Look(ref linkedHediff, "linkedHediff");
	}

	public void ExposeData()
	{
		PostExposeData();
	}
}
