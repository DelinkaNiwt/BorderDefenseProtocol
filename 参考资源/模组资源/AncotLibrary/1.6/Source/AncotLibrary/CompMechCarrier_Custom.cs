using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace AncotLibrary;

public class CompMechCarrier_Custom : CompThingCarrier_Custom
{
	private Texture2D SpawnGizmoIcon;

	private Texture2D RecoverGizmoIcon;

	private int cooldownTicksRemaining;

	public List<Pawn> spawnedPawns = new List<Pawn>();

	private List<Thing> tmpResources = new List<Thing>();

	public CompProperties_MechCarrier_Custom Props_Mech => (CompProperties_MechCarrier_Custom)props;

	public override string GizmoDesc => "MechCarrierAutofillDesc".Translate(parent.LabelCap, Props_Mech.spawnPawnKind.labelPlural);

	public virtual int CostPerPawn => Props_Mech.costPerPawn;

	public virtual int CooldownTicks => Props_Mech.cooldownTicks;

	public virtual int RecoverTicks => 18000;

	public virtual float RecoverFactor => Props_Mech.recoverFactor;

	public CompCommandPivot compCommandPivot => parent.TryGetComp<CompCommandPivot>();

	public Pawn pivot => parent as Pawn;

	public virtual PawnKindDef SpawnPawnKind => Props_Mech.spawnPawnKind;

	public AcceptanceReport CanSpawn
	{
		get
		{
			if (pivot != null)
			{
				if (pivot.IsSelfShutdown())
				{
					return "SelfShutdown".Translate();
				}
				if (pivot.Faction == Faction.OfPlayer && !pivot.IsColonyMechPlayerControlled)
				{
					return false;
				}
				if (!pivot.Awake() || pivot.Downed || pivot.Dead || !pivot.Spawned)
				{
					return false;
				}
			}
			if (MaxCanSpawn <= 0)
			{
				return "MechCarrierNotEnoughResources".Translate();
			}
			if (cooldownTicksRemaining > 0)
			{
				return "CooldownTime".Translate() + " " + cooldownTicksRemaining.ToStringSecondsFromTicks();
			}
			return true;
		}
	}

	public AcceptanceReport CanRecover
	{
		get
		{
			if (spawnedPawns.NullOrEmpty())
			{
				return "Ancot.MechCarrierNoPawnToRecover".Translate();
			}
			if (cooldownTicksRemaining > 0)
			{
				return "CooldownTime".Translate() + " " + cooldownTicksRemaining.ToStringSecondsFromTicks();
			}
			return true;
		}
	}

	public int MaxCanSpawn => Mathf.Min(Mathf.FloorToInt(base.IngredientCount / CostPerPawn), Props_Mech.maxPawnsToSpawn);

	public override void Initialize(CompProperties props)
	{
		if (!ModLister.CheckBiotech("Mech carrier"))
		{
			parent.Destroy();
		}
		else
		{
			base.Initialize(props);
		}
	}

