using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCL;

public class CompUpgradableBandNode : CompBandNode
{
	private int generations = 0;

	private int cooldownTicks = 0;

	private int prevGenerations = -1;

	private CompPowerTrader powerComp;

	private float originalBasePowerConsumption;

	public new CompProperties_UpgradableBandNode Props => (CompProperties_UpgradableBandNode)props;

	public int GenerationCount => generations;

	public bool CanGenerate => generations < Props.maxGenerations && cooldownTicks <= 0;

	public float TotalExtraPowerConsumption => (float)generations * Props.extraPowerConsumptionPerGeneration;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref generations, "generations", 0);
		Scribe_Values.Look(ref cooldownTicks, "cooldownTicks", 0);
		Scribe_Values.Look(ref prevGenerations, "prevGenerations", -1);
		Scribe_Values.Look(ref originalBasePowerConsumption, "originalBasePowerConsumption", 0f);
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		powerComp = parent.GetComp<CompPowerTrader>();
		if (powerComp == null)
		{
			Log.Error("[" + parent.Label + "] has no CompPowerTrader component");
			return;
		}
		originalBasePowerConsumption = powerComp.Props.PowerConsumption;
		if (!respawningAfterLoad)
		{
			prevGenerations = generations;
		}
		ApplyExtraPowerConsumption();
	}

	public override void CompTick()
	{
		SafeBaseCompTick();
		if (cooldownTicks > 0)
		{
			cooldownTicks--;
		}
		ApplyExtraPowerConsumption();
		if (prevGenerations != generations)
		{
			prevGenerations = generations;
			if (tunedTo != null)
			{
				GetMechNodeHediff(tunedTo)?.RecacheBandNodes();
			}
		}
	}

	private void SafeBaseCompTick()
	{
		if (powerComp == null)
		{
			return;
		}
		try
		{
			powerComp.PowerOutput = ((tunedTo == null && tuningTo == null) ? ((float)(-Props.powerConsumptionIdle)) : (0f - powerComp.Props.PowerConsumption));
			if (tunedTo != null && tunedTo.Dead)
			{
				tunedTo = null;
			}
			if (tuningTo != null && tuningTo.Dead)
			{
				tuningTo = null;
			}
			if (tuningTo != null)
			{
				tuningTimeLeft--;
				if (tuningTimeLeft <= 0)
				{
					tunedTo = tuningTo;
					tuningTo = null;
					if (Props.tuningCompleteSound != null)
					{
						Props.tuningCompleteSound.PlayOneShot(parent);
					}
				}
			}
			if (tuningTo == null && tunedTo != null && !tunedTo.health.hediffSet.HasHediff(Props.hediff))
			{
				tunedTo.health.AddHediff(Props.hediff, tunedTo.health.hediffSet.GetBrain());
			}
			if (powerComp != null && powerComp.PowerOn)
			{
			}
		}
		catch (NullReferenceException arg)
		{
			Log.Error($"SafeBaseCompTick error for {parent.Label}: {arg}");
		}
	}

	private void ApplyExtraPowerConsumption()
	{
		if (powerComp != null)
		{
			float baseConsumption = ((GetCurrentState() == BandNodeState.Untuned) ? ((float)Props.powerConsumptionIdle) : originalBasePowerConsumption);
			float totalConsumption = baseConsumption + TotalExtraPowerConsumption;
			powerComp.PowerOutput = 0f - totalConsumption;
		}
	}

	private BandNodeState GetCurrentState()
	{
		if (tunedTo != null && tuningTo != null)
		{
			return BandNodeState.Retuning;
		}
		if (tuningTo != null)
		{
			return BandNodeState.Tuning;
		}
		if (tunedTo != null)
		{
			return BandNodeState.Tuned;
		}
		return BandNodeState.Untuned;
	}

	public int GetBandwidthPoints()
	{
		return Props.baseBandwidth + Props.bandwidthPerGeneration * generations;
	}

	private Hediff_MechNode GetMechNodeHediff(Pawn mechanitor)
	{
		if (mechanitor == null || mechanitor.health == null || mechanitor.health.hediffSet == null)
		{
			return null;
		}
		return mechanitor.health.hediffSet.GetFirstHediffOfDef(Props.hediff) as Hediff_MechNode;
	}

	public void GenerateBandwidth()
	{
		if (!CanGenerate)
		{
			return;
		}
		generations++;
		cooldownTicks = (int)(Props.generationCooldownDays * 60000f);
		if (Props.tuningCompleteSound != null)
		{
			Props.tuningCompleteSound.PlayOneShot(parent);
		}
		ApplyExtraPowerConsumption();
		if (tunedTo != null)
		{
			GetMechNodeHediff(tunedTo)?.RecacheBandNodes();
			if (tunedTo.mechanitor != null)
			{
				tunedTo.mechanitor.Notify_BandwidthChanged();
			}
		}
		Messages.Message("BandwidthGenerated".Translate(parent.Label, generations, Props.maxGenerations), parent, MessageTypeDefOf.PositiveEvent);
	}

	public void ResetGenerations()
	{
		if (generations == 0)
		{
			return;
		}
		generations = 0;
		cooldownTicks = 0;
		prevGenerations = -1;
		ApplyExtraPowerConsumption();
		if (tunedTo != null)
		{
			GetMechNodeHediff(tunedTo)?.RecacheBandNodes();
			if (tunedTo.mechanitor != null)
			{
				tunedTo.mechanitor.Notify_BandwidthChanged();
			}
		}
		Messages.Message("BandwidthReset".Translate(parent.Label), parent, MessageTypeDefOf.NeutralEvent);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (tunedTo != null)
		{
			yield return new Command_Action
			{
				icon = ContentFinder<Texture2D>.Get("ModIcon/GenerateBandwidth"),
				defaultLabel = "GenerateBandwidth".Translate(),
				defaultDesc = "GenerateBandwidthDesc".Translate(Props.bandwidthPerGeneration, Props.extraPowerConsumptionPerGeneration, generations, Props.maxGenerations),
				action = GenerateBandwidth,
				disabledReason = (CanGenerate ? ((TaggedString)null) : ((generations >= Props.maxGenerations) ? "MaxGenerationsReached".Translate() : "CooldownActive".Translate(cooldownTicks.ToStringTicksToPeriod())))
			};
			if (generations > 0)
			{
				yield return new Command_Action
				{
					icon = ContentFinder<Texture2D>.Get("ModIcon/ResetBandwidth"),
					defaultLabel = "ResetBandwidth".Translate(),
					defaultDesc = "ResetBandwidthDesc".Translate(),
					action = ResetGenerations
				};
			}
		}
	}

	public override string CompInspectStringExtra()
	{
		string baseStr = base.CompInspectStringExtra();
		string bandwidthInfo = string.Concat("BandwidthPoints".Translate() + ": ", GetBandwidthPoints().ToString(), " (", Props.baseBandwidth.ToString(), " + ", Props.bandwidthPerGeneration.ToString(), " × ", generations.ToString(), ")");
		string powerInfo = string.Concat("TotalExtraPower".Translate() + ": ", TotalExtraPowerConsumption.ToString(), "W");
		string generationInfo = string.Concat("Generations".Translate() + ": ", generations.ToString(), "/", Props.maxGenerations.ToString());
		string cooldownInfo = ((cooldownTicks > 0) ? ("Cooldown".Translate() + ": " + cooldownTicks.ToStringTicksToPeriod()) : "ReadyToGenerate".Translate());
		return baseStr + "\n" + bandwidthInfo + "\n" + powerInfo + "\n" + generationInfo + "\n" + cooldownInfo;
	}
}
