using System.Linq;
using AncotLibrary;
using RimWorld;
using Verse;

namespace Milira;

public class CompMilianKnightICharge : ThingComp
{
	private CompProperties_MilianKnightICharge Props => (CompProperties_MilianKnightICharge)props;

	protected Pawn PawnOwner => parent as Pawn;

	private bool staggering => PawnOwner.stances.stagger.Staggered;

	private CompMechAutoFight compMechAutoFight => ((Thing)PawnOwner).TryGetComp<CompMechAutoFight>();

	public bool autoFightForPlayer
	{
		get
		{
			if (compMechAutoFight != null && PawnOwner != null && PawnOwner.Faction.IsPlayer)
			{
				return compMechAutoFight.AutoFight;
			}
			return false;
		}
	}

	private float movingSpeed => PawnOwner.GetStatValue(StatDefOf.MoveSpeed);

	public override void CompTick()
	{
		base.CompTick();
		if (PawnOwner == null || PawnOwner.Downed || !PawnOwner.Spawned)
		{
			return;
		}
		Hediff hediff = PawnOwner.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
		BodyPartRecord part = PawnOwner.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def == Props.bodyPartDef);
		if (PawnOwner.pather.MovingNow && (PawnOwner.Drafted || autoFightForPlayer || PawnOwner.InAggroMentalState || (PawnOwner.Faction != Faction.OfPlayer && !PawnOwner.IsPrisoner)) && movingSpeed >= Props.minSpeed)
		{
			if (hediff == null)
			{
				hediff = PawnOwner.health.AddHediff(Props.hediffDef, part);
				hediff.Severity = Props.initialSeverity;
				if (ModsConfig.IsActive("Ancot.MilianModification") && PawnOwner.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_FootCatapult) != null)
				{
					hediff.Severity = 2f;
				}
			}
			else if (Props.speedSeverityFactor.HasValue)
			{
				hediff.Severity += movingSpeed * Props.speedSeverityFactor.Value * Props.severityPerTick_Job;
			}
			else
			{
				hediff.Severity += Props.severityPerTick_Job;
			}
		}
		else if (hediff != null)
		{
			hediff.Severity -= Props.severityPerTick_Stop;
		}
		if (hediff != null && (float)PawnOwner.Map.pathing.For(PawnOwner).pathGrid.Cost(PawnOwner.Position) > Props.pathCostThreshold)
		{
			hediff.Severity -= Props.blockedSeverityFactor * Props.severityPerTick_Stop;
		}
		if (hediff != null && staggering && (!ModsConfig.IsActive("Ancot.MilianModification") || PawnOwner.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_ShockAbsorptionModule) == null))
		{
			hediff.Severity -= Props.staggeredSeverityFactor * Props.severityPerTick_Stop;
		}
	}
}
