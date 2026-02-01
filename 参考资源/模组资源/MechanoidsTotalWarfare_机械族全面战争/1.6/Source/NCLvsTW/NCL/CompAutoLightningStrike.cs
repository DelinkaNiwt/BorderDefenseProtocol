using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class CompAutoLightningStrike : ThingComp
{
	private CompPowerTrader powerComp;

	private int lastStrikeTick = -999999;

	private bool autoStrikeEnabled = false;

	private bool overdriveEnabled = false;

	private List<int> activeStrikeTicks = new List<int>();

	public CompProperties_AutoLightningStrike Props => (CompProperties_AutoLightningStrike)props;

	private int CurrentMaxTargets => overdriveEnabled ? (Props.maxTargets * Props.overdriveTargetMultiplier) : Props.maxTargets;

	private int CurrentMaxConcurrentStrikes => overdriveEnabled ? (Props.maxConcurrentStrikes * Props.overdriveTargetMultiplier) : Props.maxConcurrentStrikes;

	private int CurrentStrikeInterval => overdriveEnabled ? ((int)((float)Props.autoStrikeInterval / Props.overdriveIntervalDivider)) : Props.autoStrikeInterval;

	private float CurrentPowerCost => overdriveEnabled ? (Props.autoStrikePowerCost * Props.overdrivePowerMultiplier) : Props.autoStrikePowerCost;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		powerComp = parent.TryGetComp<CompPowerTrader>();
	}

	public override void CompTick()
	{
		base.CompTick();
		activeStrikeTicks.RemoveAll((int tick) => Find.TickManager.TicksGame > tick + 10);
		if (autoStrikeEnabled && Find.TickManager.TicksGame % CurrentStrikeInterval == 0 && CanAutoStrike() && activeStrikeTicks.Count < CurrentMaxConcurrentStrikes)
		{
			TryAutoStrike();
		}
	}

	private bool HasEnoughPower(float amount)
	{
		if (powerComp == null || powerComp.PowerNet == null)
		{
			return false;
		}
		float totalAvailable = (Props.consumeFromBatteriesOnly ? GetBatteriesStoredEnergy(powerComp.PowerNet) : GetTotalAvailableEnergy(powerComp.PowerNet));
		return totalAvailable >= amount;
	}

	private bool CanAutoStrike()
	{
		return powerComp != null && powerComp.PowerOn && powerComp.PowerNet != null && Find.TickManager.TicksGame >= lastStrikeTick + CurrentStrikeInterval && HasEnoughPower(CurrentPowerCost);
	}

	private List<IntVec3> FindTargetCells()
	{
		return (from t in (from p in parent.Map.mapPawns.AllPawnsSpawned
				where ShouldTargetPawn(p)
				orderby p.Position.DistanceTo(parent.Position)
				select p).Take(CurrentMaxTargets)
			select t.Position).ToList();
	}

	private bool ShouldTargetPawn(Pawn p)
	{
		if (p == null || p.Dead || p.Downed || p.Destroyed)
		{
			return false;
		}
		if (p.Map.roofGrid.Roofed(p.Position))
		{
			return false;
		}
		Faction faction = p.Faction;
		if (faction == null || !faction.HostileTo(parent.Faction))
		{
			return false;
		}
		if (IsPrisoner(p))
		{
			bool isInPrisonArea = p.Map.areaManager.Home[p.Position];
			if (!isInPrisonArea)
			{
				Region region = p.Map.regionGrid.GetValidRegionAt(p.Position);
				if (region != null)
				{
					District district = region.District;
					if (district != null)
					{
						isInPrisonArea = district.Cells.Any((IntVec3 c) => p.Map.areaManager.Home[c]);
					}
				}
			}
			return !isInPrisonArea;
		}
		return true;
	}

	private bool IsPrisoner(Pawn p)
	{
		return p.IsPrisoner || (p.guest != null && p.guest.IsPrisoner) || p.HostFaction == Faction.OfPlayer;
	}

	private void TryAutoStrike()
	{
		foreach (IntVec3 cell in from c in FindTargetCells()
			where c.IsValid
			select c)
		{
			ConsumePower(CurrentPowerCost);
			DoLightningStrike(cell);
			activeStrikeTicks.Add(Find.TickManager.TicksGame);
		}
	}

	private void DoLightningStrike(IntVec3 targetCell)
	{
		if (!parent.Map.roofGrid.Roofed(targetCell))
		{
			lastStrikeTick = Find.TickManager.TicksGame;
			if (parent.Map.weatherManager != null)
			{
				parent.Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(parent.Map, targetCell));
			}
			else
			{
				FleckMaker.ThrowLightningGlow(targetCell.ToVector3(), parent.Map, 3f);
			}
			GenExplosion.DoExplosion(targetCell, parent.Map, Props.empRadius, Props.damageType, parent, Props.damageAmount, 0f);
		}
	}

	private void ConsumePower(float amount)
	{
		if (Props.consumeFromBatteriesOnly)
		{
			foreach (CompPowerBattery battery in powerComp.PowerNet.batteryComps)
			{
				if (amount <= 0f)
				{
					break;
				}
				if (battery.StoredEnergy > 0f)
				{
					float consume = Mathf.Min(amount, battery.StoredEnergy);
					battery.DrawPower(consume);
					amount -= consume;
				}
			}
			return;
		}
		float remaining = amount;
		foreach (CompPowerBattery battery2 in powerComp.PowerNet.batteryComps)
		{
			if (remaining <= 0f)
			{
				break;
			}
			if (battery2.StoredEnergy > 0f)
			{
				float consume2 = Mathf.Min(remaining, battery2.StoredEnergy);
				battery2.DrawPower(consume2);
				remaining -= consume2;
			}
		}
		if (remaining > 0f)
		{
			powerComp.PowerOutput -= remaining;
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		yield return CreateAutoStrikeGizmo();
		if (Props.enableOverdrive)
		{
			yield return CreateOverdriveGizmo();
		}
	}

	private Command_Toggle CreateAutoStrikeGizmo()
	{
		Command_Toggle cmd = new Command_Toggle
		{
			icon = ContentFinder<Texture2D>.Get(Props.uiIconPath),
			defaultLabel = Props.uiLabel.Translate(),
			defaultDesc = Props.uiDescription.Translate(),
			isActive = () => autoStrikeEnabled,
			toggleAction = delegate
			{
				autoStrikeEnabled = !autoStrikeEnabled;
			}
		};
		if (!CanToggleAutoStrike(out var reason))
		{
			cmd.Disable(reason);
		}
		return cmd;
	}

	private Command_Toggle CreateOverdriveGizmo()
	{
		Command_Toggle cmd = new Command_Toggle
		{
			icon = ContentFinder<Texture2D>.Get(Props.overdriveIconPath),
			defaultLabel = Props.overdriveLabel.Translate(),
			defaultDesc = Props.overdriveDescription.Translate(),
			isActive = () => overdriveEnabled,
			toggleAction = ToggleOverdrive
		};
		if (!CanToggleOverdrive(out var reason))
		{
			cmd.Disable(reason);
		}
		return cmd;
	}

	private void ToggleOverdrive()
	{
		overdriveEnabled = !overdriveEnabled;
		Messages.Message(overdriveEnabled ? "NCL_OverdriveMode_Enabled".Translate(parent.LabelCap) : "NCL_OverdriveMode_Disabled".Translate(parent.LabelCap), MessageTypeDefOf.NeutralEvent);
	}

	private bool CanToggleAutoStrike(out string reason)
	{
		if (powerComp == null || !powerComp.PowerOn)
		{
			reason = "NCL_AutoLightningStrike_NoPower".Translate();
			return false;
		}
		if (!HasEnoughPower(CurrentPowerCost))
		{
			reason = "NCL_AutoLightningStrike_NeedPower".Translate(CurrentPowerCost.ToString("F0"));
			return false;
		}
		reason = null;
		return true;
	}

	private bool CanToggleOverdrive(out string reason)
	{
		if (!autoStrikeEnabled)
		{
			reason = "NCL_OverdriveMode_RequiresAuto".Translate();
			return false;
		}
		return CanToggleAutoStrike(out reason);
	}

	public override string CompInspectStringExtra()
	{
		StringBuilder sb = new StringBuilder();
		if (powerComp == null || !powerComp.PowerOn)
		{
			return "NCL_AutoLightningStrike_NoPower".Translate();
		}
		sb.Append("NCL_AutoLightningStrike_PowerStatus".Translate(GetAvailablePower().ToString("F0"), CurrentPowerCost.ToString("F0")));
		sb.AppendLine();
		if (autoStrikeEnabled)
		{
			sb.Append("NCL_AutoLightningStrike_AutoActive".Translate());
			sb.AppendLine();
			sb.Append("NCL_AutoLightningStrike_ConcurrentStrikes".Translate(activeStrikeTicks.Count, CurrentMaxConcurrentStrikes));
			sb.AppendLine();
		}
		if (overdriveEnabled)
		{
			sb.Append("NCL_OverdriveMode_Active".Translate());
			sb.AppendLine();
		}
		int ticksRemaining = Mathf.Max(0, lastStrikeTick + CurrentStrikeInterval - Find.TickManager.TicksGame);
		sb.Append((ticksRemaining > 0) ? "NCL_AutoLightningStrike_NextStrike".Translate(ticksRemaining.ToStringTicksToPeriod()) : "NCL_AutoLightningStrike_Ready".Translate());
		return sb.ToString();
	}

	private float GetAvailablePower()
	{
		return Props.consumeFromBatteriesOnly ? GetBatteriesStoredEnergy(powerComp.PowerNet) : GetTotalAvailableEnergy(powerComp.PowerNet);
	}

	private float GetBatteriesStoredEnergy(PowerNet net)
	{
		return net?.batteryComps.Sum((CompPowerBattery b) => b.StoredEnergy) ?? 0f;
	}

	private float GetTotalAvailableEnergy(PowerNet net)
	{
		return GetBatteriesStoredEnergy(net) + ((net != null) ? (net.CurrentEnergyGainRate() * 60000f) : 0f);
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref autoStrikeEnabled, "autoStrikeEnabled", defaultValue: false);
		Scribe_Values.Look(ref overdriveEnabled, "overdriveEnabled", defaultValue: false);
		Scribe_Values.Look(ref lastStrikeTick, "lastStrikeTick", -999999);
		Scribe_Collections.Look(ref activeStrikeTicks, "activeStrikeTicks", LookMode.Value);
	}
}
