using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class CompDualHediffMechHealer : ThingComp
{
	private CompRefuelable refuelableComp;

	private int ticksUntilNextHeal;

	public bool enableLight = true;

	public bool enableMedium = true;

	public bool enableHeavy = true;

	public bool enableUltraHeavy = true;

	public bool useHediffOne = true;

	private CompProperties_DualHediffMechHealer Props => (CompProperties_DualHediffMechHealer)props;

	private bool AnySwitchEnabled => enableLight || enableMedium || enableHeavy || enableUltraHeavy;

	private HediffDef ActiveHediff => useHediffOne ? Props.hediffOne : Props.hediffTwo;

	private Texture2D ActiveIcon => ContentFinder<Texture2D>.Get(useHediffOne ? Props.texPathOne : Props.texPathTwo);

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		refuelableComp = parent.GetComp<CompRefuelable>();
		ticksUntilNextHeal = Props.healIntervalTicks;
	}

	public override void CompTick()
	{
		base.CompTick();
		if (AnySwitchEnabled && --ticksUntilNextHeal <= 0)
		{
			TryHealMechs();
			ticksUntilNextHeal = Props.healIntervalTicks;
		}
	}

	private void TryHealMechs()
	{
		if (refuelableComp == null || !refuelableComp.HasFuel || ActiveHediff == null)
		{
			return;
		}
		foreach (Pawn mech in GetValidMechsInRange())
		{
			if (IsTargetTypeEnabled(mech))
			{
				float fuelCost = GetFuelCostByWeightClass(mech);
				if (refuelableComp.Fuel >= fuelCost)
				{
					ApplyHediffWithFeedback(mech, fuelCost);
				}
			}
		}
	}

	private void SwitchHediff()
	{
		useHediffOne = !useHediffOne;
		Messages.Message("NCL_HediffSwitched".Translate(ActiveHediff.LabelCap), MessageTypeDefOf.NeutralEvent);
	}

	private bool IsTargetTypeEnabled(Pawn mech)
	{
		if (mech.def?.race?.mechWeightClass == null)
		{
			return false;
		}
		return mech.def.race.mechWeightClass.defName switch
		{
			"Light" => enableLight, 
			"Medium" => enableMedium, 
			"Heavy" => enableHeavy, 
			"UltraHeavy" => enableUltraHeavy, 
			_ => enableLight, 
		};
	}

	private float GetFuelCostByWeightClass(Pawn mech)
	{
		if (mech.def?.race?.mechWeightClass == null)
		{
			return Props.fuelCostLight;
		}
		return mech.def.race.mechWeightClass.defName switch
		{
			"UltraHeavy" => Props.fuelCostUltraHeavy, 
			"Heavy" => Props.fuelCostHeavy, 
			"Medium" => Props.fuelCostMedium, 
			_ => Props.fuelCostLight, 
		};
	}

	private IEnumerable<Pawn> GetValidMechsInRange()
	{
		foreach (Pawn pawn in parent.Map.mapPawns.AllPawns)
		{
			if (pawn.Position.DistanceTo(parent.Position) <= Props.radius && IsValidMechTarget(pawn))
			{
				yield return pawn;
			}
		}
	}

	private bool IsValidMechTarget(Pawn pawn)
	{
		return pawn.IsColonyMech && pawn.Faction == Faction.OfPlayer && !pawn.Dead && !pawn.Downed && !pawn.health.hediffSet.HasHediff(ActiveHediff);
	}

	private void ApplyHediffWithFeedback(Pawn mech, float fuelCost)
	{
		HealthUtility.AdjustSeverity(mech, ActiveHediff, 1f);
		refuelableComp.ConsumeFuel(fuelCost);
		string weightClassKey = "MechWeightClass_" + mech.def.race.mechWeightClass.ToString();
		MoteMaker.ThrowText(mech.DrawPos, mech.Map, "NCL_MechHealed".Translate(mech.LabelShort, weightClassKey.Translate(), fuelCost.ToString("0.0"), ActiveHediff.LabelCap), Color.green);
	}

	public override string CompInspectStringExtra()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("NCL_Healer_Status".Translate(AnySwitchEnabled ? "NCL_Healer_Active".Translate() : "NCL_Healer_Inactive".Translate()));
		sb.AppendLine();
		sb.Append("NCL_CurrentHediff".Translate(ActiveHediff.LabelCap));
		if (refuelableComp != null)
		{
			sb.AppendLine();
			if (refuelableComp.HasFuel)
			{
				sb.Append("NCL_FuelCost_Light".Translate(Props.fuelCostLight.ToString("0.0")));
				sb.Append(" | ");
				sb.Append("NCL_FuelCost_Medium".Translate(Props.fuelCostMedium.ToString("0.0")));
				sb.AppendLine();
				sb.Append("NCL_FuelCost_Heavy".Translate(Props.fuelCostHeavy.ToString("0.0")));
				sb.Append(" | ");
				sb.Append("NCL_FuelCost_UltraHeavy".Translate(Props.fuelCostUltraHeavy.ToString("0.0")));
			}
			else
			{
				sb.Append("NCL_Healer_NoFuel".Translate());
			}
		}
		return sb.ToString();
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		yield return new Command_Action
		{
			icon = ActiveIcon,
			defaultLabel = (useHediffOne ? Props.labelOne : Props.labelTwo).Translate(),
			defaultDesc = "NCL_CurrentHediff".Translate(ActiveHediff.LabelCap),
			action = SwitchHediff
		};
		yield return new Command_Toggle
		{
			icon = ContentFinder<Texture2D>.Get("ModIcon/MechSize_Light"),
			defaultLabel = "NCL_EnableLight".Translate(),
			isActive = () => enableLight,
			toggleAction = delegate
			{
				enableLight = !enableLight;
			}
		};
		yield return new Command_Toggle
		{
			icon = ContentFinder<Texture2D>.Get("ModIcon/MechSize_Medium"),
			defaultLabel = "NCL_EnableMedium".Translate(),
			isActive = () => enableMedium,
			toggleAction = delegate
			{
				enableMedium = !enableMedium;
			}
		};
		yield return new Command_Toggle
		{
			icon = ContentFinder<Texture2D>.Get("ModIcon/MechSize_Heavy"),
			defaultLabel = "NCL_EnableHeavy".Translate(),
			isActive = () => enableHeavy,
			toggleAction = delegate
			{
				enableHeavy = !enableHeavy;
			}
		};
		yield return new Command_Toggle
		{
			icon = ContentFinder<Texture2D>.Get("ModIcon/MechSize_UltraHeavy"),
			defaultLabel = "NCL_EnableUltraHeavy".Translate(),
			isActive = () => enableUltraHeavy,
			toggleAction = delegate
			{
				enableUltraHeavy = !enableUltraHeavy;
			}
		};
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref ticksUntilNextHeal, "ticksUntilNextHeal", Props.healIntervalTicks);
		Scribe_Values.Look(ref enableLight, "enableLight", defaultValue: true);
		Scribe_Values.Look(ref enableMedium, "enableMedium", defaultValue: true);
		Scribe_Values.Look(ref enableHeavy, "enableHeavy", defaultValue: true);
		Scribe_Values.Look(ref enableUltraHeavy, "enableUltraHeavy", defaultValue: true);
		Scribe_Values.Look(ref useHediffOne, "useHediffOne", defaultValue: true);
	}
}
