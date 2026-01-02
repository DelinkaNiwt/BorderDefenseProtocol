using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class CompGenerator_SunLightFuel : ThingComp
{
	private int percentage = 0;

	private int tickNum = 0;

	public static readonly SimpleCurve FacilitiesNumToEfficiencyCurve = new SimpleCurve
	{
		new CurvePoint(0f, 500f),
		new CurvePoint(100f, 10f)
	};

	private Thing thing => ThingMaker.MakeThing(Props.product);

	private CompExplosive compExplosive => parent.TryGetComp<CompExplosive>();

	public CompProperties_Generator_SunLightFuel Props => (CompProperties_Generator_SunLightFuel)props;

	public Thing SunLightFuel => parent.TryGetInnerInteractableThingOwner().FirstOrFallback((Thing t) => t.def == MiliraDefOf.Milira_SunLightFuel);

	public CompThingContainer compThingContainer => parent.TryGetComp<CompThingContainer>();

	public CompAffectedByFacilities compAffectedByFacilities => parent.TryGetComp<CompAffectedByFacilities>();

	public int FacilitiesNum => compAffectedByFacilities.LinkedFacilitiesListForReading.Count();

	public int ProductPerGen => MiliraDefOf.Milira_ImprovedEnergyConcentrationCircuit.IsFinished ? (Props.productPerGenBase + 1) : Props.productPerGenBase;

	public bool CanEmptyNow
	{
		get
		{
			if (parent == null)
			{
				return false;
			}
			return SunLightFuel != null;
		}
	}

	private bool Roofed
	{
		get
		{
			foreach (IntVec3 item in GenRadial.RadialCellsAround(parent.Position, 1.9f, useCenter: true))
			{
				if (parent.Map.roofGrid.Roofed(item))
				{
					return true;
				}
			}
			return false;
		}
	}

	private bool CanWork => (float)FacilitiesNum > 0f;

	private bool SunLight
	{
		get
		{
			float num = Mathf.Lerp(0f, 100f, parent.Map.skyManager.CurSkyGlow);
			return num > 50f;
		}
	}

	public string LabelCapWithTotalCount
	{
		get
		{
			if (parent != null)
			{
				return thing.LabelCapNoCount + " x" + ProductPerGen.ToStringCached();
			}
			return null;
		}
	}

	public override void PostExposeData()
	{
		Scribe_Values.Look(ref percentage, "percentage", 0);
		Scribe_Values.Look(ref tickNum, "tickNum", 0);
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
	}

	public override void CompTick()
	{
		if (parent == null || Roofed || compThingContainer.Full || !CanWork || !SunLight)
		{
			return;
		}
		tickNum++;
		if ((float)tickNum > FacilitiesNumToEfficiencyCurve.Evaluate(FacilitiesNum))
		{
			tickNum = 0;
			percentage++;
			if (percentage >= 100)
			{
				percentage = 0;
				ProduceFuel(ProductPerGen);
			}
		}
	}

	public void ProduceFuel(int amountToMake)
	{
		int num = Mathf.CeilToInt((float)amountToMake / (float)this.thing.def.stackLimit);
		for (int i = 0; i < num; i++)
		{
			Thing thing = ThingMaker.MakeThing(Props.product);
			thing.stackCount = Mathf.Min(amountToMake, thing.def.stackLimit);
			if (parent.TryGetInnerInteractableThingOwner().TryAdd(thing))
			{
				amountToMake -= thing.stackCount;
				continue;
			}
			break;
		}
	}

	public override string CompInspectStringExtra()
	{
		return string.Concat("Milira_CompGenerator_SunLightFuel_Percentage".Translate() + LabelCapWithTotalCount + ": ", percentage.ToString(), "%");
	}
}