	public void TrySpawnPawns()
	{
		int maxCanSpawn = MaxCanSpawn;
		if (maxCanSpawn <= 0)
		{
			return;
		}
		PawnGenerationRequest request = new PawnGenerationRequest(SpawnPawnKind, parent.Faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Newborn);
		tmpResources.Clear();
		tmpResources.AddRange(innerContainer);
		Lord lord = ((parent is Pawn p) ? p.GetLord() : null);
		for (int i = 0; i < maxCanSpawn; i++)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(request);
			GenSpawn.Spawn(pawn, parent.Position, parent.Map);
			if (Props_Mech.hediffAddToSpawnPawn != null)
			{
				pawn.health.AddHediff(Props_Mech.hediffAddToSpawnPawn);
			}
			CompCommandTerminal compCommandTerminal = pawn.TryGetComp<CompCommandTerminal>();
			if (compCommandTerminal != null)
			{
				compCommandTerminal.sortie_Terminal = compCommandPivot.sortie;
				if (pawn != null)
				{
					compCommandTerminal.pivot = pivot;
				}
			}
			spawnedPawns.Add(pawn);
			lord?.AddPawn(pawn);
			int num = CostPerPawn;
			for (int j = 0; j < tmpResources.Count; j++)
			{
				if (innerContainer.Contains(tmpResources[j]))
				{
					Thing thing = innerContainer.Take(tmpResources[j], Mathf.Min(tmpResources[j].stackCount, num));
					num -= thing.stackCount;
					thing.Destroy();
					if (num <= 0)
					{
						break;
					}
				}
			}
			if (Props_Mech.spawnedMechEffecter != null)
			{
				Effecter effecter = new Effecter(Props_Mech.spawnedMechEffecter);
				effecter.Trigger(Props_Mech.attachSpawnedMechEffecter ? ((TargetInfo)pawn) : new TargetInfo(pawn.Position, pawn.Map), TargetInfo.Invalid);
				effecter.Cleanup();
			}
		}
		tmpResources.Clear();
		StartCooldown(CooldownTicks);
		if (Props_Mech.spawnEffecter != null)
		{
			Effecter effecter2 = new Effecter(Props_Mech.spawnEffecter);
			effecter2.Trigger(Props_Mech.attachSpawnedEffecter ? ((TargetInfo)parent) : new TargetInfo(parent.Position, parent.Map), TargetInfo.Invalid);
			effecter2.Cleanup();
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		Pawn pawn2;
		Pawn pawn = (pawn2 = parent as Pawn);
		if (pawn2 == null || !pawn.IsColonyMech || pawn.GetOverseer() == null)
		{
			yield break;
		}
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		AcceptanceReport canSpawn = CanSpawn;
		if ((object)SpawnGizmoIcon == null)
		{
			SpawnGizmoIcon = ContentFinder<Texture2D>.Get(Props_Mech.iconPath);
		}
		Command_ActionWithCooldown act = new Command_ActionWithCooldown
		{
			cooldownPercentGetter = () => Mathf.InverseLerp(CooldownTicks, 0f, cooldownTicksRemaining),
			action = delegate
			{
				TrySpawnPawns();
			},
			hotKey = KeyBindingDefOf.Misc2,
			Disabled = !canSpawn.Accepted,
			disabledReason = canSpawn.Reason,
			icon = SpawnGizmoIcon,
			defaultLabel = "MechCarrierRelease".Translate(SpawnPawnKind.labelPlural),
			defaultDesc = "MechCarrierDesc".Translate(Props_Mech.maxPawnsToSpawn, SpawnPawnKind.labelPlural, SpawnPawnKind.label, CostPerPawn, base.Props.fixedIngredient.label)
		};
		if (!canSpawn.Reason.NullOrEmpty())
		{
			act.Disable(canSpawn.Reason);
		}
		AcceptanceReport canRecover = CanRecover;
		if ((object)RecoverGizmoIcon == null)
		{
			RecoverGizmoIcon = ContentFinder<Texture2D>.Get(Props_Mech.iconPathRecover);
		}
		Command_ActionWithCooldown act2 = new Command_ActionWithCooldown
		{
			cooldownPercentGetter = () => Mathf.InverseLerp(CooldownTicks, 0f, cooldownTicksRemaining),
			action = delegate
			{
				foreach (Pawn spawnedPawn in spawnedPawns)
				{
					if (!spawnedPawn.Downed && spawnedPawn.Awake())
					{
						if (base.IngredientCount < base.Props.maxIngredientCount)
						{
							Thing thing = ThingMaker.MakeThing(base.Props.fixedIngredient);
							int stackCount = (int)(RecoverFactor * (float)CostPerPawn * spawnedPawn.health.summaryHealth.SummaryHealthPercent);
							thing.stackCount = stackCount;
							innerContainer.TryAdd(thing, thing.stackCount);
						}
						if (Props_Mech.spawnEffecter != null)
						{
							Effecter effecter = new Effecter(Props_Mech.spawnEffecter);
							effecter.Trigger(Props_Mech.attachSpawnedEffecter ? ((TargetInfo)spawnedPawn) : new TargetInfo(spawnedPawn.Position, spawnedPawn.Map), TargetInfo.Invalid);
							effecter.Cleanup();
						}
						spawnedPawn.Destroy();
					}
				}
				spawnedPawns.Clear();
				StartCooldown(RecoverTicks);
			},
			Disabled = !canRecover.Accepted,
			disabledReason = canRecover.Reason,
			icon = RecoverGizmoIcon,
			defaultLabel = "Ancot.MechCarrierRecover".Translate(SpawnPawnKind.labelPlural),
			defaultDesc = "Ancot.MechCarrierRecoverDesc".Translate(Props_Mech.maxPawnsToSpawn, SpawnPawnKind.labelPlural, SpawnPawnKind.label, CostPerPawn, base.Props.fixedIngredient.label, RecoverFactor.ToStringPercent())
		};
		if (!canRecover.Reason.NullOrEmpty())
		{
			act2.Disable(canRecover.Reason);
		}
		if (DebugSettings.ShowDevGizmos && cooldownTicksRemaining > 0)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Reset cooldown",
				action = delegate
				{
					cooldownTicksRemaining = 0;
				}
			};
		}
		yield return act;
		if (Props_Mech.recoverable)
		{
			yield return act2;
		}
	}

	public void StartCooldown(int ticks)
	{
		cooldownTicksRemaining = ticks;
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		innerContainer?.ClearAndDestroyContents();
		for (int i = 0; i < spawnedPawns.Count; i++)
		{
			CompCommandTerminal compCommandTerminal = spawnedPawns[i].TryGetComp<CompCommandTerminal>();
			if (compCommandTerminal != null)
			{
				compCommandTerminal.sortie_Terminal = true;
				compCommandTerminal.pivot = null;
			}
			if (Props_Mech.killSpawnedPawnIfParentDied && !spawnedPawns[i].Dead)
			{
				spawnedPawns[i].Kill(null, null);
			}
		}
	}

	public override void PostDrawExtraSelectionOverlays()
	{
		if (!Find.Selector.IsSelected(parent))
		{
			return;
		}
		for (int i = 0; i < spawnedPawns.Count; i++)
		{
			if (!spawnedPawns[i].Dead)
			{
				GenDraw.DrawLineBetween(parent.TrueCenter(), spawnedPawns[i].TrueCenter());
			}
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref cooldownTicksRemaining, "cooldownTicksRemaining", 0);
		Scribe_Collections.Look(ref spawnedPawns, "spawnedPawns", LookMode.Reference);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			spawnedPawns.RemoveAll((Pawn x) => x == null);
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		if (innerContainer != null)
		{
			innerContainer.DoTick();
		}
		if (cooldownTicksRemaining > 0)
		{
			cooldownTicksRemaining--;
		}
	}
}
