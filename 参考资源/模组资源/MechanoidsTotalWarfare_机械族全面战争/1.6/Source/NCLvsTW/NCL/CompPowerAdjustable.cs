using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class CompPowerAdjustable : CompPowerTrader
{
	public float powerPercent = 0f;

	private CompRefuelable refuelableComp;

	public float basePowerOutput = 0f;

	public float maxPowerOutput = 140000f;

	public float baseFuelConsumption = 0.5f;

	public float maxFuelConsumption = 100f;

	public float minHeatOutput = 0f;

	public float maxHeatOutput = 100f;

	private float currentHeatOutput = 0f;

	private const int HeatPushInterval = 60;

	private int ticksSinceLastHeatPush = 0;

	private bool shouldPushHeat = false;

	private float lastFuelPercent = -1f;

	private int ticksSinceLastCheck = 0;

	private const int CheckInterval = 250;

	private float currentSmoothedOutput = 0f;

	private const float SmoothingFactor = 0.1f;

	private float FuelPowerMultiplier
	{
		get
		{
			if (powerPercent <= 0f)
			{
				return 0f;
			}
			if (refuelableComp == null || !refuelableComp.HasFuel)
			{
				return 0f;
			}
			float fuelPercent = refuelableComp.FuelPercentOfMax;
			if (fuelPercent >= 0.9f)
			{
				return 2f;
			}
			return fuelPercent / 0.9f * 2f;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref powerPercent, "powerPercent", 0f);
		Scribe_Values.Look(ref basePowerOutput, "basePowerOutput", 0f);
		Scribe_Values.Look(ref maxPowerOutput, "maxPowerOutput", 140000f);
		Scribe_Values.Look(ref baseFuelConsumption, "baseFuelConsumption", 0.5f);
		Scribe_Values.Look(ref maxFuelConsumption, "maxFuelConsumption", 50f);
		Scribe_Values.Look(ref lastFuelPercent, "lastFuelPercent", -1f);
		Scribe_Values.Look(ref currentSmoothedOutput, "currentSmoothedOutput", 0f);
		Scribe_Values.Look(ref minHeatOutput, "minHeatOutput", 0f);
		Scribe_Values.Look(ref maxHeatOutput, "maxHeatOutput", 500f);
		Scribe_Values.Look(ref currentHeatOutput, "currentHeatOutput", 0f);
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		refuelableComp = parent.GetComp<CompRefuelable>();
		UpdateDesiredPowerOutput();
		UpdateHeatOutput();
		if (refuelableComp != null)
		{
			lastFuelPercent = refuelableComp.FuelPercentOfMax;
		}
		currentSmoothedOutput = base.PowerOutput;
	}

	public void UpdateDesiredPowerOutput()
	{
		base.PowerOn = powerPercent > 0f && (refuelableComp?.HasFuel ?? false);
		if (refuelableComp != null && refuelableComp.HasFuel)
		{
			refuelableComp.Props.fuelConsumptionRate = ((powerPercent > 0f) ? Mathf.Lerp(baseFuelConsumption, maxFuelConsumption, powerPercent / 100f) : 0f);
		}
	}

	private void UpdateHeatOutput()
	{
		if (powerPercent <= 0f)
		{
			currentHeatOutput = 0f;
			shouldPushHeat = false;
		}
		else
		{
			currentHeatOutput = Mathf.Lerp(minHeatOutput, maxHeatOutput, powerPercent / 100f);
			currentHeatOutput *= FuelPowerMultiplier;
			shouldPushHeat = currentHeatOutput > 0f && parent.Spawned && parent.Map != null && (refuelableComp?.HasFuel ?? true);
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		ticksSinceLastCheck++;
		ticksSinceLastHeatPush++;
		bool needsUpdate = false;
		float targetOutput;
		if (powerPercent <= 0f)
		{
			targetOutput = 0f;
		}
		else
		{
			float baseOutput = Mathf.Lerp(basePowerOutput, maxPowerOutput, powerPercent / 100f);
			targetOutput = baseOutput * FuelPowerMultiplier;
		}
		currentSmoothedOutput = Mathf.Lerp(currentSmoothedOutput, targetOutput, 0.1f);
		base.PowerOutput = currentSmoothedOutput;
		UpdateHeatOutput();
		if (ticksSinceLastHeatPush >= 60 && shouldPushHeat)
		{
			ticksSinceLastHeatPush = 0;
			GenTemperature.PushHeat(parent.Position, parent.Map, currentHeatOutput);
		}
		if (powerPercent <= 0f)
		{
			return;
		}
		if (ticksSinceLastCheck >= 250)
		{
			ticksSinceLastCheck = 0;
			if (refuelableComp != null)
			{
				float currentFuelPercent = refuelableComp.FuelPercentOfMax;
				if (Mathf.Abs(currentFuelPercent - lastFuelPercent) > 0.01f || refuelableComp.HasFuel != lastFuelPercent > 0f)
				{
					needsUpdate = true;
				}
				lastFuelPercent = currentFuelPercent;
			}
		}
		if (needsUpdate)
		{
			UpdateDesiredPowerOutput();
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		yield return new Command_Action
		{
			icon = ContentFinder<Texture2D>.Get("ModIcon/SetPowerLevel"),
			defaultLabel = "NCL.ADJUST_POWER_OUTPUT".Translate(),
			defaultDesc = "NCL.CURRENT_POWER_LEVEL".Translate(powerPercent) + "\n" + GetFuelMultiplierInfo() + "\n" + GetHeatInfo(),
			action = delegate
			{
				Find.WindowStack.Add(new Dialog_Slider((int value) => "NCL.SET_POWER_OUTPUT".Translate(value, Mathf.Lerp(minHeatOutput, maxHeatOutput, (float)value / 100f).ToString("F1")), 0, 100, delegate(int value)
				{
					powerPercent = value;
					UpdateDesiredPowerOutput();
					UpdateHeatOutput();
				}, (int)powerPercent));
			}
		};
	}

	private string GetFuelMultiplierInfo()
	{
		if (powerPercent <= 0f)
		{
			return "NCL.POWER_DISABLED".Translate();
		}
		if (refuelableComp == null)
		{
			return "NCL.NO_FUEL_SYSTEM".Translate();
		}
		if (!refuelableComp.HasFuel)
		{
			return "NCL.NO_FUEL".Translate();
		}
		float fuelPercent = refuelableComp.FuelPercentOfMax;
		float multiplier = FuelPowerMultiplier;
		string powerEffect = ((fuelPercent >= 0.9f) ? ((string)"NCL.PEAK_POWER".Translate()) : "");
		return "NCL.FUEL_STATUS".Translate(fuelPercent * 100f, multiplier, powerEffect);
	}

	private string GetHeatInfo()
	{
		return (powerPercent > 0f) ? "NCL.HEAT_OUTPUT".Translate(currentHeatOutput.ToString("F1")) : "NCL.NO_HEAT_OUTPUT".Translate();
	}

	public override string CompInspectStringExtra()
	{
		string baseStr = base.CompInspectStringExtra();
		if (baseStr == null)
		{
			baseStr = "";
		}
		string fuelStr = ((refuelableComp != null) ? ((string)"NCL.FUEL_CONSUMPTION".Translate(refuelableComp.Props.fuelConsumptionRate.ToString("F2"))) : "");
		string multiplierInfo = "NCL.NO_FUEL".Translate();
		if (powerPercent <= 0f)
		{
			multiplierInfo = "NCL.POWER_DISABLED".Translate();
		}
		else if (refuelableComp != null && refuelableComp.HasFuel)
		{
			float fuelPercent = refuelableComp.FuelPercentOfMax;
			multiplierInfo = "NCL.FUEL_MULTIPLIER".Translate(FuelPowerMultiplier.ToString("F1"));
			multiplierInfo = ((!(fuelPercent >= 0.9f)) ? ((string)(multiplierInfo + ("\n" + "NCL.NEED_MORE_FUEL".Translate(((0.9f - fuelPercent) * 100f).ToString("F0"))))) : ((string)(multiplierInfo + "NCL.PEAK".Translate())));
		}
		string heatStr = "NCL.HEAT_OUTPUT".Translate(currentHeatOutput.ToString("F1"));
		List<string> parts = new List<string>();
		if (!baseStr.NullOrEmpty())
		{
			parts.Add(baseStr);
		}
		if (!fuelStr.NullOrEmpty())
		{
			parts.Add(fuelStr);
		}
		if (!multiplierInfo.NullOrEmpty())
		{
			parts.Add(multiplierInfo);
		}
		if (!heatStr.NullOrEmpty())
		{
			parts.Add(heatStr);
		}
		return string.Join("\n", parts.Where((string s) => !s.NullOrEmpty()));
	}
}
