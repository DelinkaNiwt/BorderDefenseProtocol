using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace AncotLibrary;

public class CompApparelReloadable_DeployPawn : CompApparelReloadable
{
	public List<Pawn> spawnedPawns = new List<Pawn>();

	private int ai_DeployCooldown = 0;

	private int deployCooldownRemain = 0;

	private Texture2D GizmoIcon1;

	private Texture2D GizmoIcon2;

	public bool sortie = true;

	public CompProperties_ApparelReloadable_DeployPawn Props_Deploy => (CompProperties_ApparelReloadable_DeployPawn)props;

	public int MaxCanSpawn => Mathf.Min(Mathf.FloorToInt(remainingCharges / Props_Deploy.costPerPawn), Props_Deploy.maxPawnsToSpawn);

	public string gizmoLabel1 => Props_Deploy.gizmoLabel1.NullOrEmpty() ? ((string)"Ancot.FloatUnit_Follow".Translate()) : Props_Deploy.gizmoLabel1;

	public string gizmoDesc1 => Props_Deploy.gizmoDesc1.NullOrEmpty() ? ((string)"Ancot.FloatUnit_FollowDesc".Translate(PawnOwner.Name.ToStringShort)) : Props_Deploy.gizmoDesc1;

	public string gizmoLabel2 => Props_Deploy.gizmoLabel2.NullOrEmpty() ? ((string)"Ancot.FloatUnitSortie".Translate()) : Props_Deploy.gizmoLabel2;

	public string gizmoDesc2 => Props_Deploy.gizmoDesc2.NullOrEmpty() ? ((string)"Ancot.FloatUnitSortieDesc".Translate(PawnOwner.Name.ToStringShort)) : Props_Deploy.gizmoDesc2;

	public PawnKindDef SpawnPawnKind => Props_Deploy.spawnPawnKind;

	public Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				return null;
			}
			return wearer;
		}
	}

	public bool aiCanDeployNow => deployCooldownRemain == 0 && ai_DeployCooldown == 0;

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
		sortie = Props_Deploy.sortie;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref ai_DeployCooldown, "ai_DeployCooldown", 0);
		Scribe_Values.Look(ref deployCooldownRemain, "deployCooldownRemain", 0);
	}

	public override void CompTick()
	{
		base.CompTick();
		if (ai_DeployCooldown > 0)
		{
			ai_DeployCooldown--;
		}
		if (deployCooldownRemain > 0)
		{
			deployCooldownRemain--;
		}
	}

	public override bool CanBeUsed(out string reason)
	{
		reason = "";
		if (deployCooldownRemain > 0)
		{
			reason = "CommandReload_Cooldown".Translate(base.Props.CooldownVerbArgument, deployCooldownRemain.ToStringTicksToPeriod().Named("TIME"));
			return false;
		}
		if (!base.CanBeUsed(out reason))
		{
			return false;
		}
		return true;
	}

	public override void UsedOnce()
	{
		if (remainingCharges >= MaxCanSpawn * Props_Deploy.costPerPawn)
		{
			remainingCharges -= MaxCanSpawn * Props_Deploy.costPerPawn;
		}
		if (base.Props.destroyOnEmpty && remainingCharges == 0 && !parent.Destroyed)
		{
			parent.Destroy();
		}
	}

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetWornGizmosExtra())
		{
			yield return item;
		}
		if (!PawnOwner.Faction.IsPlayer || !Props_Deploy.showSortieSwitchGizmo)
		{
			yield break;
		}
		if (sortie)
		{
			if ((object)GizmoIcon2 == null)
			{
				GizmoIcon2 = ContentFinder<Texture2D>.Get(Props_Deploy.gizmoIconPath2);
			}
		}
		else if ((object)GizmoIcon1 == null)
		{
			GizmoIcon1 = ContentFinder<Texture2D>.Get(Props_Deploy.gizmoIconPath1);
		}
		yield return new Command_Action
		{
			Order = Props_Deploy.gizmoOrder,
			defaultLabel = (sortie ? gizmoLabel2 : gizmoLabel1),
			defaultDesc = (sortie ? gizmoDesc2 : gizmoDesc1),
			icon = (sortie ? GizmoIcon2 : GizmoIcon1),
			action = delegate
			{
				sortie = !sortie;
				for (int i = 0; i < spawnedPawns.Count; i++)
				{
					CompCommandTerminal compCommandTerminal = spawnedPawns[i].TryGetComp<CompCommandTerminal>();
					if (compCommandTerminal != null)
					{
						compCommandTerminal.sortie_Terminal = sortie;
					}
				}
			}
		};
	}

	public override void Notify_WearerDied()
	{
		for (int i = 0; i < spawnedPawns.Count; i++)
		{
			CompCommandTerminal compCommandTerminal = spawnedPawns[i].TryGetComp<CompCommandTerminal>();
			if (compCommandTerminal != null)
			{
				compCommandTerminal.sortie_Terminal = true;
			}
		}
	}

	public void Deploy()
	{
		int maxCanSpawn = MaxCanSpawn;
		if (maxCanSpawn <= 0)
		{
			return;
		}
		PawnGenerationRequest request = new PawnGenerationRequest(SpawnPawnKind, parent.Faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Newborn);
		Lord lord = PawnOwner.GetLord();
		for (int i = 0; i < maxCanSpawn; i++)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(request);
			GenSpawn.Spawn(pawn, PawnOwner.Position, PawnOwner.Map);
			pawn.SetFaction(PawnOwner.Faction);
			if (Props_Deploy.hediffAddToSpawnPawn != null)
			{
				pawn.health.AddHediff(Props_Deploy.hediffAddToSpawnPawn);
			}
			CompCommandTerminal compCommandTerminal = pawn.TryGetComp<CompCommandTerminal>();
			if (compCommandTerminal != null)
			{
				compCommandTerminal.sortie_Terminal = sortie;
				compCommandTerminal.pivot = PawnOwner;
			}
			spawnedPawns.Add(pawn);
			lord?.AddPawn(pawn);
			if (Props_Deploy.spawnedMechEffecter != null)
			{
				Effecter effecter = new Effecter(Props_Deploy.spawnedMechEffecter);
				effecter.Trigger(Props_Deploy.attachSpawnedMechEffecter ? ((TargetInfo)pawn) : new TargetInfo(pawn.Position, pawn.Map), TargetInfo.Invalid);
				effecter.Cleanup();
			}
		}
		deployCooldownRemain = Props_Deploy.cooldownTicks;
		if (Props_Deploy.spawnEffecter != null)
		{
			Effecter effecter2 = new Effecter(Props_Deploy.spawnEffecter);
			effecter2.Trigger(Props_Deploy.attachSpawnedEffecter ? ((TargetInfo)PawnOwner) : new TargetInfo(PawnOwner.Position, PawnOwner.Map), TargetInfo.Invalid);
			effecter2.Cleanup();
		}
	}
}
