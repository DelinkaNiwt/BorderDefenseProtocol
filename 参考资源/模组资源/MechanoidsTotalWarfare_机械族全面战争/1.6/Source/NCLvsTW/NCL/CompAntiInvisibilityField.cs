using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCL;

public class CompAntiInvisibilityField : ThingComp
{
	private HashSet<Pawn> affectedPawns = new HashSet<Pawn>();

	private int nextCheckTick;

	private Effecter activeEffecter;

	private bool isActivated;

	public CompProperties_AntiInvisibilityField Props => (CompProperties_AntiInvisibilityField)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		isActivated = Props.startActivated;
		nextCheckTick = Find.TickManager.TicksGame + Props.checkIntervalTicks;
	}

	public void ToggleActivation()
	{
		isActivated = !isActivated;
		if (!isActivated)
		{
			affectedPawns.Clear();
			activeEffecter?.Cleanup();
			activeEffecter = null;
		}
	}

	private bool HasPower()
	{
		if (!Props.requiresPower)
		{
			return true;
		}
		return parent.GetComp<CompPowerTrader>()?.PowerOn ?? false;
	}

	public override void CompTick()
	{
		if (!isActivated || !HasPower())
		{
			affectedPawns.Clear();
			activeEffecter?.Cleanup();
			activeEffecter = null;
		}
		else if (!HasPower())
		{
			affectedPawns.Clear();
			activeEffecter?.Cleanup();
			activeEffecter = null;
		}
		else if (parent.Spawned && Find.TickManager.TicksGame >= nextCheckTick)
		{
			CheckArea();
			nextCheckTick = Find.TickManager.TicksGame + Props.checkIntervalTicks;
		}
	}

	private void CheckArea()
	{
		if (!isActivated || !parent.Spawned || !HasPower() || !parent.Spawned || !HasPower() || !parent.Spawned)
		{
			return;
		}
		affectedPawns.RemoveWhere((Pawn p) => p == null || !p.Spawned || p.Position.DistanceTo(parent.Position) > Props.effectiveRadius);
		foreach (Thing thing in GenRadial.RadialDistinctThingsAround(parent.Position, parent.Map, Props.effectiveRadius, Props.affectsThroughWalls))
		{
			if (thing is Pawn pawn && ShouldAffect(pawn))
			{
				HandleInvisiblePawn(pawn);
			}
		}
		UpdateOngoingEffects();
	}

	private bool ShouldAffect(Pawn pawn)
	{
		if (pawn == null || !pawn.Spawned || pawn.health?.hediffSet == null)
		{
			return false;
		}
		if (!Props.affectsAllFactions && pawn.Faction == parent.Faction)
		{
			return false;
		}
		return IsEffectivelyInvisible(pawn);
	}

	private bool IsEffectivelyInvisible(Pawn pawn)
	{
		foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
		{
			HediffComp_Invisibility invisComp = hediff.TryGetComp<HediffComp_Invisibility>();
			if (invisComp != null && !invisComp.PsychologicallyVisible)
			{
				return true;
			}
		}
		if (pawn.mindState != null && pawn.mindState.lastBecameInvisibleTick > pawn.mindState.lastBecameVisibleTick)
		{
			return true;
		}
		if (pawn.Drawer?.renderer?.GetType().Name.Contains("Invisible") == true)
		{
			return true;
		}
		return false;
	}

	private void HandleInvisiblePawn(Pawn pawn)
	{
		bool wasDisrupted = false;
		foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
		{
			HediffComp_Invisibility invisComp = hediff.TryGetComp<HediffComp_Invisibility>();
			if (invisComp != null)
			{
				invisComp.DisruptInvisibility();
				wasDisrupted = true;
			}
		}
		if (!wasDisrupted && pawn.mindState != null)
		{
			pawn.mindState.lastBecameVisibleTick = Find.TickManager.TicksGame;
			pawn.Notify_BecameVisible();
			wasDisrupted = true;
		}
		if (wasDisrupted)
		{
			affectedPawns.Add(pawn);
			PlayEffects(pawn);
			ApplyAdditionalEffects(pawn);
		}
	}

	private void PlayEffects(Pawn pawn)
	{
		Props.instantEffecterDef?.Spawn(pawn, parent.Map)?.Cleanup();
		if (Props.continuousEffecterDef != null)
		{
			if (activeEffecter == null)
			{
				activeEffecter = Props.continuousEffecterDef.Spawn();
			}
			activeEffecter.EffectTick(pawn, parent);
		}
	}

	private void ApplyAdditionalEffects(Pawn pawn)
	{
		if (Props.applyHediff != null)
		{
			pawn.health.AddHediff(Props.applyHediff);
		}
		if (Props.soundOnReveal != null)
		{
			Props.soundOnReveal.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
		}
	}

	private void UpdateOngoingEffects()
	{
		if (activeEffecter != null && affectedPawns.Count == 0)
		{
			activeEffecter.Cleanup();
			activeEffecter = null;
		}
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		affectedPawns.Clear();
		activeEffecter?.Cleanup();
		activeEffecter = null;
		base.PostDestroy(mode, previousMap);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (parent.Faction == Faction.OfPlayer)
		{
			yield return new Command_Toggle
			{
				defaultLabel = Props.toggleCommandLabel,
				defaultDesc = Props.toggleCommandDesc,
				icon = ContentFinder<Texture2D>.Get("ModIcon/BanInvisibility"),
				isActive = () => isActivated,
				toggleAction = ToggleActivation
			};
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref isActivated, "isActivated", Props.startActivated);
	}

	public override void PostDraw()
	{
		base.PostDraw();
		if (!isActivated || !HasPower() || !Props.drawRadius || !Find.Selector.SelectedObjects.Contains(parent))
		{
			return;
		}
		GenDraw.DrawRadiusRing(parent.Position, Props.effectiveRadius, Props.radiusColor);
		if (!Props.drawLines)
		{
			return;
		}
		foreach (Pawn pawn in affectedPawns)
		{
			if (pawn.Spawned && pawn.Map == parent.Map)
			{
				GenDraw.DrawLineBetween(pawn.DrawPos, parent.DrawPos);
			}
		}
	}
}
