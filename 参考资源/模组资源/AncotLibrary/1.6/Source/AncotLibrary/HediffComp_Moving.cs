using RimWorld;
using Verse;

namespace AncotLibrary;

public class HediffComp_Moving : HediffComp
{
	private CompMechAutoFight compMechAutoFight;

	private HediffCompProperties_Moving Props => (HediffCompProperties_Moving)props;

	private CompMechAutoFight CompMechAutoFight => compMechAutoFight ?? (compMechAutoFight = base.Pawn.TryGetComp<CompMechAutoFight>());

	private float movingSpeed => base.Pawn.GetStatValue(StatDefOf.MoveSpeed);

	public bool autoFightForPlayer
	{
		get
		{
			if (CompMechAutoFight != null && base.Pawn != null && base.Pawn.Faction.IsPlayer)
			{
				return CompMechAutoFight.AutoFight;
			}
			return false;
		}
	}

	public override void CompPostTickInterval(ref float severityAdjustment, int delta)
	{
		base.CompPostTickInterval(ref severityAdjustment, delta);
		if (base.Pawn.Spawned && base.Pawn.IsHashIntervalTick(10, delta))
		{
			if (InMovingState())
			{
				parent.Severity = Props.severityMoving;
			}
			else if (Props.useDefaultSeverity)
			{
				parent.Severity = Props.severityDefault;
			}
		}
	}

	public virtual bool InMovingState()
	{
		return base.Pawn.pather.MovingNow && (base.Pawn.Drafted || autoFightForPlayer || base.Pawn.InAggroMentalState || (base.Pawn.Faction != Faction.OfPlayer && !base.Pawn.IsPrisoner) || !Props.onlyInCombat) && movingSpeed >= Props.minSpeed;
	}
}
