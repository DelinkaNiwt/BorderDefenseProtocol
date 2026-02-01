using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

[StaticConstructorOnStartup]
public class Alert_HighTemperature : Alert
{
	private static readonly Color WarningColor = new Color(0.9f, 0.1f, 0.1f);

	private static readonly Color CriticalColor = new Color(0.9f, 0.1f, 0.1f);

	protected override Color BGColor
	{
		get
		{
			if (GetCriticalDevices().Count > 0)
			{
				return CriticalColor;
			}
			if (GetWarningDevices().Count > 0)
			{
				return WarningColor;
			}
			return Color.clear;
		}
	}

	public Alert_HighTemperature()
	{
		defaultPriority = AlertPriority.High;
	}

	private List<Thing> GetCriticalDevices()
	{
		List<Thing> result = new List<Thing>();
		foreach (Map map in Find.Maps)
		{
			foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
			{
				CompTemperatureMonitor comp = thing.TryGetComp<CompTemperatureMonitor>();
				if (comp != null && !comp.AlreadyDestroyed && thing.Spawned)
				{
					Room room = thing.GetRoom();
					if (room != null && room.Temperature >= comp.Props.criticalTemperature)
					{
						result.Add(thing);
					}
				}
			}
		}
		return result;
	}

	private List<Thing> GetWarningDevices()
	{
		List<Thing> result = new List<Thing>();
		foreach (Map map in Find.Maps)
		{
			foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
			{
				CompTemperatureMonitor comp = thing.TryGetComp<CompTemperatureMonitor>();
				if (comp != null && !comp.AlreadyDestroyed && thing.Spawned)
				{
					Room room = thing.GetRoom();
					if (room != null && room.Temperature >= comp.Props.warningTemperature && room.Temperature < comp.Props.criticalTemperature)
					{
						result.Add(thing);
					}
				}
			}
		}
		return result;
	}

	public override string GetLabel()
	{
		List<Thing> criticalDevices = GetCriticalDevices();
		List<Thing> warningDevices = GetWarningDevices();
		if (criticalDevices.Count > 0)
		{
			return "NCL.ALERT_CRITICAL_TEMP".Translate();
		}
		if (warningDevices.Count > 0)
		{
			return "NCL.ALERT_WARNING_TEMP".Translate();
		}
		return "";
	}

	public override TaggedString GetExplanation()
	{
		List<Thing> criticalDevices = GetCriticalDevices();
		List<Thing> warningDevices = GetWarningDevices();
		if (criticalDevices.Count > 0)
		{
			return "NCL.ALERT_CRITICAL_EXPLANATION".Translate(string.Join("\n", criticalDevices.Select((Thing d) => d.LabelCap)));
		}
		if (warningDevices.Count > 0)
		{
			return "NCL.ALERT_WARNING_EXPLANATION".Translate(string.Join("\n", warningDevices.Select((Thing d) => d.LabelCap)));
		}
		return "";
	}

	public override AlertReport GetReport()
	{
		List<Thing> culprits = GetCriticalDevices().Concat(GetWarningDevices()).ToList();
		return (culprits.Count > 0) ? AlertReport.CulpritsAre(culprits) : AlertReport.Inactive;
	}
}
