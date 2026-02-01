using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class CompTemperatureMonitor : ThingComp
{
	private enum AlertState
	{
		Normal,
		Warning,
		Critical
	}

	private AlertState currentState = AlertState.Normal;

	private int ticksSinceLastCheck = 0;

	public CompProperties_TemperatureMonitor Props => (CompProperties_TemperatureMonitor)props;

	public bool AlreadyDestroyed { get; private set; } = false;

	public override void CompTick()
	{
		base.CompTick();
		if (!AlreadyDestroyed)
		{
			ticksSinceLastCheck++;
			if (ticksSinceLastCheck >= Props.checkInterval)
			{
				ticksSinceLastCheck = 0;
				CheckTemperature();
			}
		}
	}

	private void CheckTemperature()
	{
		if (!parent.Spawned || parent.Map == null)
		{
			return;
		}
		Room room = parent.GetRoom();
		if (room != null)
		{
			float temperature = room.Temperature;
			AlertState newState = ((temperature >= Props.criticalTemperature) ? AlertState.Critical : ((temperature >= Props.warningTemperature) ? AlertState.Warning : AlertState.Normal));
			if (newState != currentState)
			{
				currentState = newState;
			}
			if (currentState == AlertState.Critical)
			{
				DestroyStructure();
			}
		}
	}

	private void DestroyStructure()
	{
		Messages.Message("NCL.STRUCTURE_DESTROYED_HEAT".Translate(parent.LabelCap), parent, MessageTypeDefOf.NegativeEvent);
		parent.Destroy(DestroyMode.KillFinalize);
		AlreadyDestroyed = true;
	}

	public override string CompInspectStringExtra()
	{
		if (!parent.Spawned || parent.Map == null || AlreadyDestroyed)
		{
			return null;
		}
		Room room = parent.GetRoom();
		if (room == null)
		{
			return null;
		}
		float temperature = room.Temperature;
		string status = "";
		if (temperature >= Props.criticalTemperature)
		{
			status = "NCL.CRITICAL_OVERHEAT".Translate();
		}
		else if (temperature >= Props.warningTemperature)
		{
			status = "NCL.HIGH_TEMP_WARNING".Translate();
		}
		StringBuilder sb = new StringBuilder();
		if (!status.NullOrEmpty())
		{
			sb.AppendLine(status);
		}
		sb.Append("NCL.CURRENT_TEMP".Translate(temperature.ToString("F1")));
		return sb.ToString();
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (AlreadyDestroyed)
		{
			yield break;
		}
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (currentState >= AlertState.Warning)
		{
			yield return new Command_Action
			{
				icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt"),
				defaultLabel = "NCL.EMERGENCY_SHUTDOWN".Translate(),
				defaultDesc = "NCL.EMERGENCY_SHUTDOWN_DESC".Translate(),
				action = delegate
				{
					TryShutdownPowerComponents();
					Messages.Message("NCL.STRUCTURE_SHUTDOWN".Translate(parent.LabelCap), parent, MessageTypeDefOf.CautionInput);
				}
			};
		}
	}

	private void TryShutdownPowerComponents()
	{
		CompPowerAdjustable powerAdjustable = parent.GetComp<CompPowerAdjustable>();
		if (powerAdjustable != null)
		{
			powerAdjustable.powerPercent = 0f;
			powerAdjustable.UpdateDesiredPowerOutput();
		}
		CompPowerTrader powerTrader = parent.GetComp<CompPowerTrader>();
		if (powerTrader != null)
		{
			powerTrader.PowerOn = false;
		}
		CompFlickable flickable = parent.GetComp<CompFlickable>();
		if (flickable != null)
		{
			flickable.SwitchIsOn = false;
		}
	}
}
