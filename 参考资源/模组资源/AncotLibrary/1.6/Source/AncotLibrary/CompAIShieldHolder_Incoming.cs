using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAIShieldHolder_Incoming : ThingComp
{
	public int shieldHoldingTickLeft = -1;

	public int shieldDurationTick => Props.shieldDurationTick;

	public CompPhysicalShield compPhysicalShield
	{
		get
		{
			if (parent is Apparel apparel)
			{
				return apparel.GetComp<CompPhysicalShield>();
			}
			if (parent is Pawn pawn)
			{
				return pawn.GetComp<CompPhysicalShield>();
			}
			return null;
		}
	}

	private CompProperties_AIShieldHolder_Incoming Props => (CompProperties_AIShieldHolder_Incoming)props;

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

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref shieldHoldingTickLeft, "shieldHoldingTickLeft", -1);
	}

	public override void CompTickInterval(int delta)
	{
		if (compPhysicalShield != null && compPhysicalShield.holdShield && PawnOwner != null && PawnOwner.Spawned && shieldHoldingTickLeft > 0 && (PawnOwner.Faction != Faction.OfPlayer || (autoFightForPlayer && !PawnOwner.Drafted)))
		{
			shieldHoldingTickLeft -= delta;
			if (shieldHoldingTickLeft <= 0)
			{
				compPhysicalShield.holdShield = false;
				shieldHoldingTickLeft = -1;
			}
		}
	}

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		if (PawnOwner != null && (PawnOwner.Faction != Faction.OfPlayer || (autoFightForPlayer && !PawnOwner.Drafted)))
		{
			if (!compPhysicalShield.holdShield)
			{
				compPhysicalShield.holdShield = true;
				shieldHoldingTickLeft = Props.shieldDurationTick;
			}
			if (Props.refreshNextIncoming)
			{
				shieldHoldingTickLeft = Props.shieldDurationTick;
			}
		}
		absorbed = false;
	}
}
