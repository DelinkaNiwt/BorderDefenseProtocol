using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompGetHediff_Charging : ThingComp
{
	private CompProperties_GetHediff_Charging Props => (CompProperties_GetHediff_Charging)props;

	protected Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				if (parent is Pawn result)
				{
					return result;
				}
				return null;
			}
			return wearer;
		}
	}

	private bool staggering => PawnOwner.stances.stagger.Staggered;

	private CompMechAutoFight compMechAutoFight => PawnOwner.TryGetComp<CompMechAutoFight>();

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

	private Hediff Hediff => PawnOwner.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);

	private BodyPartRecord bodyPart => PawnOwner.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def == Props.bodyPartDef);

	public override void CompTick()
	{
		base.CompTick();
		if (PawnOwner != null && !PawnOwner.Downed && PawnOwner.Spawned)
		{
			if (Hediff != null && (float)PawnOwner.Map.pathing.For(PawnOwner).pathGrid.Cost(PawnOwner.Position) > Props.pathCostThreshold)
			{
				Hediff.Severity -= Props.blockedSeverityFactor * Props.severityPerTick_Stop;
			}
			if (Hediff != null && staggering)
			{
				Hediff.Severity -= Props.staggeredSeverityFactor * Props.severityPerTick_Stop;
			}
		}
	}

	public override void CompTickInterval(int delta)
	{
		base.CompTickInterval(delta);
		if (PawnOwner == null || PawnOwner.Downed || !PawnOwner.Spawned)
		{
			return;
		}
		if (PawnOwner.pather.MovingNow && (PawnOwner.Drafted || autoFightForPlayer || PawnOwner.InAggroMentalState || (PawnOwner.Faction != Faction.OfPlayer && !PawnOwner.IsPrisoner)) && movingSpeed >= Props.minSpeed)
		{
			if (Hediff == null)
			{
				Hediff hediff = PawnOwner.health.AddHediff(Props.hediffDef, bodyPart);
				hediff.Severity = Props.initialSeverity;
			}
			else if (Props.speedSeverityFactor.HasValue)
			{
				Hediff.Severity += movingSpeed * Props.speedSeverityFactor.Value * Props.severityPerTick_Job * (float)delta;
			}
			else
			{
				Hediff.Severity += Props.severityPerTick_Job * (float)delta;
			}
		}
		else if (Hediff != null)
		{
			Hediff.Severity -= Props.severityPerTick_Stop * (float)delta;
		}
	}
}
