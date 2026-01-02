using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompSpawnerCustom_PlantGrow : CompSpawnerCustom
{
	public int growTicks = 0;

	public CompProperties_SpawnerCustom_PlantGrow Props => (CompProperties_SpawnerCustom_PlantGrow)props;

	public int fullGrowTick => (int)(60000f * Props.growDays);

	private bool fullyGrown
	{
		get
		{
			if (growTicks > fullGrowTick)
			{
				return true;
			}
			return false;
		}
	}

	private bool lightSatisfy
	{
		get
		{
			float num = Mathf.Lerp(0f, 100f, parent.MapHeld.glowGrid.GroundGlowAt(parent.Position));
			if (num > Mathf.Lerp(0f, 100f, Props.growlight))
			{
				return true;
			}
			return false;
		}
	}

	private bool temperatureSatisfy
	{
		get
		{
			GenTemperature.TryGetTemperatureForCell(parent.Position, parent.Map, out var tempResult);
			if (Props.growTemperature.Includes(tempResult))
			{
				return true;
			}
			return false;
		}
	}

	private float growPercentage => (float)growTicks / (float)fullGrowTick;

	public override void CompTick()
	{
		if (parent.Map != null && temperatureSatisfy && lightSatisfy)
		{
			growTicks++;
			if (fullyGrown)
			{
				base.CompTick();
			}
		}
	}

	public override void CompTickRare()
	{
		if (parent.Map != null && temperatureSatisfy && lightSatisfy)
		{
			growTicks += 250;
			if (fullyGrown)
			{
				base.CompTickRare();
			}
		}
	}

	public override void CompTickLong()
	{
		if (parent.Map != null && temperatureSatisfy && lightSatisfy)
		{
			growTicks += 2000;
			if (fullyGrown)
			{
				base.CompTickLong();
			}
		}
	}

	public override string CompInspectStringExtra()
	{
		TaggedString taggedString = "";
		if (parent.Map != null)
		{
			if (fullyGrown)
			{
				taggedString += "Ancot.PlantGrowFully".Translate();
				taggedString += "\n" + base.CompInspectStringExtra();
			}
			else
			{
				taggedString += "Ancot.PlantGrowProgress".Translate(growPercentage.ToStringPercent());
			}
			if (!lightSatisfy)
			{
				taggedString += "\n" + "Ancot.PlantGrow_LightRequire".Translate(Props.growlight.ToStringPercent());
			}
			if (!temperatureSatisfy)
			{
				taggedString += "\n" + "Ancot.PlantGrow_TempRequire".Translate() + Props.growTemperature.ToString() + "℃";
			}
		}
		return taggedString;
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (DebugSettings.ShowDevGizmos)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: FullGrow",
				action = delegate
				{
					growTicks = fullGrowTick;
				}
			};
		}
	}

	public override void PostExposeData()
	{
		string text = (base.PropsSpawner.saveKeysPrefix.NullOrEmpty() ? null : (base.PropsSpawner.saveKeysPrefix + "_"));
		Scribe_Values.Look(ref growTicks, text + "growTicks", 0);
		base.PostExposeData();
	}
}
