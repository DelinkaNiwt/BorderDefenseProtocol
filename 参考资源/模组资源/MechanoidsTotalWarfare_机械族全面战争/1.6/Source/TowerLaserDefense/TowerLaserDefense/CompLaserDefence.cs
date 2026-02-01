using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TowerLaserDefense;

public class CompLaserDefence : ThingComp, ILaserDefenceParent
{
	private LaserDefenceCore core;

	private CompProperties_LaserDefence Props => props as CompProperties_LaserDefence;

	public LaserDefenceCore DefenceCore => core ?? (core = new LaserDefenceCore(this, Props.laserDefenceProperties));

	public Thing Thing => parent;

	private CompPowerTrader Power => parent.GetComp<CompPowerTrader>();

	public bool DetectionEnabled
	{
		get
		{
			return DefenceCore?.DetectionEnabled ?? true;
		}
		set
		{
			if (DefenceCore != null)
			{
				DefenceCore.DetectionEnabled = value;
			}
		}
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		LaserDefenceCore.Instances.Add(DefenceCore);
	}

	public virtual void PostDeSpawn()
	{
		LaserDefenceCore.Instances.Remove(DefenceCore);
	}

	public virtual void PostDeSpawn(Map map)
	{
		if (core != null && LaserDefenceCore.Instances.Contains(core))
		{
			LaserDefenceCore.Instances.Remove(core);
		}
		base.PostDeSpawn(map);
	}

	private bool HasEnoughPowerToFire()
	{
		if (!Props.laserDefenceProperties.requiresPower || !Props.laserDefenceProperties.enablePowerConsumption || Power == null)
		{
			return true;
		}
		return DefenceCore.HasEnoughPowerToFire();
	}

	public override string CompInspectStringExtra()
	{
		string text = base.CompInspectStringExtra();
		string powerInfoString = GetPowerInfoString();
		return string.IsNullOrEmpty(text) ? powerInfoString : (text + "\n" + powerInfoString);
	}

	private string GetPowerInfoString()
	{
		LaserDefenceProperties laserDefenceProperties = Props.laserDefenceProperties;
		if (!laserDefenceProperties.requiresPower || !laserDefenceProperties.enablePowerConsumption || Power == null)
		{
			return string.Empty;
		}
		string text = "LaserDefence_PowerPerShot".Translate(laserDefenceProperties.powerConsumptionPerShot.ToString("F1"));
		if (!HasEnoughPowerToFire())
		{
			text += "\n" + "LaserDefence_PowerWarning".Translate(GetTotalStoredEnergy().ToString("F1"));
		}
		return text;
	}

	private float GetTotalStoredEnergy()
	{
		if (Power?.PowerNet == null)
		{
			return 0f;
		}
		float num = 0f;
		foreach (CompPowerBattery batteryComp in Power.PowerNet.batteryComps)
		{
			num += batteryComp.StoredEnergy;
		}
		return num;
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		if (core != null && LaserDefenceCore.Instances.Contains(core))
		{
			LaserDefenceCore.Instances.Remove(core);
		}
		base.PostDestroy(mode, previousMap);
	}

	public override void PostExposeData()
	{
		Scribe_Deep.Look(ref core, "CompLaserDefence_core", this, Props.laserDefenceProperties);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && core != null && !DetectionEnabled)
		{
			core.DetectionEnabled = false;
		}
	}

	public override void PostDrawExtraSelectionOverlays()
	{
		GenDraw.DrawRadiusRing(parent.Position, Props.laserDefenceProperties.range);
	}

	public override void CompTick()
	{
		DefenceCore.Tick();
	}

	public void ToggleDetection()
	{
		if (DefenceCore != null)
		{
			DefenceCore.ToggleDetection();
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (parent.Faction == Faction.OfPlayer && DefenceCore != null)
		{
			yield return new Command_Toggle
			{
				icon = ContentFinder<Texture2D>.Get("ModIcon/KillAllSplitterSpider"),
				defaultLabel = "TLD.DetectionState".Translate(),
				defaultDesc = (DefenceCore.DetectionEnabled ? "TLD.DetectionEnabledDesc".Translate() : "TLD.DetectionDisabledDesc".Translate()),
				isActive = () => DefenceCore.DetectionEnabled,
				toggleAction = ToggleDetection
			};
		}
	}

	public override void PostDraw()
	{
		DefenceCore.DrawAt(parent.TrueCenter());
	}
}
