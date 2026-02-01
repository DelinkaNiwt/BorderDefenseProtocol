using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class CompLightningStrike : ThingComp
{
	private CompPowerTrader powerComp;

	private int lastStrikeTick = -999999;

	private bool isTargeting = false;

	private IntVec3 currentTargetCell;

	public CompProperties_LightningStrike Props => (CompProperties_LightningStrike)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		powerComp = parent.TryGetComp<CompPowerTrader>();
	}

	public override void CompTick()
	{
		base.CompTick();
		if (isTargeting)
		{
			currentTargetCell = UI.MouseCell();
			DrawTargetingEffects(currentTargetCell);
			if (!Find.Targeter.IsTargeting)
			{
				isTargeting = false;
			}
		}
	}

	private void DrawTargetingEffects(IntVec3 cell)
	{
		GenDraw.DrawRadiusRing(cell, Props.empRadius, Color.white);
		GenDraw.DrawRadiusRing(cell, 1.5f, Color.white);
	}

	private bool HasEnoughPower()
	{
		float totalAvailable = (Props.consumeFromBatteriesOnly ? GetBatteriesStoredEnergy(powerComp.PowerNet) : GetTotalAvailableEnergy(powerComp.PowerNet));
		return totalAvailable >= Props.requiredPower;
	}

	private float GetBatteriesStoredEnergy(PowerNet net)
	{
		float total = 0f;
		foreach (CompPowerBattery battery in net.batteryComps)
		{
			total += battery.StoredEnergy;
		}
		return total;
	}

	private float GetTotalAvailableEnergy(PowerNet net)
	{
		return GetBatteriesStoredEnergy(net) + net.CurrentEnergyGainRate() * 60000f;
	}

	private void DoLightningStrike(IntVec3 targetCell)
	{
		ConsumePower(Props.requiredPower);
		lastStrikeTick = Find.TickManager.TicksGame;
		if (parent.Map.weatherManager != null)
		{
			parent.Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(parent.Map, targetCell));
		}
		else
		{
			FleckMaker.ThrowLightningGlow(targetCell.ToVector3(), parent.Map, 3f);
		}
		GenExplosion.DoExplosion(targetCell, parent.Map, Props.empRadius, DamageDefOf.EMP, parent, 50, 0f);
		Messages.Message("LightningStrike_Message".Translate(parent.LabelCap), MessageTypeDefOf.NeutralEvent);
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
		Command_Action lightningStrike = new Command_Action
		{
			icon = ContentFinder<Texture2D>.Get("ModIcon/EMPBurst"),
			defaultLabel = "LightningStrike".Translate(),
			defaultDesc = "LightningStrikeDesc".Translate(),
			action = delegate
			{
				BeginTargeting();
			}
		};
		if (!CanActivate(out var reason))
		{
			lightningStrike.Disable(reason);
		}
		yield return lightningStrike;
	}

	private bool CanActivate(out string reason)
	{
		if (powerComp == null || !powerComp.PowerOn)
		{
			reason = "LightningStrike_NoPower".Translate();
			return false;
		}
		if (Find.TickManager.TicksGame < lastStrikeTick + Props.cooldownTicks)
		{
			reason = "LightningStrike_Cooldown".Translate((lastStrikeTick + Props.cooldownTicks - Find.TickManager.TicksGame).ToStringTicksToPeriod());
			return false;
		}
		if (!HasEnoughPower())
		{
			reason = "LightningStrike_NeedPower".Translate(Props.requiredPower.ToString("F0"));
			return false;
		}
		reason = null;
		return true;
	}

	private void BeginTargeting()
	{
		isTargeting = true;
		Find.Targeter.BeginTargeting(new TargetingParameters
		{
			canTargetLocations = true,
			canTargetPawns = true,
			canTargetBuildings = true
		}, delegate(LocalTargetInfo t)
		{
			GenDraw.DrawRadiusRing(t.Cell, Props.empRadius, Color.red);
			DoLightningStrike(t.Cell);
			isTargeting = false;
		});
	}

	public override string CompInspectStringExtra()
	{
		StringBuilder sb = new StringBuilder();
		if (powerComp == null || !powerComp.PowerOn)
		{
			return "LightningStrike_NoPower".Translate();
		}
		sb.Append("LightningStrike_PowerStatus".Translate(GetAvailablePower().ToString("F0"), Props.requiredPower.ToString("F0")));
		sb.AppendLine();
		int ticksRemaining = Mathf.Max(0, lastStrikeTick + Props.cooldownTicks - Find.TickManager.TicksGame);
		sb.Append((ticksRemaining > 0) ? "LightningStrike_Cooldown".Translate(ticksRemaining.ToStringTicksToPeriod()) : "LightningStrike_Ready".Translate());
		return sb.ToString();
	}

	private float GetAvailablePower()
	{
		return Props.consumeFromBatteriesOnly ? GetBatteriesStoredEnergy(powerComp.PowerNet) : GetTotalAvailableEnergy(powerComp.PowerNet);
	}
}
